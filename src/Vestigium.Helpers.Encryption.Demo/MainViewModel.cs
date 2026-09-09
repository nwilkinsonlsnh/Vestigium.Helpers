using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Vestigium.Helpers;
using Vestigium.Helpers.Encryption;
using Vestigium.Helpers.Gallery;

namespace Vestigium.Helpers.Encryption.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    private static readonly byte[] DemoKey = CreateDemoKey();

    public MainViewModel()
    {
        Gcm = new CipherSession(EncryptionAlgorithm.Aes256Gcm, SetStatus);
        ChaCha = new CipherSession(EncryptionAlgorithm.ChaCha20Poly1305, SetStatus);
        Cbc = new CipherSession(EncryptionAlgorithm.Aes256CbcHmac, SetStatus);
        Argon = new CipherSession(EncryptionAlgorithm.Aes256Gcm, SetStatus, title: "Argon2id", lockPassphrase: true);
        Rsa = new RsaSession(SetStatus);
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Encryption}";
    }

    public CipherSession Gcm { get; }
    public CipherSession ChaCha { get; }
    public CipherSession Cbc { get; }
    public CipherSession Argon { get; }
    public RsaSession Rsa { get; }

    public string Identity => EncryptionHelper.Identity;
    public string ExportFolder => EncryptionHelper.DefaultExportDirectory(HelperLog.AppIds.Encryption);

    public string StartupSnippet =>
        "using var secret = EncryptionSecret.FromPassphrase(\"gallery-demo-only\");\n" +
        "var path = EncryptionHelper.SealFile(\"nathan.txt\", exportDir, secret);\n" +
        "EncryptionHelper.OpenFile(path, restoredDir, secret);";

    [ObservableProperty] private string validateTarget = "AES-256-GCM";
    [ObservableProperty] private string peekSummary = "Seal a string or file first.";
    [ObservableProperty] private string validateSummary = "";
    [ObservableProperty] private string originalName = "";
    [ObservableProperty] private string problemsText = "";
    [ObservableProperty] private string magicsText = "";

    public IReadOnlyList<string> ValidateTargets { get; } = ["AES-256-GCM", "ChaCha20-Poly1305", "AES-256-CBC + HMAC", "Argon2id", "RSA wrap"];

    private CipherSession Current => ValidateTarget.StartsWith("ChaCha", StringComparison.Ordinal)
        ? ChaCha
        : ValidateTarget.Contains("CBC", StringComparison.Ordinal)
            ? Cbc
            : ValidateTarget.StartsWith("Argon", StringComparison.Ordinal)
                ? Argon
                : Gcm;

    [RelayCommand]
    private void RunProbe()
    {
        var id = EncryptionHelper.Probe();
        StatusText = $"Probe complete · Identity={id}";
        RefreshLines();
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        var dir = ExportFolder;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        StatusText = dir;
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        var session = Current;
        if (string.IsNullOrWhiteSpace(session.EnvelopePath) || !File.Exists(session.EnvelopePath))
        {
            ValidateSummary = "Seal a string or file on a cipher tab first.";
            return;
        }

        try
        {
            using var secret = session.MakeSecret();
            var peek = EncryptionHelper.PeekFile(session.EnvelopePath);
            PeekSummary =
                $"suite {peek.SuiteVersion} · {peek.Algorithm} · {peek.FrameCount} frames · {peek.PlaintextLength} bytes · hidden={(peek.HasHiddenOriginalName ? "yes" : "no")}";
            MagicsText = Magics(session.EnvelopePath);
            var check = EncryptionHelper.ValidateFile(session.EnvelopePath, secret);
            OriginalName = check.OriginalFileName ?? "(none)";
            ValidateSummary =
                $"Vestigium={check.IsVestigium}  header={check.HeaderPresent}  trailer={check.TrailerPresent}  agree={check.HeaderTrailerAgree}  mac={check.StructuralMacValid}";
            ProblemsText = check.Problems.Count == 0 ? "(none)" : string.Join(Environment.NewLine, check.Problems);
            StatusText = $"Validated {Path.GetFileName(session.EnvelopePath)}";
        }
        catch (Exception ex)
        {
            ValidateSummary = ex.Message;
            StatusText = ex.Message;
        }

        RefreshLines();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task FlipMacAsync()
    {
        var session = Current;
        if (string.IsNullOrWhiteSpace(session.EnvelopePath) || !File.Exists(session.EnvelopePath))
        {
            ValidateSummary = "Seal a string or file first.";
            return;
        }

        try
        {
            var tampered = session.EnvelopePath + ".tampered";
            var bytes = File.ReadAllBytes(session.EnvelopePath);
            if (bytes.Length >= 20)
                bytes[^20] ^= 0xFF;
            File.WriteAllBytes(tampered, bytes);
            session.EnvelopePath = tampered;
            MagicsText = Magics(tampered);
            using var secret = session.MakeSecret();
            var check = EncryptionHelper.ValidateFile(tampered, secret);
            ValidateSummary = $"Flipped mac · StructuralMacValid={check.StructuralMacValid}";
            ProblemsText = check.Problems.Count == 0 ? "(none)" : string.Join(Environment.NewLine, check.Problems);
            try
            {
                var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                EncryptionHelper.OpenFile(tampered, tmp, secret);
            }
            catch (CryptographicException ex)
            {
                ValidateSummary += Environment.NewLine + ex.Message;
            }
        }
        catch (Exception ex)
        {
            ValidateSummary = ex.Message;
        }

        StatusText = ValidateSummary.Split('\n')[0];
        RefreshLines();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task WrongSecretAsync()
    {
        var session = Current;
        if (string.IsNullOrWhiteSpace(session.EnvelopePath) || !File.Exists(session.EnvelopePath))
        {
            ValidateSummary = "Seal a string or file first.";
            return;
        }

        try
        {
            using var wrong = session.UsePassphrase
                ? EncryptionSecret.FromPassphrase("wrong battery")
                : EncryptionSecret.FromKey(new byte[32]);
            var check = EncryptionHelper.ValidateFile(session.EnvelopePath, wrong);
            ValidateSummary = $"Wrong secret · StructuralMacValid={check.StructuralMacValid}";
            ProblemsText = check.Problems.Count == 0 ? "(none)" : string.Join(Environment.NewLine, check.Problems);
            try
            {
                EncryptionHelper.OpenFile(session.EnvelopePath, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), wrong);
            }
            catch (CryptographicException ex)
            {
                ValidateSummary += Environment.NewLine + ex.Message;
            }
        }
        catch (Exception ex)
        {
            ValidateSummary = ex.Message;
        }

        StatusText = "Wrong secret failed closed.";
        RefreshLines();
        await Task.CompletedTask;
    }

    private void SetStatus(string text) => StatusText = text;

    private static string Magics(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var head = Encoding.ASCII.GetString(bytes, 0, Math.Min(13, bytes.Length));
        var tail = bytes.Length >= 13
            ? Encoding.ASCII.GetString(bytes, bytes.Length - 13, 13)
            : "";
        return $"{head}  …  {tail}  ({bytes.Length} bytes)";
    }

    private static byte[] CreateDemoKey()
    {
        var key = new byte[32];
        key[0] = 7;
        key[31] = 9;
        return key;
    }
}

