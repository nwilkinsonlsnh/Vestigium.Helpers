using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Helpers.Hashing.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    public MainViewModel()
    {
        Sha256 = new HashPane(HashingAlgorithm.Sha256, SetStatus);
        Sha2 = new HashPane(HashingAlgorithm.Sha384, SetStatus, [HashingAlgorithm.Sha384, HashingAlgorithm.Sha512]);
        Sha3 = new HashPane(HashingAlgorithm.Sha3_256, SetStatus, [HashingAlgorithm.Sha3_256, HashingAlgorithm.Sha3_384, HashingAlgorithm.Sha3_512]);
        Interop = new HashPane(HashingAlgorithm.Md5, SetStatus, [HashingAlgorithm.Md5, HashingAlgorithm.Sha1], interop: true);
        Checksum = new ChecksumPane(SetStatus);
        Hmac = new HmacPane(SetStatus);
        Password = new PasswordPane(SetStatus);
        HexText = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Hashing}";
    }

    public HashPane Sha256 { get; }
    public HashPane Sha2 { get; }
    public HashPane Sha3 { get; }
    public HashPane Interop { get; }
    public ChecksumPane Checksum { get; }
    public HmacPane Hmac { get; }
    public PasswordPane Password { get; }

    public string Identity => HashingHelper.Identity;

    public string StartupSnippet =>
        "var hex = HashingHelper.HashString(\"abc\");\n" +
        "var crc = HashingHelper.ChecksumCrc32(\"123456789\");\n" +
        "using var key = HmacKey.Generate();\n" +
        "var mac = HashingHelper.HmacString(msg, key, HmacAlgorithm.Sha384);";

    [ObservableProperty] private string hexText = "";
    [ObservableProperty] private string base64Text = "";
    [ObservableProperty] private string convertCaption = "Paste hex or Base64, or load the SHA-256 of abc.";
    [ObservableProperty] private string convertError = "";

    [RelayCommand]
    private void RunProbe()
    {
        var id = HashingHelper.Probe();
        StatusText = $"Probe complete · Identity={id}";
        RefreshLines();
    }

    [RelayCommand]
    private void LoadAbc()
    {
        HexText = HashingHelper.HashString("abc");
        ConvertError = "";
        ConvertCaption = "Loaded SHA-256(\"abc\") hex.";
        RefreshLines();
    }

    [RelayCommand]
    private void HexToBase64()
    {
        try
        {
            Base64Text = HashingConvert.HexToBase64(HexText);
            ConvertError = "";
            ConvertCaption = "Hex → Base64.";
            RefreshLines();
        }
        catch (Exception ex)
        {
            ConvertError = ex.Message;
        }
    }

    [RelayCommand]
    private void HexToBase64Url()
    {
        try
        {
            Base64Text = HashingConvert.HexToBase64Url(HexText);
            ConvertError = "";
            ConvertCaption = "Hex → Base64 URL.";
            RefreshLines();
        }
        catch (Exception ex)
        {
            ConvertError = ex.Message;
        }
    }

    [RelayCommand]
    private void Base64ToHex()
    {
        try
        {
            HexText = HashingConvert.Base64ToHex(Base64Text);
            ConvertError = "";
            ConvertCaption = "Base64 → hex lower.";
            RefreshLines();
        }
        catch (Exception ex)
        {
            ConvertError = ex.Message;
        }
    }

    [RelayCommand]
    private void Base64UrlToHex()
    {
        try
        {
            HexText = HashingConvert.Base64UrlToHex(Base64Text);
            ConvertError = "";
            ConvertCaption = "Base64 URL → hex lower.";
            RefreshLines();
        }
        catch (Exception ex)
        {
            ConvertError = ex.Message;
        }
    }

    private void SetStatus(string text)
    {
        StatusText = text;
        RefreshLines();
    }
}

public sealed partial class HashPane : ObservableObject
{
    private readonly Action<string> _status;
    private readonly Dictionary<string, HashingAlgorithm> _map = [];

