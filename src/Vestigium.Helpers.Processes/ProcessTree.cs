namespace Vestigium.Helpers.Processes;

/// <summary>Live PPID tree rooted at one process.</summary>
public sealed class ProcessTree(ProcessInfo root, IReadOnlyList<ProcessTree> children)
{
    public ProcessInfo Root { get; } = root;
    public IReadOnlyList<ProcessTree> Children { get; } = children;

    public IReadOnlyList<ProcessInfo> Flatten()
    {
        var rows = new List<ProcessInfo>();
        Walk(this, rows);
        return rows;
    }

    private static void Walk(ProcessTree node, List<ProcessInfo> rows)
    {
        rows.Add(node.Root);
        foreach (var child in node.Children)
            Walk(child, rows);
    }
}