public sealed partial class CipherSession : ObservableObject
{
    private readonly Action<string> _status;

    public CipherSession(EncryptionAlgorithm algorithm, Action<string> status, string? title = null, bool lockPassphrase = false)
    {
        Algorithm = algorithm;
        Title = title ?? algorithm switch
        {
            EncryptionAlgorithm.ChaCha20Poly1305 => "ChaCha20-Poly1305",
            EncryptionAlgorithm.Aes256CbcHmac => "AES-256-CBC + HMAC",
            _ => "AES-256-GCM"
        };
        LockPassphrase = lockPassphrase;
        UsePassphrase = lockPassphrase;
        Passphrase = "gallery-demo-only";
        ShredChoice = "Keep original";
        _status = status;
        PlainText = "token from Vestigium";
    }

    public EncryptionAlgorithm Algorithm { get; }
    public string Title { get; }
    public bool LockPassphrase { get; }
    public bool ShowPassphraseToggle => !LockPassphrase;
    public bool PassphraseBoxVisible => UsePassphrase;

    public IReadOnlyList<string> ShredChoices { get; } =
        ["Keep original", "3-pass random + zero", "7-pass random + zero"];

    [ObservableProperty] private string plainText = "";
    [ObservableProperty] private string sealedBase64 = "";
    [ObservableProperty] private string openedText = "";
    [ObservableProperty] private bool usePassphrase;
    [ObservableProperty] private string passphrase = "gallery-demo-only";
    [ObservableProperty] private string shredChoice = "Keep original";
    [ObservableProperty] private string shredSummary = "";
    [ObservableProperty] private string sourcePath = "";
    [ObservableProperty] private string sourceName = "";
    [ObservableProperty] private string envelopePath = "";
    [ObservableProperty] private string restoredPath = "";
    [ObservableProperty] private string restoredPreview = "";
    [ObservableProperty] private string peekLine = "";
    [ObservableProperty] private string errorText = "";

