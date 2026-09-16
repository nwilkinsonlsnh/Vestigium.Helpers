using System.Reflection;
using System.Security.Cryptography;

namespace Vestigium.Helpers.Tests.Support;

/// <summary>
/// Seeds %USERPROFILE%\Documents\Xml from gold files shipped as
/// MSBuild Content and EmbeddedResource. Tests never write the gold copies.
/// </summary>
public static class XmlContentSeeder
{
    public const string ResourcePrefix = "Vestigium.Helpers.Xml.Tests.Content.Xml.";

    public static readonly string[] RequiredFiles =
    [
        "DevicePreparationDDF.xml",
        "DMClient_DDF.xml",
        "diagwrn.xml",
        "ipcfg.xml",
        "osinfo.xml",
        "ReAgent.xml",
    ];

    private static readonly object Gate = new();
    private static bool _seeded;

    public static string WorkDirectory
    {
        get
        {
            var overridePath = Environment.GetEnvironmentVariable("VESTIGIUM_XML_TESTAREA");
            if (!string.IsNullOrWhiteSpace(overridePath))
                return Path.GetFullPath(overridePath);

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Xml");
        }
    }

    public static string ScratchDirectory => Path.Combine(WorkDirectory, "_scratch");

    public static string ContentDirectory
    {
        get
        {
            var documents = Path.Combine(AppContext.BaseDirectory, "Documents", "Xml");
            if (Directory.Exists(documents))
                return documents;
            return Path.Combine(AppContext.BaseDirectory, "Content", "Xml");
        }
    }

    public static string SeedMode =>
        Environment.GetEnvironmentVariable("VESTIGIUM_XML_SEED_MODE") ?? "Overwrite";

    public static string GetPath(string fileName)
    {
        EnsureSeeded();
        return Path.Combine(WorkDirectory, fileName);
    }

    public static string CreateScratchCopy(string fileName)
    {
        EnsureSeeded();
        var destDir = Path.Combine(ScratchDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destDir);
        var dest = Path.Combine(destDir, fileName);
        File.Copy(GetPath(fileName), dest, overwrite: true);
        return dest;
    }

    public static void EnsureSeeded()
    {
        lock (Gate)
        {
            if (_seeded)
                return;

            Directory.CreateDirectory(WorkDirectory);
            if (Directory.Exists(ScratchDirectory))
                Directory.Delete(ScratchDirectory, recursive: true);
            Directory.CreateDirectory(ScratchDirectory);

            var overwrite = !string.Equals(SeedMode, "IfMissing", StringComparison.OrdinalIgnoreCase);

            foreach (var file in RequiredFiles)
            {
                var dest = Path.Combine(WorkDirectory, file);
                if (!overwrite && File.Exists(dest) && new FileInfo(dest).Length > 0)
                    continue;

                if (!TryCopyFromContent(file, dest) && !TryCopyFromEmbedded(file, dest))
                {
                    throw new FileNotFoundException(
                        $"Could not seed '{file}'. Content folder '{ContentDirectory}' is empty " +
                        $"and embedded resource '{ResourcePrefix}{file}' was not found.");
                }

                if (!File.Exists(dest) || new FileInfo(dest).Length == 0)
                    throw new InvalidDataException($"Seeded file is missing or empty: {dest}");
            }

            _seeded = true;
        }
    }

    public static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    public static void ResetForTests()
    {
        lock (Gate)
            _seeded = false;
    }

    private static bool TryCopyFromContent(string fileName, string dest)
    {
        var source = Path.Combine(ContentDirectory, fileName);
        if (!File.Exists(source) || new FileInfo(source).Length == 0)
            return false;

        File.Copy(source, dest, overwrite: true);
        return true;
    }

    private static bool TryCopyFromEmbedded(string fileName, string dest)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = ResourcePrefix + fileName;
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null || stream.Length == 0)
            return false;

        using var output = File.Create(dest);
        stream.CopyTo(output);
        return true;
    }
}
