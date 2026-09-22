namespace Vestigium.Helpers.Processes;

/// <summary>Live PPID tree rooted at one process.</summary>
public sealed class ProcessTree
{
    public ProcessTree(ProcessInfo root, IReadOnlyList<ProcessTree> children)
    {
        Root = root;
        Children = children;
    }

    public ProcessInfo Root { get; }
    public IReadOnlyList<ProcessTree> Children { get; }

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
