using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

internal static class RegistryIndexWriter
{
    public const string Schema = "vest-regidx/1";
    public const int MaxValues = 2_000_000;
    public const int MaxDepth = 64;
    public const int MaxPayloadChars = 256;

    public static RegistryWriteResult Write(
        RegistryClient client,
        string dest,
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view,
        bool confirm,
        bool includePayload,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel)
    {
        dest = HelperGuard.NotBlank(dest, nameof(dest));
        var root = RegistryPath.Normalize(key);
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, root, null, "confirm=false");

        var snap = client.GetKey(hive, root, view, RegistryDetailLevel.Identity);
        if (snap is null)
            return new RegistryWriteResult(RegistryWriteStatus.NotFound, hive, root, null, "key gone");

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dest)) is { Length: > 0 } dir ? dir : ".");
        var started = DateTime.UtcNow;
        var keys = 0;
        var values = 0;

        using var writer = new StreamWriter(dest, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        WriteLine(writer, new Dictionary<string, object?>
        {
            ["rec"] = "header",
            ["schema"] = Schema,
            ["machine"] = client.Machine ?? Environment.MachineName,
            ["capturedAt"] = DateTime.UtcNow.ToString("o"),
            ["hive"] = hive.ToString(),
            ["path"] = root,
            ["view"] = view.ToString(),
            ["source"] = client.IsLocal ? "live" : "remote"
        });

        try
        {
            Walk(client, hive, root, view, root, 0, includePayload, writer, ref keys, ref values, started, progress, cancel);
        }
        catch (OperationCanceledException)
        {
            WriteLine(writer, new Dictionary<string, object?> { ["rec"] = "footer", ["keys"] = keys, ["values"] = values, ["stopped"] = true });
            return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, root, null, "canceled");
        }

        WriteLine(writer, new Dictionary<string, object?> { ["rec"] = "footer", ["keys"] = keys, ["values"] = values, ["stopped"] = false });
        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"WriteIndex hive={hive} path={root} keys={keys} values={values}");
        progress?.Report(new RegistryCompareProgress { Phase = "Done", KeysSeen = keys, ValuesSeen = values, CurrentPath = root, Elapsed = DateTime.UtcNow - started });
        return new RegistryWriteResult(RegistryWriteStatus.Ok, hive, root, null, dest);
    }

    private static void Walk(
        RegistryClient client,
        RegistryHiveKind hive,
        string path,
        RegistryViewKind view,
        string root,
        int depth,
        bool includePayload,
        StreamWriter writer,
        ref int keys,
        ref int values,
        DateTime started,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        if (depth > MaxDepth)
            return;

        var snap = client.GetKey(hive, path, view, RegistryDetailLevel.Full);
        if (snap is null)
            return;

        var relative = Relative(root, path);
        keys++;
        WriteLine(writer, new Dictionary<string, object?> { ["rec"] = "key", ["path"] = relative });
        Report(progress, "IndexLeft", keys, values, relative, started);

        foreach (var value in snap.Values)
        {
            cancel.ThrowIfCancellationRequested();
            if (values >= MaxValues)
                throw new ArgumentException($"MaxValues cap is {MaxValues}.", nameof(values));
            values++;
            var row = new Dictionary<string, object?>
            {
                ["rec"] = "value",
                ["path"] = relative,
                ["name"] = value.Name,
                ["type"] = value.Type.ToString(),
                ["hash"] = Hash(value)
            };
            if (includePayload)
            {
                var text = SmallText(value);
                if (text is not null)
                    row["text"] = text;
            }
            WriteLine(writer, row);
        }

        foreach (var child in snap.SubKeyNames)
        {
            var next = string.IsNullOrEmpty(path) ? child : path + "\\" + child;
            Walk(client, hive, next, view, root, depth + 1, includePayload, writer, ref keys, ref values, started, progress, cancel);
        }
    }

    internal static string? SmallText(RegistryValueInfo value)
    {
        if (value.Type is not RegistryValueKind.String and not RegistryValueKind.ExpandString)
            return null;
        var text = value.Data as string ?? value.DataText;
        return text is { Length: <= MaxPayloadChars } ? text : null;
    }

    internal static string Relative(string root, string path)
    {
        if (string.IsNullOrEmpty(root))
            return path;
        if (path.Equals(root, StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        return path.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase) ? path[(root.Length + 1)..] : path;
    }

    internal static string Hash(RegistryValueInfo value)
    {
        var payload = Canonical(value);
        var buffer = new byte[1 + payload.Length];
        buffer[0] = (byte)(int)value.Type;
        Buffer.BlockCopy(payload, 0, buffer, 1, payload.Length);
        return Convert.ToHexString(SHA256.HashData(buffer));
    }

    private static byte[] Canonical(RegistryValueInfo value) => value.Type switch
    {
        RegistryValueKind.String or RegistryValueKind.ExpandString => Encoding.Unicode.GetBytes(value.Data as string ?? string.Empty),
        RegistryValueKind.MultiString => Multi(value.Data as string[] ?? []),
        RegistryValueKind.DWord => BitConverter.GetBytes(value.Data is int i ? i : Convert.ToInt32(value.Data, CultureInfo.InvariantCulture)),
        RegistryValueKind.QWord => BitConverter.GetBytes(value.Data is long l ? l : Convert.ToInt64(value.Data, CultureInfo.InvariantCulture)),
        RegistryValueKind.Binary or RegistryValueKind.None
            or RegistryValueKind.Link or RegistryValueKind.ResourceList
            or RegistryValueKind.FullResourceDescriptor or RegistryValueKind.ResourceRequirementsList
            or RegistryValueKind.Unknown => Raw(value),
        _ => Raw(value)
    };

    private static byte[] Raw(RegistryValueInfo value) => value.Data switch
    {
        byte[] bytes => bytes,
        _ => []
    };

    private static byte[] Multi(string[] parts)
    {
        var list = new List<byte>();
        foreach (var part in parts)
        {
            list.AddRange(Encoding.Unicode.GetBytes(part));
            list.Add(0);
            list.Add(0);
        }
        list.Add(0);
        list.Add(0);
        return [.. list];
    }

    private static void Report(IProgress<RegistryCompareProgress>? progress, string phase, int keys, int values, string path, DateTime started)
    {
        if (progress is null)
            return;
        if (keys % 25 != 0 && values % 50 != 0)
            return;
        progress.Report(new RegistryCompareProgress
        {
            Phase = phase,
            KeysSeen = keys,
            ValuesSeen = values,
            CurrentPath = path,
            Elapsed = DateTime.UtcNow - started
        });
    }

    private static void WriteLine(StreamWriter writer, Dictionary<string, object?> map)
        => writer.WriteLine(JsonSerializer.Serialize(map));
}