    public HashPane(HashingAlgorithm initial, Action<string> status, HashingAlgorithm[]? choices = null, bool interop = false)
    {
        _status = status;
        Algorithm = initial;
        Title = HashingHelper.AlgorithmName(initial);
        Blurb = interop
            ? "Interop only. Never the default. Use these to read old vendor manifests."
            : initial is HashingAlgorithm.Sha3_256 or HashingAlgorithm.Sha3_384 or HashingAlgorithm.Sha3_512
                ? "SHA-3 is not SHA-256 version 3. Probe the OS: SHA3_256.IsSupported. Default remains SHA-256."
                : "Round-trip a UTF-8 string and a file. Default print is lowercase hex. Base64 is a converter.";
        Warning = interop ? "MD5 and SHA-1 are broken for security. Do not authenticate captures with them." : "";
        var list = choices ?? [initial];
        AlgorithmChoices = list.Select(HashingHelper.AlgorithmName).ToList();
        foreach (var alg in list)
            _map[HashingHelper.AlgorithmName(alg)] = alg;
        AlgorithmChoice = HashingHelper.AlgorithmName(initial);
        InputText = "abc";
        SourceLine = "No file selected.";
        FileDigestLine = "";
        VerifyCaption = "";
        ErrorText = "";
    }

    public string Title { get; }
    public string Blurb { get; }
    public string Warning { get; }
    public IReadOnlyList<string> AlgorithmChoices { get; }
    public HashingAlgorithm Algorithm { get; private set; }

    [ObservableProperty] private string algorithmChoice = "";
    [ObservableProperty] private string inputText = "abc";
    [ObservableProperty] private string digestHex = "";
    [ObservableProperty] private string digestBase64 = "";
    [ObservableProperty] private string verifyCaption = "";
    [ObservableProperty] private string sourceLine = "";
    [ObservableProperty] private string fileDigestLine = "";
    [ObservableProperty] private string errorText = "";
    private string? _filePath;

    partial void OnAlgorithmChoiceChanged(string value)
    {
        if (_map.TryGetValue(value, out var alg))
        {
            Algorithm = alg;
            TitleChange(alg);
        }
    }

    private void TitleChange(HashingAlgorithm alg)
    {
        // Title is get-only for the header; digest fields reset.
        DigestHex = "";
        DigestBase64 = "";
        VerifyCaption = HashingHelper.IsSupported(alg) ? "" : $"{HashingHelper.AlgorithmName(alg)} is not available on this OS.";
    }

