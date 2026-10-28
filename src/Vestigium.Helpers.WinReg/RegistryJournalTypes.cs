namespace Vestigium.Helpers.WinReg;

public sealed class RegistryJournalBatch
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public string? Label { get; init; }
    public string Status { get; init; } = "open";
    public int Mutations { get; init; }
}

public sealed class RegistryJournalInfo
{
    public required string Path { get; init; }
    public required string Schema { get; init; }
    public string? Machine { get; init; }
    public bool Protect { get; init; }
    public IReadOnlyList<RegistryJournalBatch> Batches { get; init; } = [];
}