    public string VisibleName
    {
        get
        {
            var name = string.IsNullOrWhiteSpace(SourceName) ? "nathan.txt" : SourceName;
            var stem = Path.GetFileNameWithoutExtension(name);
            if (string.IsNullOrWhiteSpace(stem))
                stem = "file";
            return stem + (UsePassphrase ? ".argon" : ".aes");
        }
    }

    public string SealFileCaption => $"Seal → {VisibleName}";

    public string SourceLine => string.IsNullOrWhiteSpace(SourceName)
        ? "No file selected yet. Choose one or use nathan.txt."
        : $"{SourceName} → {VisibleName}";

    public string OpenedCaption => string.IsNullOrEmpty(OpenedText) ? "" : $"Opened: {OpenedText}";

    public string RestoredCaption => string.IsNullOrWhiteSpace(RestoredPath)
        ? ""
        : $"Restored {Path.GetFileName(RestoredPath)}";

    public string SecretCaption => UsePassphrase ? "Passphrase → .argon" : "Raw key → .aes";

    private SecureDeleteMode ShredMode =>
        ShredChoice.StartsWith("7", StringComparison.Ordinal) ? SecureDeleteMode.SevenPass
        : ShredChoice.StartsWith("3", StringComparison.Ordinal) ? SecureDeleteMode.ThreePass
        : SecureDeleteMode.Keep;

    partial void OnUsePassphraseChanged(bool value)
    {
        OnPropertyChanged(nameof(VisibleName));
        OnPropertyChanged(nameof(SecretCaption));
        OnPropertyChanged(nameof(SealFileCaption));
        OnPropertyChanged(nameof(SourceLine));
        OnPropertyChanged(nameof(PassphraseBoxVisible));
    }

    partial void OnSourceNameChanged(string value)
    {
        OnPropertyChanged(nameof(VisibleName));
        OnPropertyChanged(nameof(SealFileCaption));
        OnPropertyChanged(nameof(SourceLine));
    }

    partial void OnOpenedTextChanged(string value) => OnPropertyChanged(nameof(OpenedCaption));

    partial void OnRestoredPathChanged(string value) => OnPropertyChanged(nameof(RestoredCaption));

    public EncryptionSecret MakeSecret() => UsePassphrase
        ? EncryptionSecret.FromPassphrase(string.IsNullOrWhiteSpace(Passphrase) ? "gallery-demo-only" : Passphrase)
        : EncryptionSecret.FromKey(MainViewModelKey);

    // Same demo key as MainViewModel (7 at 0, 9 at 31).
    private static readonly byte[] MainViewModelKey = CreateKey();

    private static byte[] CreateKey()
    {
        var key = new byte[32];
        key[0] = 7;
        key[31] = 9;
        return key;
    }

