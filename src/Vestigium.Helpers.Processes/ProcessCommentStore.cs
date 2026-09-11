using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Vestigium.Helpers.Processes;

internal static class ProcessCommentStore
{
    private static readonly ConcurrentDictionary<string, string> Memory = new(StringComparer.OrdinalIgnoreCase);

    internal static string Key(string? imagePath, string name)
        => Hash(ProcessImagePath.Normalize(imagePath, name));

    internal static string Hash(string normalized)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized.ToUpperInvariant()));
        return Convert.ToHexString(bytes);
    }

    internal static string? Get(string key)
    {
        if (Memory.TryGetValue(key, out var live))
            return live;
        LoadDisk();
        return Memory.TryGetValue(key, out var stored) ? stored : null;
    }

    internal static void Set(string key, string? comment, bool persist)
    {
        if (string.IsNullOrWhiteSpace(comment))
            Memory.TryRemove(key, out _);
        else
            Memory[key] = comment;

        if (!persist)
            return;

        var path = ResolvePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var snapshot = Memory.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot));
    }

    private static string ResolvePath()
    {
        if (!string.IsNullOrWhiteSpace(ProcessTestHooks.CommentStorePath))
            return Path.GetFullPath(ProcessTestHooks.CommentStorePath);
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Vestigium", "Processes", "Comments.json");
    }

    private static void LoadDisk()
    {
        var path = ResolvePath();
        if (!File.Exists(path))
            return;
        try
        {
            var text = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            if (data is null)
                return;
            foreach (var pair in data)
                Memory.TryAdd(pair.Key, pair.Value);
        }
        catch
        {
        }
    }
}
