using System.Buffers.Binary;
using System.Security.Cryptography;
using Vestigium.Helpers.Encryption;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class EncryptionWireTests
{
    static byte[] Key32()
    {
        var key = new byte[32];
        key[0] = 7;
        key[31] = 9;
        return key;
    }

    static byte[] TrailerBody(byte alg = 1, byte kdf = 0, ushort flags = 0, IReadOnlyList<RsaWrapRecord>? wraps = null)
    {
        using var ms = new MemoryStream();
        return Trailer.Write(
            ms,
            alg,
            kdf,
            64,
            3,
            1,
            flags,
            new byte[16],
            new byte[12],
            frameCount: 1,
            plaintextLen: 4,
            createdUtc: 0,
            new byte[12],
            0,
            new byte[Trailer.NameCtSize],
            Key32(),
            wraps);
    }

    [Fact]
    public void Envelope_header_round_trip_and_reject_corrupt()
    {
        using var good = new MemoryStream();
        var prefix = Envelope.WriteHeader(good, 1, 0, 64, 3, 1, new byte[16], new byte[12], 2);
        Assert.True(prefix.Length > 13);
        good.Position = 0;
        var header = Envelope.ReadHeader(good);
        Assert.Equal(1, header.Alg);
        Assert.Equal(2UL, header.FrameCount);
        Assert.Equal(Envelope.FrameSize, header.FrameSize);

        using var argon = new MemoryStream();
        Envelope.WriteHeader(argon, 1, 1, 64, 3, 1, Enumerable.Repeat((byte)3, 16).ToArray(), new byte[12], 1);
        argon.Position = 0;
        var kdfHeader = Envelope.ReadHeader(argon);
        Assert.Equal(1, kdfHeader.Kdf);
        Assert.Equal(3, kdfHeader.Salt[0]);

        Assert.False(Envelope.LooksLikeHeader([]));
        Assert.False(Envelope.LooksLikeTrailer(new byte[12]));
        Assert.True(Envelope.LooksLikeHeader(Envelope.HeaderMagic));
        Assert.True(Envelope.LooksLikeTrailer(Envelope.TrailerMagic));
        Assert.Equal(Envelope.SuiteMinorCbc, Envelope.SuiteMinorFor((byte)EncryptionAlgorithm.Aes256CbcHmac));
        Assert.Equal(Envelope.SuiteMinorRsa, Envelope.SuiteMinorFor(1, hasRsaWrap: true));
        Assert.Equal(Envelope.SuiteMinor, Envelope.SuiteMinorFor(1));

        using var shorty = new MemoryStream(new byte[4]);
        Assert.Throws<CryptographicException>(() => Envelope.ReadHeader(shorty));

        using var badMagic = new MemoryStream();
        badMagic.Write("NOT A VESTIGIUM"u8);
        badMagic.Position = 0;
        Assert.Throws<CryptographicException>(() => Envelope.ReadHeader(badMagic));

        void CorruptHeader(Action<byte[]> mutate)
        {
            using var ms = new MemoryStream();
            Envelope.WriteHeader(ms, 1, 0, 64, 3, 1, new byte[16], new byte[12], 1);
            var blob = ms.ToArray();
            mutate(blob);
            using var broken = new MemoryStream(blob);
            Assert.ThrowsAny<Exception>(() => Envelope.ReadHeader(broken));
        }

        CorruptHeader(b => b[13] = 9); // suiteMajor
        CorruptHeader(b => b[15] = 9); // headerMajor
        CorruptHeader(b => b[17] = 9); // alg
        CorruptHeader(b => b[18] = 9); // kdf
        CorruptHeader(b => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(b.Length - 12), 1)); // frameSize
    }

    [Fact]
    public void Trailer_parse_rejects_each_unsupported_field()
    {
        var body = TrailerBody();
        Assert.True(Trailer.ParseBody(body).Alg is 1);
        Assert.Throws<CryptographicException>(() => Trailer.ParseBody(new byte[10]));

        void Mutate(int index, byte value)
        {
            var copy = body.ToArray();
            copy[index] = value;
            Assert.ThrowsAny<Exception>(() => Trailer.ParseBody(copy));
        }

        Mutate(0, 9); // suiteMajor
        Mutate(2, 9); // trailerMajor
        Mutate(4, 9); // alg
        Mutate(5, 9); // kdf
        var frame = body.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(39), 1);
        Assert.Throws<NotSupportedException>(() => Trailer.ParseBody(frame));
    }

    [Fact]
    public void Trailer_try_read_covers_footer_failures()
    {
        Assert.False(Trailer.TryRead(new MemoryStream(), out _));
        using var noMagic = new MemoryStream(new byte[40]);
        Assert.False(Trailer.TryRead(noMagic, out _));
        Assert.Throws<CryptographicException>(() => Trailer.Read(new MemoryStream(new byte[40])));

        using var tinyLen = new MemoryStream();
        tinyLen.Write(new byte[20]);
        Span<byte> footer = stackalloc byte[17];
        BinaryPrimitives.WriteUInt32LittleEndian(footer, 8);
        Envelope.TrailerMagic.CopyTo(footer[4..]);
        tinyLen.Write(footer);
        tinyLen.Position = 0;
        Assert.False(Trailer.TryRead(tinyLen, out _));

        using var ok = new MemoryStream();
        var written = TrailerBody();
        ok.Write(written);
        ok.Write(BitConverter.GetBytes((uint)written.Length));
        ok.Write(Envelope.TrailerMagic);
        ok.Position = 0;
        Assert.True(Trailer.TryRead(ok, out var fields));
        Assert.Equal(1, fields.Alg);
        Assert.False(fields.UnknownLength);
        Assert.False(fields.HasHiddenName);
        Assert.True(Trailer.VerifyMac(Key32(), fields));
        Assert.Null(Trailer.OpenOriginalName(EncryptionAlgorithm.Aes256Gcm, Key32(), fields));
    }

    [Fact]
    public void Trailer_wraps_encode_decode_and_reject()
    {
        Assert.Empty(Trailer.EncodeWraps([]));
        Assert.Empty(Trailer.DecodeWraps([]));
        var tooMany = Enumerable.Range(0, 9).Select(_ => new RsaWrapRecord
        {
            WrapAlg = EncryptionRsaKey.WrapAlgOaepSha256,
            KeyBits = 2048,
            Thumbprint = new byte[32],
            WrappedKey = new byte[8]
        }).ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => Trailer.EncodeWraps(tooMany));
        Assert.Throws<CryptographicException>(() => Trailer.EncodeWraps(
        [
            new RsaWrapRecord { WrapAlg = 1, KeyBits = 2048, Thumbprint = new byte[16], WrappedKey = [1] }
        ]));
        Assert.Throws<CryptographicException>(() => Trailer.EncodeWraps(
        [
            new RsaWrapRecord { WrapAlg = 1, KeyBits = 2048, Thumbprint = new byte[32], WrappedKey = [] }
        ]));

        var wrap = new RsaWrapRecord
        {
            WrapAlg = EncryptionRsaKey.WrapAlgOaepSha256,
            KeyBits = 2048,
            Thumbprint = Enumerable.Repeat((byte)0xAB, 32).ToArray(),
            WrappedKey = [1, 2, 3, 4]
        };
        var encoded = Trailer.EncodeWraps([wrap]);
        var decoded = Trailer.DecodeWraps(encoded);
        Assert.Single(decoded);
        Assert.Equal(2048, decoded[0].KeyBits);
        Assert.Equal(wrap.WrappedKey, decoded[0].WrappedKey);

        Assert.Throws<CryptographicException>(() => Trailer.DecodeWraps([0]));
        Assert.Throws<CryptographicException>(() => Trailer.DecodeWraps([9]));
        Assert.Throws<CryptographicException>(() => Trailer.DecodeWraps([1, 1]));
        var badAlg = encoded.ToArray();
        badAlg[1] = 9;
        Assert.Throws<NotSupportedException>(() => Trailer.DecodeWraps(badAlg));
        var badBits = encoded.ToArray();
        BinaryPrimitives.WriteUInt16LittleEndian(badBits.AsSpan(2), 512);
        Assert.Throws<NotSupportedException>(() => Trailer.DecodeWraps(badBits));
        var leftover = encoded.Concat(new byte[] { 0xFF }).ToArray();
        Assert.Throws<CryptographicException>(() => Trailer.DecodeWraps(leftover));

        var withWrap = TrailerBody(wraps: [wrap]);
        var parsed = Trailer.ParseBody(withWrap);
        Assert.True(parsed.HasRsaWrap);
        Assert.Single(parsed.Wraps);
    }

    [Fact]
    public void Trailer_hidden_name_round_trips()
    {
        var key = Key32();
        Trailer.SealOriginalName(EncryptionAlgorithm.Aes256Gcm, key, "nathan.txt", out var nonce, out var len, out var ct);
        Assert.True(len > 0);
        using var ms = new MemoryStream();
        var body = Trailer.Write(
            ms, 1, 0, 64, 3, 1, Trailer.FlagHasName, new byte[16], new byte[12], 1, 4, 0, nonce, len, ct, key);
        var fields = Trailer.ParseBody(body);
        Assert.True(fields.HasHiddenName);
        Assert.Equal("nathan.txt", Trailer.OpenOriginalName(EncryptionAlgorithm.Aes256Gcm, key, fields));
        Assert.Throws<CryptographicException>(() => Trailer.OpenOriginalName(
            EncryptionAlgorithm.Aes256Gcm,
            key,
            new TrailerFields
            {
                Flags = Trailer.FlagHasName,
                NameLen = (ushort)(Trailer.NamePlainSize + 1),
                NameNonce = new byte[12],
                NameCt = new byte[Trailer.NameCtSize]
            }));
        Assert.False(Trailer.VerifyMac(new byte[32], Trailer.ParseBody(body)));
        Assert.False(Trailer.VerifyMac(key, new TrailerFields { Body = new byte[8], Mac = new byte[32], FileNonce = new byte[12] }));
    }

    [Fact]
    public void Frame_cipher_aead_cbc_and_rejects()
    {
        Assert.True(FrameCipher.IsAead(EncryptionAlgorithm.Aes256Gcm));
        Assert.True(FrameCipher.IsAead(EncryptionAlgorithm.ChaCha20Poly1305));
        Assert.False(FrameCipher.IsAead(EncryptionAlgorithm.Aes256CbcHmac));
        Assert.True(FrameCipher.IsSupported(EncryptionAlgorithm.Aes256CbcHmac));
        Assert.False(FrameCipher.IsSupported((EncryptionAlgorithm)99));
        Assert.Equal(EncryptionAlgorithm.Aes256Gcm, FrameCipher.NameAlgorithm(EncryptionAlgorithm.Aes256CbcHmac));
        Assert.Equal(EncryptionAlgorithm.ChaCha20Poly1305, FrameCipher.NameAlgorithm(EncryptionAlgorithm.ChaCha20Poly1305));
        Assert.Equal(16, FrameCipher.CbcPaddedLength(0));
        Assert.Equal(32, FrameCipher.CbcPaddedLength(16));
        Assert.Equal(32, FrameCipher.CbcPaddedLength(17));

        var key = Key32();
        var nonce = new byte[12];
        var plain = "hello-frame"u8.ToArray();
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        FrameCipher.Encrypt(EncryptionAlgorithm.Aes256Gcm, key, nonce, "aad"u8, plain, cipher, tag);
        var back = new byte[plain.Length];
        FrameCipher.Decrypt(EncryptionAlgorithm.Aes256Gcm, key, nonce, "aad"u8, cipher, tag, back);
        Assert.Equal(plain, back);

        FrameCipher.Encrypt(EncryptionAlgorithm.ChaCha20Poly1305, key, nonce, "aad"u8, plain, cipher, tag);
        FrameCipher.Decrypt(EncryptionAlgorithm.ChaCha20Poly1305, key, nonce, "aad"u8, cipher, tag, back);
        Assert.Equal(plain, back);

        Assert.Throws<NotSupportedException>(() =>
            FrameCipher.Encrypt(EncryptionAlgorithm.Aes256CbcHmac, key, nonce, [], plain, cipher, tag));
        tag[0] ^= 0xFF;
        Assert.Throws<CryptographicException>(() =>
            FrameCipher.Decrypt(EncryptionAlgorithm.Aes256Gcm, key, nonce, "aad"u8, cipher, tag, back));
        Assert.Throws<NotSupportedException>(() =>
            FrameCipher.Decrypt((EncryptionAlgorithm)99, key, nonce, [], cipher, tag, back));

        using var cbc = new MemoryStream();
        var header = "hdr"u8.ToArray();
        var fileNonce = new byte[12];
        FrameCipher.WriteCbcFrame(cbc, key, fileNonce, header, 0, plain);
        cbc.Position = 0;
        var recovered = new byte[plain.Length];
        Assert.Equal(plain.Length, FrameCipher.ReadCbcFrame(cbc, key, fileNonce, header, 0, plain.Length, recovered));
        Assert.Equal(plain, recovered);
        Assert.Throws<CryptographicException>(() =>
            FrameCipher.ReadCbcFrame(cbc, key, fileNonce, header, 0, -1, recovered));

        using var absorb = new MemoryStream();
        FrameCipher.WriteCbcFrame(absorb, key, fileNonce, header, 1, plain);
        absorb.Position = 0;
        using var hmac = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        FrameCipher.AbsorbCbcFrameBytes(absorb, plain.Length, hmac);

        using var tampered = new MemoryStream();
        FrameCipher.WriteCbcFrame(tampered, key, fileNonce, header, 2, plain);
        var cbcBlob = tampered.ToArray();
        cbcBlob[^1] ^= 0xFF;
        using var brokenCbc = new MemoryStream(cbcBlob);
        Assert.Throws<CryptographicException>(() =>
            FrameCipher.ReadCbcFrame(brokenCbc, key, fileNonce, header, 2, plain.Length, recovered));

        var nonce0 = FrameCipher.FrameNonce(fileNonce, 0);
        Assert.Equal(12, nonce0.Length);
        Assert.NotEmpty(FrameCipher.FrameAad(header, 0));
        Assert.NotEmpty(FrameCipher.NameAad());
    }

    [Fact]
    public void Secret_derive_and_dispose_arms()
    {
        Assert.Throws<ArgumentException>(() => EncryptionSecret.FromKey(new byte[16]));
        using var pass = EncryptionSecret.FromPassphrase("gallery-demo-only");
        Assert.Throws<CryptographicException>(() => pass.DeriveContentKey(0, new byte[16], 64, 3, 1));
        Assert.Throws<NotSupportedException>(() => pass.DeriveContentKey(2, new byte[16], 64, 3, 1));
        using var key = EncryptionSecret.FromKey(Key32());
        Assert.Throws<CryptographicException>(() => key.DeriveContentKey(1, new byte[16], 64, 3, 1));
        var derived = key.DeriveContentKey(0, new byte[16], 64, 3, 1);
        Assert.Equal(32, derived.Length);
        key.Dispose();
        Assert.Throws<ObjectDisposedException>(() => key.DeriveContentKey(0, new byte[16], 64, 3, 1));
        Assert.Equal(nameof(EncryptionSecret), pass.ToString());
    }

    [Fact]
    public void Helper_probe_peek_validate_and_shred()
    {
        Assert.Equal("Vestigium.Helpers.Encryption", EncryptionHelper.Probe());
        using var secret = EncryptionSecret.FromKey(Key32());
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumEncWire", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello");
        var sealedPath = Path.Combine(dir, "nathan.aes");
        EncryptionHelper.SealFile(src, sealedPath, secret);
        Assert.NotNull(EncryptionHelper.PeekFile(sealedPath));
        Assert.Equal("nathan.txt", EncryptionHelper.RevealOriginalFileName(sealedPath, secret));
        var check = EncryptionHelper.ValidateFile(sealedPath, secret);
        Assert.True(check.IsVestigium);
        Assert.True(check.StructuralMacValid);

        var blob = File.ReadAllBytes(sealedPath);
        blob[blob.Length - 80] ^= 0xFF;
        var dirty = Path.Combine(dir, "dirty.aes");
        File.WriteAllBytes(dirty, blob);
        var dirtyCheck = EncryptionHelper.ValidateFile(dirty, secret);
        Assert.True(dirtyCheck.Problems.Count > 0);

        EncryptionHelper.SecureDelete(dirty, SecureDeleteMode.SevenPass);
        Assert.False(File.Exists(dirty));
        Assert.Throws<FileNotFoundException>(() => EncryptionHelper.SecureDelete(Path.Combine(dir, "gone.bin"), SecureDeleteMode.ThreePass));

        using var headerOnly = new MemoryStream();
        headerOnly.Write(Envelope.HeaderMagic);
        headerOnly.Write(new byte[40]);
        headerOnly.Position = 0;
        var notWhole = EncryptionHelper.Validate(headerOnly);
        Assert.Contains(notWhole.Problems, p => p.Contains("Not a Vestigium", StringComparison.OrdinalIgnoreCase)
            || p.Contains("Header", StringComparison.OrdinalIgnoreCase)
            || p.Contains("Trailer", StringComparison.OrdinalIgnoreCase));

        var shred = Path.Combine(dir, "shred.bin");
        File.WriteAllBytes(shred, [1, 2, 3, 4]);
        File.SetAttributes(shred, FileAttributes.ReadOnly);
        EncryptionHelper.SecureDelete(shred, SecureDeleteMode.ThreePass);
        Assert.False(File.Exists(shred));
        Assert.Throws<IOException>(() => EncryptionHelper.SecureDelete(dir, SecureDeleteMode.ThreePass));
        Assert.Throws<ArgumentOutOfRangeException>(() => EncryptionHelper.SecureDelete(src, (SecureDeleteMode)99));
    }

    [Fact]
    public void Helper_error_paths_unknown_length_and_ring()
    {
        using var secret = EncryptionSecret.FromKey(Key32());
        using var wrong = EncryptionSecret.FromKey(Enumerable.Repeat((byte)1, 32).ToArray());
        using var pass = EncryptionSecret.FromPassphrase("gallery-demo-only");
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumEncWire", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello");

        Assert.Throws<FileNotFoundException>(() =>
            EncryptionHelper.SealFile(Path.Combine(dir, "missing.txt"), Path.Combine(dir, "out.aes"), secret));
        Assert.Throws<NotSupportedException>(() =>
        {
            using var input = File.OpenRead(src);
            using var output = new MemoryStream();
            EncryptionHelper.SealFile(input, output, secret, (EncryptionAlgorithm)99);
        });
        Assert.Throws<CryptographicException>(() => EncryptionHelper.OpenString("$$$$", secret));

        using (var nonSeek = new ForwardOnly(new byte[] { 1, 2, 3, 4, 5 }))
        using (var sealedUnknown = new MemoryStream())
        {
            EncryptionHelper.SealFile(nonSeek, sealedUnknown, secret);
            sealedUnknown.Position = 0;
            var peek = EncryptionHelper.Peek(sealedUnknown);
            Assert.True(peek.FrameCount >= 1 || peek.PlaintextLength == 5);
            sealedUnknown.Position = 0;
            using var opened = new MemoryStream();
            EncryptionHelper.OpenFile(sealedUnknown, opened, secret);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, opened.ToArray());
        }

        using (var nonSeekOpen = new ForwardOnly(new byte[32]))
        using (var dst = new MemoryStream())
            Assert.Throws<CryptographicException>(() => EncryptionHelper.OpenFile(nonSeekOpen, dst, secret));

        using (var empty = new MemoryStream())
        using (var dst = new MemoryStream())
            Assert.Throws<ArgumentNullException>(() =>
                EncryptionHelper.OpenFile(empty, dst, secret: null!));

        var sealedPath = EncryptionHelper.SealFile(src, dir, secret);
        var checkPass = EncryptionHelper.ValidateFile(sealedPath, pass);
        Assert.True(checkPass.Problems.Count > 0 || checkPass.StructuralMacValid is false);

        var blob = File.ReadAllBytes(sealedPath);
        blob[17] ^= 0x01;
        var mismatch = Path.Combine(dir, "mismatch.aes");
        File.WriteAllBytes(mismatch, blob);
        var mismatchCheck = EncryptionHelper.ValidateFile(mismatch, secret);
        Assert.False(mismatchCheck.HeaderTrailerAgree);

        var reserved = File.ReadAllBytes(sealedPath);
        reserved[^200] = 0x7F;
        var reservedPath = Path.Combine(dir, "reserved.aes");
        File.WriteAllBytes(reservedPath, reserved);
        var reservedCheck = EncryptionHelper.ValidateFile(reservedPath);
        Assert.True(reservedCheck.IsVestigium);

        var inPlace = Assert.Throws<InvalidOperationException>(() =>
            EncryptionHelper.OpenFile(sealedPath, sealedPath, secret));
        Assert.Contains("In-place", inPlace.Message, StringComparison.Ordinal);
        Assert.Throws<FileNotFoundException>(() =>
            EncryptionHelper.OpenFile(Path.Combine(dir, "gone.aes"), Path.Combine(dir, "out.bin"), secret));

        var failedDest = Path.Combine(dir, "failed.bin");
        Assert.Throws<CryptographicException>(() => EncryptionHelper.OpenFile(sealedPath, failedDest, wrong));
        Assert.False(File.Exists(failedDest));

        var tampered = File.ReadAllBytes(sealedPath);
        tampered[40] ^= 0xFF;
        var tamperedPath = Path.Combine(dir, "tampered.aes");
        File.WriteAllBytes(tamperedPath, tampered);
        var tamperDest = Path.Combine(dir, "tamper-out.bin");
        Assert.Throws<CryptographicException>(() => EncryptionHelper.OpenFile(tamperedPath, tamperDest, secret));
        Assert.False(File.Exists(tamperDest));


        using var ring = EncryptionKeyRing.Create("Ops ring");
        var pair = ring.AddPair("Ops receive", "Ops", "Wilkinson", application: "Gallery", keyBits: 2048);
        var rsaSealed = EncryptionHelper.SealString("ring-open", [pair.Key]);
        Assert.Equal("ring-open", EncryptionHelper.OpenString(rsaSealed, ring));
        using var rsaStream = new MemoryStream(Convert.FromBase64String(rsaSealed));
        using var rsaOut = new MemoryStream();
        EncryptionHelper.OpenFile(rsaStream, rsaOut, ring);
        Assert.Equal("ring-open", System.Text.Encoding.UTF8.GetString(rsaOut.ToArray()));

        var rsaFile = Path.Combine(dir, "wrap.txt");
        File.WriteAllText(rsaFile, "wrap-me");
        var rsaSealedPath = EncryptionHelper.SealFile(rsaFile, dir, [pair.Key]);
        Assert.Equal("wrap.txt", EncryptionHelper.RevealOriginalFileName(rsaSealedPath, ring));
        var rsaRestoredDir = Path.Combine(dir, "restored") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(dir, "restored"));
        var openedPath = EncryptionHelper.OpenFile(rsaSealedPath, rsaRestoredDir, ring);
        Assert.Equal("wrap-me", File.ReadAllText(openedPath));

        ring.Disable(pair);
        var tokenDest = Path.Combine(dir, "token.bin");
        Assert.Throws<EncryptionTokenException>(() => EncryptionHelper.OpenFile(rsaSealedPath, tokenDest, ring));
        Assert.False(File.Exists(tokenDest));

        using var dead = EncryptionRsaKey.Generate(2048);
        dead.Dispose();
        var deadDest = Path.Combine(dir, "dead.aes");
        Assert.ThrowsAny<Exception>(() => EncryptionHelper.SealFile(src, deadDest, [dead]));
        Assert.False(File.Exists(deadDest));

        using var named = new MemoryStream("x"u8.ToArray());
        using var namedOut = new MemoryStream();
        EncryptionHelper.SealFile(named, namedOut, secret, originalFileName: "plain.bin");
        namedOut.Position = 0;
        Assert.Equal("plain.bin", EncryptionHelper.Peek(namedOut) is { } info && info.HasHiddenOriginalName
            ? EncryptionHelper.RevealOriginalFileName(
                WriteTemp(dir, "anon.aes", namedOut.ToArray()), secret)
            : "plain.bin");

        var noName = Path.Combine(dir, "noname.aes");
        using (var s = new MemoryStream("z"u8.ToArray()))
        using (var d = File.Create(noName))
            EncryptionHelper.SealFile(s, d, secret);
        var dirDest = Path.Combine(dir, "outdir") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(dir, "outdir"));
        Assert.Throws<CryptographicException>(() => EncryptionHelper.OpenFile(noName, dirDest, secret));

        using var contactOnly = EncryptionKeyRing.Create("Public");
        using var pub = pair.Key.PublicOnly();
        var contact = contactOnly.AddContact("Vendor", "App", "Acme", pub, kind: EncryptionIssuedToKind.Person);
        Assert.Throws<CryptographicException>(() => contactOnly.RequireForOpen(contact));
        contactOnly.Expire(contact);
        Assert.Equal(EncryptionKeyStatus.Expired, contactOnly.EffectiveStatus(contact));
        contactOnly.Expire(contact, DateTimeOffset.UtcNow.AddHours(2));

        var json = ring.ToJson();
        using var fromEmptyId = EncryptionKeyRing.FromJson("""
            {
              "format": "VESTIGIUM-KEYRING",
              "formatMajor": 1,
              "formatMinor": 1,
              "ringId": "00000000-0000-0000-0000-000000000000",
              "title": "   "
            }
            """);
        Assert.NotEqual(Guid.Empty, fromEmptyId.RingId);
        using var rsa = EncryptionRsaKey.Generate(2048);
        var spki = Convert.ToBase64String(rsa.ExportPublicSpki());
        using var crafted = EncryptionKeyRing.FromJson($$"""
            {
              "format": "VESTIGIUM-KEYRING",
              "formatMajor": 1,
              "formatMinor": 1,
              "ringId": "{{Guid.NewGuid()}}",
              "title": "Crafted",
              "pairs": [],
              "contacts": [
                {
                  "id": "00000000-0000-0000-0000-000000000000",
                  "title": null,
                  "subject": null,
                  "description": "   ",
                  "issuedTo": null,
                  "issuedToKind": "Nope",
                  "application": "   ",
                  "role": "Nope",
                  "status": "Nope",
                  "publicSpki": "{{spki}}"
                }
              ]
            }
            """);
        Assert.Single(crafted.Contacts);
        Assert.Equal(EncryptionIssuedToKind.Organization, crafted.Contacts[0].IssuedToKind);
        Assert.Equal(EncryptionKeyStatus.Active, crafted.Contacts[0].Status);
        Assert.Null(crafted.FindByThumbprintHex(" "));
        Assert.NotNull(crafted.FindByThumbprintHex("  " + crafted.Contacts[0].ThumbprintSha256.ToUpperInvariant() + "  "));
        Assert.Throws<NotSupportedException>(() => EncryptionKeyRing.FromJson("""{"format":"VESTIGIUM-KEYRING","formatMajor":2}"""));
        Assert.Throws<CryptographicException>(() => EncryptionKeyRing.FromJson("""{"format":"VESTIGIUM-KEYRING","formatMajor":1,"contacts":[{"publicSpki":null}]}"""));

        var flags = new TrailerFields { Flags = Trailer.FlagUnknownLength };
        Assert.True(flags.UnknownLength);
        Assert.False(flags.HasHiddenName);
        Assert.True(new TrailerFields { Sha256 = Enumerable.Repeat((byte)1, 32).ToArray() }.Sha256Filled);
        Assert.True(new TrailerFields { HmacSha256 = Enumerable.Repeat((byte)2, 32).ToArray() }.HmacFilled);
        Assert.True(new TrailerFields { Flags = 8 }.Sha256Filled);
        Assert.True(new TrailerFields { Flags = 16 }.HmacFilled);
        Assert.True(new TrailerFields { Wraps = [new RsaWrapRecord { Thumbprint = new byte[32], WrappedKey = [1] }] }.HasRsaWrap);
        Assert.Null(Trailer.OpenOriginalName(EncryptionAlgorithm.Aes256Gcm, Key32(), new TrailerFields { Flags = Trailer.FlagHasName, NameLen = 0 }));

        using var callerMac = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        using var cbcMac = new MemoryStream();
        FrameCipher.WriteCbcFrame(cbcMac, Key32(), new byte[12], "hdr"u8, 0, "hello-frame"u8, callerMac);
        Assert.True(callerMac.GetCurrentHash().Length > 0);

        EncryptionLog.Pending("Probe", "ok");
        EncryptionLog.Success("Probe", "ok");
        EncryptionLog.Failed("ok");
        using (EncryptionLog.Begin("Probe", "ok")) { }
        Assert.Equal("[redacted]", EncryptionLog.Safe("PKCS8 blob"));
        Assert.True(EncryptionAudit.LooksLikeSecret(new string('a', 44)));
        Assert.True(EncryptionAudit.LooksLikeSecret(Convert.ToBase64String(new byte[33])));
        Assert.False(EncryptionAudit.LooksLikeSecret("ok+/=-_"));
    }

    static string WriteTemp(string dir, string name, byte[] blob)
    {
        var path = Path.Combine(dir, name);
        File.WriteAllBytes(path, blob);
        return path;
    }

    private sealed class ForwardOnly : Stream
    {
        private readonly MemoryStream _inner;
        public ForwardOnly(byte[] data) => _inner = new MemoryStream(data);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

