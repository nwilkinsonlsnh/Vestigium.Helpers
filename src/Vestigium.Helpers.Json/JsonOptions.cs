namespace Vestigium.Helpers.Json;

/// <summary>
/// Document shape stored on a <see cref="JsonSession"/>.
/// </summary>
public enum JsonDocumentKind
{
    /// <summary>One RFC 8259 JSON document (<c>.json</c>).</summary>
    Json = 0,

    /// <summary>Newline-delimited JSON records (<c>.jsonl</c>).</summary>
    Jsonl = 1
}

/// <summary>
/// What happens when a write target already exists.
/// </summary>
public enum JsonCollision
{
    /// <summary>Throw <see cref="System.IO.IOException"/> and leave the destination unchanged.</summary>
    Fail = 0,

    /// <summary>Replace the destination file.</summary>
    Overwrite = 1
}

/// <summary>
/// Options for <see cref="JsonHelper.WriteFile{T}"/>.
/// </summary>
public sealed class JsonWriteOptions
{
    /// <summary>Pretty-print the document. Default is <see langword="true"/>.</summary>
    public bool WriteIndented { get; init; } = true;

    /// <summary>Collision policy. Default is <see cref="JsonCollision.Fail"/>.</summary>
    public JsonCollision Collision { get; init; } = JsonCollision.Fail;

    /// <summary>Write to a temp file and replace. Default is <see langword="true"/>.</summary>
    public bool AtomicWrite { get; init; } = true;
}

/// <summary>
/// Options for <see cref="JsonHelper.FromJson{T}"/>.
/// </summary>
public sealed class JsonReadOptions
{
    /// <summary>Maximum nested depth accepted by System.Text.Json. Default is 64. Must be at least 1.</summary>
    public int MaxDepth { get; init; } = 64;
}

/// <summary>
/// Options for an opened or created <see cref="JsonSession"/>.
/// </summary>
public sealed class JsonSessionOptions
{
    /// <summary>Collision policy for Save / SaveAs. Default is <see cref="JsonCollision.Fail"/>.</summary>
    public JsonCollision Collision { get; init; } = JsonCollision.Fail;

    /// <summary>Atomic replace on Save. Default is <see langword="true"/>.</summary>
    public bool AtomicWrite { get; init; } = true;
}
