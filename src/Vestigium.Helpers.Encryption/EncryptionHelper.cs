using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Encryption;

/// <summary>
/// Authenticated encryption helpers (AES-256-GCM default, ChaCha20-Poly1305, AES-256-CBC+HMAC v1.1, RSA-OAEP wrap v1.2, Argon2id).
/// Libraries never call Initialize. Hashing lives in Vestigium.Helpers.Hashing. RSA never encrypts payload frames.
/// </summary>
public static class EncryptionHelper
{
    public static string Identity => "Vestigium.Helpers.Encryption";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Encryption;
        HelperLog.Information(app, VestigiumStatus.Pending, "Encryption", "Opening an encryption helper probe.");
        using var secret = EncryptionSecret.FromPassphrase("gallery-demo-only");
        var sealedText = SealString("probe", secret);
        var back = OpenString(sealedText, secret);
        if (back != "probe")
            throw new CryptographicException("The envelope is corrupt.");
        HelperLog.Information(app, VestigiumStatus.Success, "Encryption", "Encryption probe complete. Identity=" + Identity);
        return Identity;
    }

    public static string DefaultExportDirectory(string appId)
    {
        var id = HelperGuard.NotBlank(appId, nameof(appId));
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (string.IsNullOrWhiteSpace(desktop))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            desktop = Path.Combine(string.IsNullOrWhiteSpace(home) ? "." : home, "Desktop");
        }

        return Path.Combine(desktop, "Vestigium", "Exports", id);
    }

    public static string FileExtension(EncryptionSecret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return secret.IsPassphrase ? ".argon" : ".aes";
    }

    public static string SealedFileName(string originalFileName, EncryptionSecret secret)
        => OriginalNames.Stem(originalFileName) + FileExtension(secret);

    public static string NewExportPath(string appId, string? originalFileName = null, EncryptionSecret? secret = null)
    {
        var id = HelperGuard.NotBlank(appId, nameof(appId));
        string file;
        if (!string.IsNullOrWhiteSpace(originalFileName) && secret is not null)
            file = SealedFileName(originalFileName, secret);
        else
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var ext = secret is null ? ".aes" : FileExtension(secret);
            file = $"vestigium-{id}-{stamp}{ext}";
        }

        foreach (var ch in Path.GetInvalidFileNameChars())
            file = file.Replace(ch, '_');
        return Path.Combine(DefaultExportDirectory(id), file);
    }

    public static string? RevealOriginalFileName(string path, EncryptionSecret secret)
    {
        using var source = OpenRead(path);
        return RevealOriginalFileName(source, secret, rsa: null, ring: null);
    }

    public static string? RevealOriginalFileName(string path, EncryptionRsaKey rsa)
    {
        using var source = OpenRead(path);
        return RevealOriginalFileName(source, secret: null, rsa, ring: null);
    }

    public static string? RevealOriginalFileName(string path, EncryptionKeyRing ring)
    {
        using var source = OpenRead(path);
        return RevealOriginalFileName(source, secret: null, rsa: null, ring);
    }

    public static bool IsVestigiumFile(string path)
    {
        if (!File.Exists(path))
            return false;
        using var source = OpenRead(path);
        return IsVestigium(source);
    }

    public static bool IsVestigium(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanSeek)
            return false;
        var origin = source.Position;
        try
        {
            if (source.Length - origin >= 13)
            {
                source.Position = origin;
                Span<byte> head = stackalloc byte[13];
                if (source.Read(head) == 13 && Envelope.LooksLikeHeader(head))
                    return true;
            }

            if (source.Length >= 13)
            {
                source.Position = source.Length - 13;
                Span<byte> tail = stackalloc byte[13];
                if (source.Read(tail) == 13 && Envelope.LooksLikeTrailer(tail))
                    return true;
            }

            return false;
        }
        finally
        {
            source.Position = origin;
        }
    }

    public static bool TryPeekFile(string path, out EncryptionFileInfo info)
    {
        info = null!;
        try
        {
            if (!File.Exists(path))
                return false;
            using var source = OpenRead(path);
            if (!Trailer.TryRead(source, out var trailer))
                return false;
            info = ToInfo(trailer, originalName: null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static EncryptionFileInfo PeekFile(string path)
    {
        using var source = OpenRead(path);
        return Peek(source);
    }

    public static EncryptionFileInfo Peek(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var origin = source.CanSeek ? source.Position : 0;
        try
        {
            var trailer = Trailer.Read(source);
            Log("Peek", VestigiumStatus.Success, $"alg={trailer.Alg} frames={trailer.FrameCount} bytes={trailer.PlaintextLength}");
            return ToInfo(trailer, originalName: null);
        }
        finally
        {
            if (source.CanSeek)
                source.Position = origin;
        }
    }

    public static EncryptionValidationResult ValidateFile(string path, EncryptionSecret? secret = null)
    {
        if (!File.Exists(path))
        {
            return new EncryptionValidationResult
            {
                Problems = ["Source file was not found."]
            };
        }

        using var source = OpenRead(path);
        return Validate(source, secret);
    }

    public static EncryptionValidationResult Validate(Stream source, EncryptionSecret? secret = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        var problems = new List<string>();
        var origin = source.CanSeek ? source.Position : 0;
        EnvelopeHeader? header = null;
        TrailerFields? trailer = null;
        try
        {
            var isVestigium = IsVestigium(source);
            if (!isVestigium)
            {
                problems.Add("Not a Vestigium envelope.");
                return new EncryptionValidationResult { Problems = problems };
            }

            if (source.CanSeek)
            {
                source.Position = origin;
                try
                {
                    header = Envelope.ReadHeader(source);
                }
                catch (Exception ex) when (ex is CryptographicException or NotSupportedException)
                {
                    problems.Add("Header is missing or unsupported.");
                }

                try
                {
                    trailer = Trailer.Read(source);
                }
                catch (Exception ex) when (ex is CryptographicException or NotSupportedException)
                {
                    problems.Add("Trailer is missing or unsupported.");
                }
            }

            var agree = header is not null && trailer is not null && HeadersAgree(header, trailer, problems);
            bool? macValid = null;
            string? originalName = null;
            if (secret is not null && trailer is not null)
            {
                byte[]? key = null;
                try
                {
                    key = secret.DeriveContentKey(trailer.Kdf, trailer.Salt, trailer.KdfMemMiB, trailer.KdfIter, trailer.KdfPar);
                    macValid = Trailer.VerifyMac(key, trailer);
                    if (macValid == true)
                        originalName = Trailer.OpenOriginalName((EncryptionAlgorithm)trailer.Alg, key, trailer);
                    else
                        problems.Add("Structural mac is invalid.");
                }
                catch (Exception ex) when (ex is CryptographicException or NotSupportedException)
                {
                    macValid = false;
                    problems.Add("Structural mac is invalid.");
                }
                finally
                {
                    if (key is not null)
                        CryptographicOperations.ZeroMemory(key);
                }
            }

            var reservedHashEmpty = trailer is null || trailer.Sha256.All(b => b == 0);
            var reservedHmacEmpty = trailer is null || trailer.HmacSha256.All(b => b == 0);
            if (trailer is not null && !reservedHashEmpty)
                problems.Add("Reserved sha256 slot is not empty.");
            if (trailer is not null && !reservedHmacEmpty)
                problems.Add("Reserved hmacSha256 slot is not empty.");

            var info = trailer is null ? null : ToInfo(trailer, originalName);
            return new EncryptionValidationResult
            {
                IsVestigium = true,
                HeaderPresent = header is not null,
                TrailerPresent = trailer is not null,
                HeaderTrailerAgree = agree,
                StructuralMacValid = macValid,
                HasHiddenOriginalName = trailer?.HasHiddenName == true,
                OriginalFileName = originalName,
                Info = info,
                Problems = problems
            };
        }
        finally
        {
            if (source.CanSeek)
                source.Position = origin;
        }
    }

    public static string SealString(string plaintext, EncryptionSecret secret, EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm, IReadOnlyList<EncryptionRsaKey>? rsaRecipients = null)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(secret);
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        using var source = new MemoryStream(bytes, writable: false);
        using var destination = new MemoryStream();
        SealFile(source, destination, secret, alg, bytes.LongLength, originalFileName: null, rsaRecipients);
        return Convert.ToBase64String(destination.ToArray());
    }

    public static string SealString(
        string plaintext,
        IReadOnlyList<EncryptionRsaKey> rsaRecipients,
        EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm,
        EncryptionRsaKey? alsoWrapTo = null)
    {
        ArgumentNullException.ThrowIfNull(rsaRecipients);
        var raw = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var secret = EncryptionSecret.FromKey(raw);
            var wraps = CombineWraps(rsaRecipients, alsoWrapTo);
            return SealString(plaintext, secret, alg, wraps);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    public static string OpenString(string sealedBase64, EncryptionSecret secret)
        => OpenStringCore(sealedBase64, secret, rsa: null, ring: null);

    public static string OpenString(string sealedBase64, EncryptionRsaKey rsa)
        => OpenStringCore(sealedBase64, secret: null, rsa, ring: null);

    public static string OpenString(string sealedBase64, EncryptionKeyRing ring)
        => OpenStringCore(sealedBase64, secret: null, rsa: null, ring);

    private static string OpenStringCore(string sealedBase64, EncryptionSecret? secret, EncryptionRsaKey? rsa, EncryptionKeyRing? ring)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sealedBase64);
        byte[] blob;
        try
        {
            blob = Convert.FromBase64String(sealedBase64);
        }
        catch (FormatException)
        {
            throw new CryptographicException("The envelope is corrupt.");
        }

        using var source = new MemoryStream(blob, writable: false);
        using var destination = new MemoryStream();
        OpenFileCore(source, destination, secret, rsa, ring);
        return Encoding.UTF8.GetString(destination.ToArray());
    }

    public static string SealFile(
        string sourcePath,
        string destinationPath,
        EncryptionSecret secret,
        EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm,
        SecureDeleteMode shredPlaintext = SecureDeleteMode.Keep,
        IReadOnlyList<EncryptionRsaKey>? rsaRecipients = null)
    {
        var source = HelperGuard.NotBlank(sourcePath, nameof(sourcePath));
        var destArg = HelperGuard.NotBlank(destinationPath, nameof(destinationPath));
        ArgumentNullException.ThrowIfNull(secret);
        if (!File.Exists(source))
        {
            var missing = new FileNotFoundException("Source file was not found.", source);
            LogFailed("Source file was not found.", missing);
            throw missing;
        }

        var originalName = OriginalNames.Validate(Path.GetFileName(source));
        var destFile = ResolveSealDestination(destArg, originalName, secret);
        if (PathsEqual(source, destFile))
            throw new InvalidOperationException("In-place encryption is not supported.");

        Directory.CreateDirectory(Path.GetDirectoryName(destFile) ?? ".");
        var created = !File.Exists(destFile);
        try
        {
            using var input = File.OpenRead(source);
            using var output = File.Create(destFile);
            SealFile(input, output, secret, alg, input.Length, originalName, rsaRecipients);
            Log("Seal", VestigiumStatus.Success, $"path={Path.GetFileName(destFile)} frames done");
        }
        catch (Exception ex)
        {
            LogFailed("Seal failed.", ex);
            if (created)
                TryDelete(destFile);
            throw;
        }

        if (shredPlaintext != SecureDeleteMode.Keep)
            SecureDelete(source, shredPlaintext);

        return destFile;
    }

    public static string SealFile(
        string sourcePath,
        string destinationPath,
        IReadOnlyList<EncryptionRsaKey> rsaRecipients,
        EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm,
        SecureDeleteMode shredPlaintext = SecureDeleteMode.Keep,
        EncryptionRsaKey? alsoWrapTo = null)
    {
        var raw = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var secret = EncryptionSecret.FromKey(raw);
            return SealFile(sourcePath, destinationPath, secret, alg, shredPlaintext, CombineWraps(rsaRecipients, alsoWrapTo));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    public static string OpenFile(string sourcePath, string destinationPath, EncryptionSecret secret)
        => OpenFilePath(sourcePath, destinationPath, secret, rsa: null, ring: null);

    public static string OpenFile(string sourcePath, string destinationPath, EncryptionRsaKey rsa)
        => OpenFilePath(sourcePath, destinationPath, secret: null, rsa, ring: null);

    public static string OpenFile(string sourcePath, string destinationPath, EncryptionKeyRing ring)
        => OpenFilePath(sourcePath, destinationPath, secret: null, rsa: null, ring);

    private static string OpenFilePath(
        string sourcePath,
        string destinationPath,
        EncryptionSecret? secret,
        EncryptionRsaKey? rsa,
        EncryptionKeyRing? ring)
    {
        var source = HelperGuard.NotBlank(sourcePath, nameof(sourcePath));
        var destArg = HelperGuard.NotBlank(destinationPath, nameof(destinationPath));
        if (!File.Exists(source))
        {
            var missing = new FileNotFoundException("Source file was not found.", source);
            LogFailed("Source file was not found.", missing);
            throw missing;
        }

        using var input = File.OpenRead(source);
        var originalName = RevealOriginalFileName(input, secret, rsa, ring);
        input.Position = 0;
        var destFile = ResolveOpenDestination(destArg, originalName);
        if (PathsEqual(source, destFile))
            throw new InvalidOperationException("In-place decryption is not supported.");

        Directory.CreateDirectory(Path.GetDirectoryName(destFile) ?? ".");
        var created = !File.Exists(destFile);
        try
        {
            using var output = File.Create(destFile);
            OpenFileCore(input, output, secret, rsa, ring);
            Log("Open", VestigiumStatus.Success, $"path={Path.GetFileName(destFile)}");
            return destFile;
        }
        catch (Exception ex)
        {
            LogFailed("Open failed.", ex);
            if (created)
                TryDelete(destFile);
            throw;
        }
    }

    /// <summary>
    /// Overwrite <paramref name="path"/> with random bytes, then zeros, then delete it.
    /// <see cref="SecureDeleteMode.ThreePass"/> is 3 random + 1 zero.
    /// <see cref="SecureDeleteMode.SevenPass"/> is 7 random + 1 zero.
    /// Flash media may still retain prior cells; this is the on-disk contract for magnetic / ordinary files.
    /// </summary>
    public static void SecureDelete(string path, SecureDeleteMode mode)
    {
        if (mode is not (SecureDeleteMode.ThreePass or SecureDeleteMode.SevenPass))
            throw new ArgumentOutOfRangeException(nameof(mode), "Use ThreePass or SevenPass.");

        var file = HelperGuard.NotBlank(path, nameof(path));
        if (Directory.Exists(file) && !File.Exists(file))
            throw new IOException("Refusing to shred a directory.");
        if (!File.Exists(file))
        {
            var missing = new FileNotFoundException("Source file was not found.", file);
            LogFailed("Source file was not found.", missing);
            throw missing;
        }

        var randomPasses = (int)mode;
        Log("Shred", VestigiumStatus.Pending, $"passes={randomPasses}+zero");
        const int chunk = 65536;
        var buffer = new byte[chunk];
        try
        {
            var attrs = File.GetAttributes(file);
            if (attrs.HasFlag(FileAttributes.ReadOnly))
                File.SetAttributes(file, attrs & ~FileAttributes.ReadOnly);

            using (var stream = new FileStream(
                       file,
                       FileMode.Open,
                       FileAccess.Write,
                       FileShare.None,
                       chunk,
                       FileOptions.SequentialScan | FileOptions.WriteThrough))
            {
                var length = stream.Length;
                for (var pass = 0; pass < randomPasses; pass++)
                    OverwritePass(stream, length, buffer, random: true);
                OverwritePass(stream, length, buffer, random: false);
                stream.Flush(flushToDisk: true);
            }

            File.Delete(file);
            Log("Shred", VestigiumStatus.Success, $"passes={randomPasses}+zero");
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            LogFailed("Shred failed.", ex);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }

    public static void SealFile(
        Stream source,
        Stream destination,
        EncryptionSecret secret,
        EncryptionAlgorithm alg = EncryptionAlgorithm.Aes256Gcm,
        long? plaintextLength = null,
        string? originalFileName = null,
        IReadOnlyList<EncryptionRsaKey>? rsaRecipients = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(secret);
        if (!FrameCipher.IsSupported(alg))
            throw new NotSupportedException("alg");
        var hasWrap = rsaRecipients is { Count: > 0 };

        var unknown = plaintextLength is null && !source.CanSeek;
        long length;
        if (plaintextLength is not null)
            length = plaintextLength.Value;
        else if (source.CanSeek)
            length = Math.Max(0, source.Length - source.Position);
        else
            length = -1;
        if (length < 0 && !unknown)
            unknown = true;

        var algByte = (byte)alg;
        byte kdf = secret.IsPassphrase ? (byte)1 : (byte)0;
        byte kdfMem = kdf == 1 ? Argon2idKdf.MemoryMiB : (byte)0;
        byte kdfIter = kdf == 1 ? Argon2idKdf.Iterations : (byte)0;
        byte kdfPar = kdf == 1 ? Argon2idKdf.Parallelism : (byte)0;
        var salt = kdf == 1 ? RandomNumberGenerator.GetBytes(16) : new byte[16];
        var fileNonce = new byte[12];
        RandomNumberGenerator.Fill(fileNonce.AsSpan(0, 8));

        byte[]? key = null;
        var plain = new byte[Envelope.FrameSize];
        var cipher = new byte[Envelope.FrameSize];
        var tag = new byte[Envelope.TagSize];
        try
        {
            key = secret.DeriveContentKey(kdf, salt, kdfMem, kdfIter, kdfPar);
            var headerCount = unknown ? 0UL : (ulong)FrameCountFor(length);
            Log("Seal", VestigiumStatus.Pending, $"alg={algByte} kdf={kdf} wrap={hasWrap} bytes={(unknown ? -1 : length)} frames={headerCount}");
            var prefix = Envelope.WriteHeader(destination, algByte, kdf, kdfMem, kdfIter, kdfPar, salt, fileNonce, headerCount, hasWrap);

            ulong frames = 0;
            ulong writtenPlain = 0;
            uint index = 0;
            while (true)
            {
                int n;
                if (!unknown && writtenPlain >= (ulong)length)
                    break;
                var want = (int)Envelope.FrameSize;
                if (!unknown)
                    want = (int)Math.Min((ulong)want, (ulong)length - writtenPlain);
                n = ReadUpTo(source, plain.AsSpan(0, want));
                if (n <= 0)
                    break;
                if (FrameCipher.IsAead(alg))
                {
                    var nonce = FrameCipher.FrameNonce(fileNonce, index);
                    var aad = FrameCipher.FrameAad(prefix, index);
                    FrameCipher.Encrypt(alg, key, nonce, aad, plain.AsSpan(0, n), cipher.AsSpan(0, n), tag);
                    destination.Write(cipher.AsSpan(0, n));
                    destination.Write(tag);
                }
                else
                {
                    FrameCipher.WriteCbcFrame(destination, key, fileNonce, prefix, index, plain.AsSpan(0, n));
                }
                writtenPlain += (uint)n;
                frames++;
                index++;
            }

            ushort flags = 0;
            if (unknown)
                flags |= Trailer.FlagUnknownLength;
            var nameNonce = new byte[12];
            ushort nameLen = 0;
            var nameCt = new byte[Trailer.NameCtSize];
            if (!string.IsNullOrWhiteSpace(originalFileName))
            {
                Trailer.SealOriginalName(alg, key, originalFileName, out nameNonce, out nameLen, out nameCt);
                flags |= Trailer.FlagHasName;
            }

            var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var wraps = BuildWraps(rsaRecipients, key);
            Trailer.Write(
                destination,
                algByte,
                kdf,
                kdfMem,
                kdfIter,
                kdfPar,
                flags,
                salt,
                fileNonce,
                frames,
                writtenPlain,
                created,
                nameNonce,
                nameLen,
                nameCt,
                key,
                wraps);
            Log("Seal", VestigiumStatus.Success, $"alg={algByte} kdf={kdf} wraps={wraps.Count} frames={frames} bytes={writtenPlain}");
        }
        finally
        {
            if (key is not null)
                CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
            CryptographicOperations.ZeroMemory(cipher);
        }
    }

    public static void OpenFile(Stream source, Stream destination, EncryptionSecret secret)
        => OpenFileCore(source, destination, secret, rsa: null, ring: null);

    public static void OpenFile(Stream source, Stream destination, EncryptionRsaKey rsa)
        => OpenFileCore(source, destination, secret: null, rsa, ring: null);

    public static void OpenFile(Stream source, Stream destination, EncryptionKeyRing ring)
        => OpenFileCore(source, destination, secret: null, rsa: null, ring);

    private static void OpenFileCore(
        Stream source,
        Stream destination,
        EncryptionSecret? secret,
        EncryptionRsaKey? rsa,
        EncryptionKeyRing? ring)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (secret is null && rsa is null && ring is null)
            throw new ArgumentNullException(nameof(secret));
        if (!source.CanSeek)
            throw new CryptographicException("The envelope is corrupt.");

        var origin = source.Position;
        Log("Open", VestigiumStatus.Pending, "Open start");
        EnvelopeHeader header;
        TrailerFields trailer;
        try
        {
            header = Envelope.ReadHeader(source);
            trailer = Trailer.Read(source);
        }
        catch (NotSupportedException)
        {
            throw;
        }
        catch
        {
            throw new CryptographicException("The envelope is corrupt.");
        }

        var problems = new List<string>();
        if (!HeadersAgree(header, trailer, problems))
            throw new CryptographicException("The envelope is corrupt.");

        byte[]? key = null;
        var plain = new byte[Envelope.FrameSize];
        var cipher = new byte[Envelope.FrameSize];
        var tag = new byte[Envelope.TagSize];
        try
        {
            key = ResolveContentKey(trailer, secret, rsa, ring);
            if (key is null || !Trailer.VerifyMac(key, trailer))
                throw new CryptographicException("The envelope is corrupt.");

            var frameCount = trailer.FrameCount;

            var alg = (EncryptionAlgorithm)trailer.Alg;
            if (!FrameCipher.IsSupported(alg))
                throw new NotSupportedException("alg");
            source.Position = origin;
            Envelope.ReadHeader(source);
            ulong remaining = trailer.PlaintextLength;
            for (uint i = 0; i < frameCount; i++)
            {
                var n = remaining == 0 ? 0 : (int)Math.Min(Envelope.FrameSize, remaining);
                if (FrameCipher.IsAead(alg))
                {
                    Envelope.ReadExact(source, cipher.AsSpan(0, n));
                    Envelope.ReadExact(source, tag);
                    var nonce = FrameCipher.FrameNonce(trailer.FileNonce, i);
                    var aad = FrameCipher.FrameAad(header.PrefixThroughFrameSize, i);
                    FrameCipher.Decrypt(alg, key, nonce, aad, cipher.AsSpan(0, n), tag, plain.AsSpan(0, n));
                }
                else
                {
                    FrameCipher.ReadCbcFrame(
                        source,
                        key,
                        trailer.FileNonce,
                        header.PrefixThroughFrameSize,
                        i,
                        n,
                        plain.AsSpan(0, n));
                }
                destination.Write(plain.AsSpan(0, n));
                remaining -= (uint)n;
            }
            Log("Open", VestigiumStatus.Success, $"alg={trailer.Alg} frames={frameCount} bytes={trailer.PlaintextLength}");
        }
        finally
        {
            if (key is not null)
                CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
            CryptographicOperations.ZeroMemory(cipher);
        }
    }

    private static string? RevealOriginalFileName(
        Stream source,
        EncryptionSecret? secret,
        EncryptionRsaKey? rsa,
        EncryptionKeyRing? ring)
    {
        var origin = source.CanSeek ? source.Position : 0;
        byte[]? key = null;
        try
        {
            var trailer = Trailer.Read(source);
            key = ResolveContentKey(trailer, secret, rsa, ring);
            if (key is null || !Trailer.VerifyMac(key, trailer))
                throw new CryptographicException("The envelope is corrupt.");
            return Trailer.OpenOriginalName((EncryptionAlgorithm)trailer.Alg, key, trailer);
        }
        finally
        {
            if (key is not null)
                CryptographicOperations.ZeroMemory(key);
            if (source.CanSeek)
                source.Position = origin;
        }
    }

    private static EncryptionFileInfo ToInfo(TrailerFields trailer, string? originalName)
        => new()
        {
            SuiteVersion = $"{trailer.SuiteMajor}.{trailer.SuiteMinor}",
            HeaderVersion = $"{trailer.SuiteMajor}.{trailer.SuiteMinor}",
            TrailerVersion = $"{trailer.TrailerMajor}.{trailer.TrailerMinor}",
            Algorithm = (EncryptionAlgorithm)trailer.Alg,
            UsedArgon2id = trailer.Kdf == 1,
            FrameSize = (int)trailer.FrameSize,
            FrameCount = trailer.FrameCount,
            PlaintextLength = trailer.PlaintextLength,
            CreatedUtc = trailer.CreatedUtc == 0
                ? null
                : DateTimeOffset.FromUnixTimeSeconds(trailer.CreatedUtc),
            Sha256ReservedFilled = trailer.Sha256Filled,
            HmacSha256ReservedFilled = trailer.HmacFilled,
            HasHiddenOriginalName = trailer.HasHiddenName,
            OriginalFileName = originalName,
            HasRsaWrap = trailer.HasRsaWrap,
            WrapCount = trailer.Wraps.Count,
            WrapThumbprints = trailer.Wraps.Select(w => Convert.ToHexString(w.Thumbprint).ToLowerInvariant()).ToArray()
        };

    private static bool HeadersAgree(EnvelopeHeader header, TrailerFields trailer, List<string> problems)
    {
        var ok = true;
        void Check(bool cond, string problem)
        {
            if (cond)
                return;
            ok = false;
            problems.Add(problem);
        }

        Check(header.Alg == trailer.Alg, "alg mismatch.");
        Check(header.Kdf == trailer.Kdf, "kdf mismatch.");
        Check(header.FrameSize == trailer.FrameSize, "frameSize mismatch.");
        Check(header.FileNonce.AsSpan().SequenceEqual(trailer.FileNonce), "fileNonce mismatch.");
        if (header.Kdf == 1)
            Check(header.Salt.AsSpan().SequenceEqual(trailer.Salt), "salt mismatch.");
        else
            Check(trailer.Salt.All(b => b == 0), "raw-key salt must be zeros.");

        if (header.FrameCount != 0)
            Check(header.FrameCount == trailer.FrameCount, "frameCount mismatch.");
        else if (!trailer.UnknownLength)
            Check(trailer.FrameCount == 0, "frameCount mismatch.");

        var expected = FrameCountFor((long)trailer.PlaintextLength);
        Check((ulong)expected == trailer.FrameCount, "plaintext length does not match frame count.");
        return ok;
    }

    private static long FrameCountFor(long length)
        => length <= 0 ? 0 : (length + Envelope.FrameSize - 1) / Envelope.FrameSize;

    private static void OverwritePass(FileStream stream, long length, byte[] buffer, bool random)
    {
        stream.Position = 0;
        var remaining = length;
        while (remaining > 0)
        {
            var n = (int)Math.Min(buffer.Length, remaining);
            if (random)
                RandomNumberGenerator.Fill(buffer.AsSpan(0, n));
            else
                Array.Clear(buffer, 0, n);
            stream.Write(buffer, 0, n);
            remaining -= n;
        }

        stream.Flush(flushToDisk: true);
    }

    private static string ResolveSealDestination(string destinationPath, string originalName, EncryptionSecret secret)
    {
        if (IsDirectoryPath(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
            return Path.Combine(destinationPath, SealedFileName(originalName, secret));
        }

        return destinationPath;
    }

    private static string ResolveOpenDestination(string destinationPath, string? originalName)
    {
        if (!IsDirectoryPath(destinationPath))
            return destinationPath;
        Directory.CreateDirectory(destinationPath);
        if (string.IsNullOrWhiteSpace(originalName))
            throw new CryptographicException("The envelope is corrupt.");
        return Path.Combine(destinationPath, originalName);
    }

    private static bool IsDirectoryPath(string path)
        => Directory.Exists(path)
           || path.EndsWith(Path.DirectorySeparatorChar)
           || path.EndsWith(Path.AltDirectorySeparatorChar);

    private static bool PathsEqual(string a, string b)
        => string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    private static FileStream OpenRead(string path)
        => new(HelperGuard.NotBlank(path, nameof(path)), FileMode.Open, FileAccess.Read, FileShare.Read);

    private static int ReadUpTo(Stream source, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var n = source.Read(buffer[total..]);
            if (n == 0)
                break;
            total += n;
        }

        return total;
    }

    private static IReadOnlyList<EncryptionRsaKey> CombineWraps(IReadOnlyList<EncryptionRsaKey> recipients, EncryptionRsaKey? alsoWrapTo)
    {
        ArgumentNullException.ThrowIfNull(recipients);
        if (recipients.Count == 0 && alsoWrapTo is null)
            throw new ArgumentException("At least one RSA wrap key is required.", nameof(recipients));
        var list = new List<EncryptionRsaKey>(recipients);
        if (alsoWrapTo is not null)
            list.Add(alsoWrapTo);
        return list;
    }

    private static IReadOnlyList<RsaWrapRecord> BuildWraps(IReadOnlyList<EncryptionRsaKey>? recipients, byte[] contentKey)
    {
        if (recipients is null || recipients.Count == 0)
            return [];
        if (recipients.Count > Trailer.MaxWraps)
            throw new ArgumentOutOfRangeException(nameof(recipients), "At most 8 RSA wraps.");
        var wraps = new List<RsaWrapRecord>(recipients.Count);
        foreach (var rsa in recipients)
        {
            ArgumentNullException.ThrowIfNull(rsa);
            wraps.Add(new RsaWrapRecord
            {
                WrapAlg = EncryptionRsaKey.WrapAlgOaepSha256,
                KeyBits = (ushort)rsa.KeyBits,
                Thumbprint = rsa.Thumbprint,
                WrappedKey = rsa.Wrap(contentKey)
            });
        }

        return wraps;
    }

    private static byte[]? ResolveContentKey(
        TrailerFields trailer,
        EncryptionSecret? secret,
        EncryptionRsaKey? rsa,
        EncryptionKeyRing? ring)
    {
        if (secret is not null)
        {
            byte[]? derived = null;
            try
            {
                derived = secret.DeriveContentKey(trailer.Kdf, trailer.Salt, trailer.KdfMemMiB, trailer.KdfIter, trailer.KdfPar);
                if (Trailer.VerifyMac(derived, trailer))
                    return derived;
            }
            catch (CryptographicException)
            {
                // try RSA wraps next
            }
            if (derived is not null)
                CryptographicOperations.ZeroMemory(derived);
        }

        foreach (var wrap in trailer.Wraps)
        {
            EncryptionRsaKey? key = null;
            if (rsa is not null && rsa.CanUnwrap && rsa.ThumbprintEquals(wrap.Thumbprint))
                key = rsa;
            else if (ring is not null)
                key = ring.FindPrivate(wrap.Thumbprint);
            if (key is null)
                continue;
            try
            {
                var unwrapped = key.Unwrap(wrap.WrappedKey);
                if (Trailer.VerifyMac(unwrapped, trailer))
                    return unwrapped;
                CryptographicOperations.ZeroMemory(unwrapped);
            }
            catch (CryptographicException)
            {
                // next wrap
            }
        }

        return null;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // best-effort cleanup of a failed destination
        }
    }

    private static void Log(string verb, VestigiumStatus status, string message)
        => HelperLog.Information(HelperLog.AppIds.Encryption, status, "Encryption", $"{verb}: {message}");

    private static void LogFailed(string message, Exception ex)
        => HelperLog.Error(HelperLog.AppIds.Encryption, VestigiumStatus.Failed, "Encryption", message, ex);
}