    [RelayCommand]
    private void HashString()
    {
        try
        {
            ErrorText = "";
            DigestHex = HashingHelper.HashString(InputText ?? "", Algorithm, HashingTextFormat.HexLower);
            DigestBase64 = HashingConvert.HexToBase64(DigestHex);
            VerifyCaption = "";
            _status($"{HashingHelper.AlgorithmName(Algorithm)} string · {DigestHex.Length} hex chars");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private void VerifyString()
    {
        try
        {
            ErrorText = "";
            var ok = HashingHelper.VerifyString(InputText ?? "", DigestHex, Algorithm);
            VerifyCaption = ok ? "Match." : "No match.";
            _status($"VerifyString {(ok ? "ok" : "failed")}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog { Title = "Choose a file to hash" };
        if (dlg.ShowDialog() == true)
        {
            _filePath = dlg.FileName;
            SourceLine = Path.GetFileName(_filePath);
        }
    }

    [RelayCommand]
    private void UseNathanSample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashingDemo");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(_filePath, "hello from Vestigium hashing");
        SourceLine = "nathan.txt (temp sample)";
        _status("Wrote nathan.txt sample.");
    }

    [RelayCommand]
    private void HashFile()
    {
        try
        {
            ErrorText = "";
            if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
            {
                ErrorText = "Choose a file first.";
                return;
            }

            var hex = HashingHelper.HashFile(_filePath, Algorithm);
            FileDigestLine = hex;
            DigestHex = hex;
            DigestBase64 = HashingConvert.HexToBase64(hex);
            _status($"{HashingHelper.AlgorithmName(Algorithm)} file · {hex}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }
}

public sealed partial class ChecksumPane : ObservableObject
{
    private readonly Action<string> _status;
    private readonly Dictionary<string, ChecksumAlgorithm> _map = [];

    public ChecksumPane(Action<string> status)
    {
        _status = status;
        Algorithm = ChecksumAlgorithm.Crc32;
        var list = new[]
        {
            ChecksumAlgorithm.Crc32,
            ChecksumAlgorithm.Crc64,
            ChecksumAlgorithm.XxHash32,
            ChecksumAlgorithm.XxHash64,
            ChecksumAlgorithm.XxHash3,
        };
        AlgorithmChoices = list.Select(HashingHelper.ChecksumName).ToList();
        foreach (var alg in list)
            _map[HashingHelper.ChecksumName(alg)] = alg;
        AlgorithmChoice = HashingHelper.ChecksumName(Algorithm);
        InputText = "123456789";
        SourceLine = "No file selected.";
        FileDigestLine = "";
        VerifyCaption = "";
        ErrorText = "";
    }

    public IReadOnlyList<string> AlgorithmChoices { get; }
    public ChecksumAlgorithm Algorithm { get; private set; }

    [ObservableProperty] private string algorithmChoice = "";
    [ObservableProperty] private string inputText = "123456789";
    [ObservableProperty] private string digestHex = "";
    [ObservableProperty] private string verifyCaption = "";
    [ObservableProperty] private string sourceLine = "";
    [ObservableProperty] private string fileDigestLine = "";
    [ObservableProperty] private string errorText = "";
    private string? _filePath;

    public string NamedMethod => Algorithm switch
    {
        ChecksumAlgorithm.Crc32 => "ChecksumCrc32",
        ChecksumAlgorithm.Crc64 => "ChecksumCrc64",
        ChecksumAlgorithm.XxHash64 => "ChecksumXxHash",
        _ => "ChecksumString",
    };

    partial void OnAlgorithmChoiceChanged(string value)
    {
        if (_map.TryGetValue(value, out var alg))
        {
            Algorithm = alg;
            DigestHex = "";
            VerifyCaption = "";
            OnPropertyChanged(nameof(NamedMethod));
        }
    }

    [RelayCommand]
    private void ChecksumString()
    {
        try
        {
            ErrorText = "";
            DigestHex = HashingHelper.ChecksumString(InputText ?? "", Algorithm, HashingTextFormat.HexLower);
            VerifyCaption = "";
            _status($"{HashingHelper.ChecksumName(Algorithm)} string · {DigestHex}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private void VerifyString()
    {
        try
        {
            ErrorText = "";
            var ok = HashingHelper.VerifyChecksumString(InputText ?? "", DigestHex, Algorithm);
            VerifyCaption = ok ? "Match." : "No match.";
            _status($"VerifyChecksumString {(ok ? "ok" : "failed")}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog { Title = "Choose a file to checksum" };
        if (dlg.ShowDialog() == true)
        {
            _filePath = dlg.FileName;
            SourceLine = Path.GetFileName(_filePath);
        }
    }

    [RelayCommand]
    private void UseNathanSample()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashingDemo");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(_filePath, "hello from Vestigium hashing");
        SourceLine = "nathan.txt (temp sample)";
        _status("Wrote nathan.txt sample.");
    }

    [RelayCommand]
    private void ChecksumFile()
    {
        try
        {
            ErrorText = "";
            if (string.IsNullOrWhiteSpace(_filePath) || !File.Exists(_filePath))
            {
                ErrorText = "Choose a file first.";
                return;
            }

            var hex = HashingHelper.ChecksumFile(_filePath, Algorithm);
            FileDigestLine = hex;
            DigestHex = hex;
            _status($"{HashingHelper.ChecksumName(Algorithm)} file · {hex}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }
}

public sealed partial class HmacPane : ObservableObject
{
    private readonly Action<string> _status;
    private HmacKey? _generated;

    public HmacPane(Action<string> status)
    {
        _status = status;
        Algorithm = HmacAlgorithm.Sha256;
        var list = new[] { HmacAlgorithm.Sha256, HmacAlgorithm.Sha384, HmacAlgorithm.Sha512 };
        AlgorithmChoices = list.Select(HashingHelper.HmacName).ToList();
        foreach (var alg in list)
            _map[HashingHelper.HmacName(alg)] = alg;
        AlgorithmChoice = HashingHelper.HmacName(Algorithm);
        SizeChoices = ["16 bytes", "32 bytes (default)", "64 bytes", "128 bytes"];
        SizeChoice = SizeChoices[1];
        InputText = "Hi There";
        TypedSecret = "";
        KeyBase64 = "";
        MacHex = "";
        VerifyCaption = "";
        ErrorText = "";
    }

    public IReadOnlyList<string> SizeChoices { get; }
    public IReadOnlyList<string> AlgorithmChoices { get; }
    public HmacAlgorithm Algorithm { get; private set; }
    private readonly Dictionary<string, HmacAlgorithm> _map = [];

    [ObservableProperty] private string algorithmChoice = "";
    [ObservableProperty] private string sizeChoice = "";
    [ObservableProperty] private string keyBase64 = "";
    [ObservableProperty] private string typedSecret = "";
    [ObservableProperty] private string inputText = "";
    [ObservableProperty] private string macHex = "";
    [ObservableProperty] private string verifyCaption = "";
    [ObservableProperty] private string errorText = "";

    partial void OnAlgorithmChoiceChanged(string value)
    {
        if (_map.TryGetValue(value, out var alg))
        {
            Algorithm = alg;
            MacHex = "";
            VerifyCaption = "";
        }
    }

    [RelayCommand]
    private void Generate()
    {
        _generated?.Dispose();
        _generated = HmacKey.Generate(ParseSize());
        KeyBase64 = _generated.ToBase64();
        TypedSecret = "";
        _status($"Generated HMAC key · {_generated.Length} bytes");
    }

    [RelayCommand]
    private void LoadRfc4231()
    {
        TypedSecret = "";
        KeyBase64 = Convert.ToBase64String(Enumerable.Repeat((byte)0x0b, 20).ToArray());
        InputText = "Hi There";
        MacHex = "";
        VerifyCaption = "";
        _status("Loaded RFC 4231 case 1 key and message.");
    }

    [RelayCommand]
    private void HmacString()
    {
        try
        {
            ErrorText = "";
            using var key = ResolveKey();
            MacHex = HashingHelper.HmacString(InputText ?? "", key, Algorithm);
            VerifyCaption = "";
            _status($"{HashingHelper.HmacName(Algorithm)} complete.");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private void Verify()
    {
        try
        {
            ErrorText = "";
            using var key = ResolveKey();
            var ok = HashingHelper.VerifyHmacString(InputText ?? "", key, MacHex, Algorithm);
            VerifyCaption = ok ? "Match." : "No match.";
            _status($"VerifyHmacString {(ok ? "ok" : "failed")}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    private HmacKey ResolveKey()
    {
        if (!string.IsNullOrWhiteSpace(TypedSecret))
            return HmacKey.FromString(TypedSecret.Trim());
        if (!string.IsNullOrWhiteSpace(KeyBase64))
            return HmacKey.FromBase64(KeyBase64);
        throw new InvalidOperationException("Generate a key or type a secret of at least 16 UTF-8 bytes.");
    }

    private HmacKeySize ParseSize() => SizeChoice.StartsWith("16", StringComparison.Ordinal)
        ? HmacKeySize.Bytes16
        : SizeChoice.StartsWith("64", StringComparison.Ordinal)
            ? HmacKeySize.Bytes64
            : SizeChoice.StartsWith("128", StringComparison.Ordinal)
                ? HmacKeySize.Bytes128
                : HmacKeySize.Bytes32;
}

public sealed partial class PasswordPane : ObservableObject
{
    private readonly Action<string> _status;

    public PasswordPane(Action<string> status)
    {
        _status = status;
        PasswordText = "gallery-demo-only";
        StoredPhc = "";
        VerifyCaption = "";
        ErrorText = "";
    }

    [ObservableProperty] private string passwordText = "";
    [ObservableProperty] private string storedPhc = "";
    [ObservableProperty] private string verifyCaption = "";
    [ObservableProperty] private string errorText = "";

    [RelayCommand]
    private void Hash()
    {
        try
        {
            ErrorText = "";
            StoredPhc = HashingHelper.HashPassword(PasswordText ?? "");
            VerifyCaption = "Verifier stored. JSONL does not contain this string.";
            _status("Password hashed (PHC).");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }

    [RelayCommand]
    private void Verify()
    {
        try
        {
            ErrorText = "";
            var ok = HashingHelper.VerifyPassword(PasswordText ?? "", StoredPhc);
            VerifyCaption = ok ? "Verify ok." : "Verify failed.";
            _status($"Password verify {(ok ? "ok" : "failed")}");
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }
}
