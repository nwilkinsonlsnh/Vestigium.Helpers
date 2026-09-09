namespace Vestigium.Helpers.Json;

public enum JsonDocumentKind
{
    Json = 0,
    Jsonl = 1
}

public enum JsonCollision
{
    Fail = 0,
    Overwrite = 1
}

public sealed class JsonWriteOptions
{
    public bool WriteIndented { get; init; } = true;
    public JsonCollision Collision { get; init; } = JsonCollision.Fail;
    public bool AtomicWrite { get; init; } = true;
}

public sealed class JsonReadOptions
{
    public int MaxDepth { get; init; } = 64;
}