    [RelayCommand]
    private async Task SealStringAsync()
    {
        ErrorText = "";
        try
        {
            using var secret = MakeSecret();
            var text = PlainText ?? "";
            var alg = Algorithm;
            SealedBase64 = await Task.Run(() => EncryptionHelper.SealString(text, secret, alg));
            OpenedText = "";
            var blob = Convert.FromBase64String(SealedBase64);
            EnvelopePath = WriteBlob("string", blob);
            var peek = EncryptionHelper.PeekFile(EnvelopePath);
            PeekLine = $"{peek.Algorithm} · {peek.FrameCount} frames · {peek.PlaintextLength} bytes · hidden={peek.HasHiddenOriginalName}";
            _status($"Sealed string · {blob.Length} bytes · {Title}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            _status(ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenStringAsync()
    {
        ErrorText = "";
        if (string.IsNullOrWhiteSpace(SealedBase64))
        {
            ErrorText = "Seal the string first.";
            return;
        }

        try
        {
            using var secret = MakeSecret();
            var sealedText = SealedBase64;
            OpenedText = await Task.Run(() => EncryptionHelper.OpenString(sealedText, secret));
            _status($"Opened string · {OpenedText.Length} chars");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            _status(ex.Message);
        }
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Choose a file to seal",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dlg.ShowDialog() != true)
            return;
        SourcePath = dlg.FileName;
        SourceName = Path.GetFileName(dlg.FileName);
        RestoredPath = "";
        RestoredPreview = "";
        EnvelopePath = "";
        ShredSummary = "";
        OnPropertyChanged(nameof(VisibleName));
        _status($"Selected {SourceName}");
    }

    [RelayCommand]
    private void UseNathanSample()
    {
        var inbox = Path.Combine(Path.GetTempPath(), "vestigium-encryption-demo");
        Directory.CreateDirectory(inbox);
        SourcePath = Path.Combine(inbox, "nathan.txt");
        File.WriteAllText(SourcePath, "hello from Vestigium");
        SourceName = "nathan.txt";
        RestoredPath = "";
        RestoredPreview = "";
        EnvelopePath = "";
        ShredSummary = "";
        OnPropertyChanged(nameof(VisibleName));
        _status("Sample nathan.txt ready.");
    }

    [RelayCommand]
    private async Task SealFileAsync()
    {
        ErrorText = "";
        if (string.IsNullOrWhiteSpace(SourcePath) || !File.Exists(SourcePath))
            UseNathanSample();
        if (string.IsNullOrWhiteSpace(SourcePath) || !File.Exists(SourcePath))
        {
            ErrorText = "Choose a file first.";
            return;
        }

        try
        {
            using var secret = MakeSecret();
            var source = SourcePath;
            var destDir = EncryptionHelper.DefaultExportDirectory(HelperLog.AppIds.Encryption);
            Directory.CreateDirectory(destDir);
            var alg = Algorithm;
            var shred = ShredMode;
            EnvelopePath = await Task.Run(() => EncryptionHelper.SealFile(source, destDir, secret, alg, shred));
            RestoredPath = "";
            RestoredPreview = "";
            var peek = EncryptionHelper.PeekFile(EnvelopePath);
            PeekLine = $"{Path.GetFileName(EnvelopePath)} · {peek.Algorithm} · {peek.FrameCount} frames · hidden={peek.HasHiddenOriginalName}";
            if (shred != SecureDeleteMode.Keep)
            {
                var gone = !File.Exists(source);
                ShredSummary = gone
                    ? $"Shredded {SourceName} ({(int)shred} random + zero)."
                    : "Shred failed — original still on disk.";
                if (gone)
                    SourcePath = "";
            }
            else
            {
                ShredSummary = "Original file kept.";
            }
            _status($"Sealed {SourceName} → {Path.GetFileName(EnvelopePath)}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            _status(ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        ErrorText = "";
        if (string.IsNullOrWhiteSpace(EnvelopePath) || !File.Exists(EnvelopePath))
        {
            ErrorText = "Seal a file first.";
            return;
        }

        try
        {
            using var secret = MakeSecret();
            var source = EnvelopePath;
            var restoredDir = Path.Combine(EncryptionHelper.DefaultExportDirectory(HelperLog.AppIds.Encryption), "restored");
            Directory.CreateDirectory(restoredDir);
            RestoredPath = await Task.Run(() => EncryptionHelper.OpenFile(source, restoredDir + Path.DirectorySeparatorChar, secret));
            RestoredPreview = Preview(RestoredPath);
            _status($"Opened → {Path.GetFileName(RestoredPath)}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            _status(ex.Message);
        }
    }

    private string WriteBlob(string stem, byte[] blob)
    {
        var dir = EncryptionHelper.DefaultExportDirectory(HelperLog.AppIds.Encryption);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{stem}-{Algorithm.ToString().ToLowerInvariant()}.bin");
        File.WriteAllBytes(path, blob);
        return path;
    }

    private static string Preview(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length == 0)
            return "(empty file)";
        if (bytes.Any(b => b == 0) && bytes.Length > 8)
            return $"(binary · {bytes.Length} bytes)";
        var text = Encoding.UTF8.GetString(bytes);
        return text.Length > 800 ? text[..800] + "…" : text;
    }
}

public sealed partial class RsaSession : ObservableObject
{
    private readonly Action<string> _status;
    private readonly EncryptionKeyRing _ring = EncryptionKeyRing.Create("Ops ring");
    private EncryptionKeyRecord? _ops;
    private EncryptionRsaKey? _appXSlip;
    private EncryptionRsaKey? _appYSlip;
    private EncryptionKeyRecord? _appX;
    private EncryptionKeyRecord? _appY;

    public RsaSession(Action<string> status)
    {
        _status = status;
        PlainText = "token from Vestigium";
        Recipient = "CompanyX / AppX";
        OpenAs = "AppX";
        TokenTarget = "AppX";
        AlsoWrapToOps = true;
        UseOverride = false;
        OverrideBy = "qa-operator";
        OverrideReason = "restore for incident";
        KeySummary = "Generate an Ops pair, then Issue AppX and AppY (2048-bit gallery keys).";
        TokenStatusLine = "Issue a token, then Enable / Disable / Expire.";
    }

    public IReadOnlyList<string> Recipients { get; } = ["CompanyX / AppX", "CompanyX / AppY"];
    public IReadOnlyList<string> OpenAsChoices { get; } = ["Ops", "AppX", "AppY"];
    public IReadOnlyList<string> TokenTargets { get; } = ["Ops", "AppX", "AppY"];

    [ObservableProperty] private string plainText = "";
    [ObservableProperty] private string sealedBase64 = "";
    [ObservableProperty] private string openedText = "";
    [ObservableProperty] private string recipient = "CompanyX / AppX";
    [ObservableProperty] private string openAs = "AppX";
    [ObservableProperty] private string tokenTarget = "AppX";
    [ObservableProperty] private bool alsoWrapToOps = true;
    [ObservableProperty] private bool useOverride;
    [ObservableProperty] private string overrideBy = "qa-operator";
    [ObservableProperty] private string overrideReason = "restore for incident";
    [ObservableProperty] private string keySummary = "";
    [ObservableProperty] private string tokenStatusLine = "";
    [ObservableProperty] private string peekLine = "";
    [ObservableProperty] private string envelopePath = "";
    [ObservableProperty] private string restoredPath = "";
    [ObservableProperty] private string errorText = "";

    public string OpenedCaption => string.IsNullOrEmpty(OpenedText) ? "" : $"Opened: {OpenedText}";
    public string RestoredCaption => string.IsNullOrWhiteSpace(RestoredPath) ? "" : $"Restored {Path.GetFileName(RestoredPath)}";

    partial void OnOpenedTextChanged(string value) => OnPropertyChanged(nameof(OpenedCaption));
    partial void OnRestoredPathChanged(string value) => OnPropertyChanged(nameof(RestoredCaption));
    partial void OnTokenTargetChanged(string value) => TokenStatusLine = StatusLine(SelectedToken());

    private void RefreshKeys()
    {
        KeySummary =
            $"Ops {Short(_ops)}  ·  AppX {Short(_appX)}  ·  AppY {Short(_appY)}";
        TokenStatusLine = StatusLine(SelectedToken());
    }

    private static string Short(EncryptionKeyRecord? row)
        => row is null ? "(none)" : row.ThumbprintSha256[..12] + "… " + row.Status;

    private EncryptionKeyRecord? SelectedToken()
        => TokenTarget.Equals("Ops", StringComparison.OrdinalIgnoreCase) ? _ops
            : TokenTarget.Contains("AppY", StringComparison.Ordinal) ? _appY
            : _appX;

    private static string StatusLine(EncryptionKeyRecord? row)
        => row is null
            ? "Issue a token, then Enable / Disable / Expire."
            : $"{row.ThumbprintSha256[..12]}…  {row.Status}";

    private EncryptionKeyOverride? MaybeOverride()
        => UseOverride ? EncryptionKeyOverride.Request(OverrideBy, OverrideReason) : null;

    [RelayCommand]
    private async Task GenerateOpsAsync()
    {
        ErrorText = "";
        try
        {
            var row = await Task.Run(() => _ring.AddPair("Ops receive", "Ops", "Wilkinson", application: "Gallery", keyBits: 2048));
            _ops = row;
            RefreshKeys();
            _status("Ops pair ready on the ring (2048-bit).");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task IssueAppXAsync()
    {
        ErrorText = "";
        try
        {
            var issued = await Task.Run(() =>
                _ring.Issue("Company X AppX", "AppX wrap", "CompanyX", application: "ApplicationX", keyBits: 2048));
            _appXSlip?.Dispose();
            _appX = issued.Contact;
            _appXSlip = issued.PrivateExport;
            RefreshKeys();
            _status("Issued CompanyX / ApplicationX (public contact kept; slip in gallery).");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task IssueAppYAsync()
    {
        ErrorText = "";
        try
        {
            var issued = await Task.Run(() =>
                _ring.Issue("Company X AppY", "AppY wrap", "CompanyX", application: "ApplicationY", keyBits: 2048));
            _appYSlip?.Dispose();
            _appY = issued.Contact;
            _appYSlip = issued.PrivateExport;
            RefreshKeys();
            _status("Issued CompanyX / ApplicationY (public contact kept; slip in gallery).");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SealStringAsync()
    {
        ErrorText = "";
        var contact = ContactRecord();
        if (contact is null)
        {
            ErrorText = "Issue AppX or AppY first.";
            return;
        }
        if (AlsoWrapToOps && _ops is null)
        {
            ErrorText = "Generate the Ops pair first, or turn off also wrap to Ops.";
            return;
        }

        try
        {
            var text = PlainText ?? "";
            var also = AlsoWrapToOps ? _ops?.Key : null;
            var ov = MaybeOverride();
            SealedBase64 = await Task.Run(() =>
            {
                var key = _ring.RequireForSeal(contact, ov);
                return EncryptionHelper.SealString(text, [key], alsoWrapTo: also);
            });
            OpenedText = "";
            var blob = Convert.FromBase64String(SealedBase64);
            var dir = EncryptionHelper.DefaultExportDirectory(HelperLog.AppIds.Encryption);
            Directory.CreateDirectory(dir);
            EnvelopePath = Path.Combine(dir, "rsa-string.bin");
            File.WriteAllBytes(EnvelopePath, blob);
            var peek = EncryptionHelper.PeekFile(EnvelopePath);
            PeekLine = $"suite {peek.SuiteVersion} · wraps={peek.WrapCount} · {string.Join(", ", peek.WrapThumbprints.Select(t => t[..12] + "…"))}";
            _status($"Sealed string · suite {peek.SuiteVersion} · {peek.WrapCount} wrap(s)");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            _status(ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenStringAsync()
    {
        ErrorText = "";
        if (string.IsNullOrWhiteSpace(SealedBase64))
        {
            ErrorText = "Seal the string first.";
            return;
        }

        try
        {
            var sealedText = SealedBase64;
            var ov = MaybeOverride();
            OpenedText = await Task.Run(() => OpenSealed(sealedText, ov));
            _status($"Opened as {OpenAs} · {OpenedText.Length} chars");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            _status(ex.Message);
        }
    }

    [RelayCommand]
    private async Task SealFileAsync()
    {
        ErrorText = "";
        var contact = ContactRecord();
        if (contact is null)
        {
            ErrorText = "Issue AppX or AppY first.";
            return;
        }
        if (AlsoWrapToOps && _ops is null)
        {
            ErrorText = "Generate the Ops pair first, or turn off also wrap to Ops.";
            return;
        }

        try
        {
            var inbox = Path.Combine(Path.GetTempPath(), "vestigium-encryption-demo");
            Directory.CreateDirectory(inbox);
            var src = Path.Combine(inbox, "nathan.txt");
            File.WriteAllText(src, "hello from Vestigium");
            var destDir = EncryptionHelper.DefaultExportDirectory(HelperLog.AppIds.Encryption);
            Directory.CreateDirectory(destDir);
            var also = AlsoWrapToOps ? _ops?.Key : null;
            var ov = MaybeOverride();
            EnvelopePath = await Task.Run(() =>
            {
                var key = _ring.RequireForSeal(contact, ov);
                return EncryptionHelper.SealFile(src, destDir, [key], alsoWrapTo: also);
            });
            RestoredPath = "";
            var peek = EncryptionHelper.PeekFile(EnvelopePath);
            PeekLine = $"{Path.GetFileName(EnvelopePath)} · suite {peek.SuiteVersion} · wraps={peek.WrapCount} · hidden={peek.HasHiddenOriginalName}";
            _status($"Sealed nathan.txt → {Path.GetFileName(EnvelopePath)}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            _status(ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        ErrorText = "";
        if (string.IsNullOrWhiteSpace(EnvelopePath) || !File.Exists(EnvelopePath))
        {
            ErrorText = "Seal a file first.";
            return;
        }

        try
        {
            var source = EnvelopePath;
            var restoredDir = Path.Combine(EncryptionHelper.DefaultExportDirectory(HelperLog.AppIds.Encryption), "restored");
            Directory.CreateDirectory(restoredDir);
            var ov = MaybeOverride();
            RestoredPath = await Task.Run(() => OpenSealedFile(source, restoredDir + Path.DirectorySeparatorChar, ov));
            _status($"Opened as {OpenAs} → {Path.GetFileName(RestoredPath)}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            _status(ex.Message);
        }
    }

    [RelayCommand]
    private void EnableToken() => ChangeToken(row => _ring.Enable(row), "Enabled");

    [RelayCommand]
    private void DisableToken() => ChangeToken(row => _ring.Disable(row), "Disabled");

    [RelayCommand]
    private void ExpireToken() => ChangeToken(row => _ring.Expire(row), "Expired");

    private void ChangeToken(Action<EncryptionKeyRecord> change, string verb)
    {
        ErrorText = "";
        var row = SelectedToken();
        if (row is null)
        {
            ErrorText = "Generate or issue the token first.";
            return;
        }

        try
        {
            change(row);
            RefreshKeys();
            _status($"{verb} {TokenTarget} · warning written to JSONL.");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            _status(ex.Message);
        }
    }

    private EncryptionKeyRecord? ContactRecord()
        => Recipient.Contains("AppY", StringComparison.Ordinal) ? _appY : _appX;

    private string OpenSealed(string sealedText, EncryptionKeyOverride? keyOverride)
    {
        if (OpenAs.Equals("Ops", StringComparison.OrdinalIgnoreCase))
        {
            if (_ops is null)
                throw new InvalidOperationException("Generate the Ops pair first.");
            return EncryptionHelper.OpenString(sealedText, _ring, keyOverride);
        }

        var slip = OpenAs.Equals("AppY", StringComparison.OrdinalIgnoreCase) ? _appYSlip : _appXSlip;
        if (slip is null)
            throw new InvalidOperationException("Issue the Open-as key first.");
        return EncryptionHelper.OpenString(sealedText, slip);
    }

    private string OpenSealedFile(string source, string dest, EncryptionKeyOverride? keyOverride)
    {
        if (OpenAs.Equals("Ops", StringComparison.OrdinalIgnoreCase))
        {
            if (_ops is null)
                throw new InvalidOperationException("Generate the Ops pair first.");
            return EncryptionHelper.OpenFile(source, dest, _ring, keyOverride);
        }

        var slip = OpenAs.Equals("AppY", StringComparison.OrdinalIgnoreCase) ? _appYSlip : _appXSlip;
        if (slip is null)
            throw new InvalidOperationException("Issue the Open-as key first.");
        return EncryptionHelper.OpenFile(source, dest, slip);
    }
}
