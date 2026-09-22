namespace Vestigium.Helpers.Processes;

internal static class ProcessTreeWalker
{
    internal static ProcessTree? Build(int pid, ProcessDetailLevel level)
    {
        var table = ProcessSnapshotter.Capture(ProcessDetailLevel.Identity);
        var byPid = new Dictionary<int, ProcessInfo>();
        foreach (var row in table)
            byPid[row.Pid] = row;

        if (!byPid.ContainsKey(pid))
        {
            var live = ProcessSnapshotter.CapturePid(pid, level);
            if (live is null)
                return null;
            byPid[pid] = ToIdentity(live);
        }

        var childrenOf = new Dictionary<int, List<ProcessInfo>>();
        foreach (var row in byPid.Values)
        {
            if (row.ParentPid is not int parent || parent <= 0)
                continue;
            if (!childrenOf.TryGetValue(parent, out var list))
            {
                list = [];
                childrenOf[parent] = list;
            }
            list.Add(row);
        }

        return Node(pid, byPid, childrenOf, [], level);
    }

    internal static IReadOnlyList<ProcessInfo> ChildrenOf(int pid)
    {
        var tree = Build(pid, ProcessDetailLevel.Identity);
        return tree is null ? [] : tree.Children.Select(child => child.Root).ToArray();
    }

    internal static IReadOnlyList<ProcessInfo> DescendantsOf(int pid)
    {
        var tree = Build(pid, ProcessDetailLevel.Identity);
        return tree is null ? [] : tree.Flatten().Skip(1).ToArray();
    }

    private static ProcessTree Node(
        int pid,
        Dictionary<int, ProcessInfo> byPid,
        Dictionary<int, List<ProcessInfo>> childrenOf,
        HashSet<int> ancestors,
        ProcessDetailLevel level)
    {
        var root = byPid[pid];
        if (level != ProcessDetailLevel.Identity)
        {
            var rich = ProcessSnapshotter.CapturePid(pid, level);
            if (rich is not null)
                root = rich;
        }

        var kids = new List<ProcessTree>();
        if (!childrenOf.TryGetValue(pid, out var rawKids))
            return new ProcessTree(root, kids);

        var nextAncestors = new HashSet<int>(ancestors) { pid };
        foreach (var child in rawKids.OrderBy(row => row.Pid))
        {
            if (nextAncestors.Contains(child.Pid))
            {
                child.AmbiguousParent = true;
                kids.Add(new ProcessTree(child, []));
                continue;
            }

            kids.Add(Node(child.Pid, byPid, childrenOf, nextAncestors, ProcessDetailLevel.Identity));
        }

        return new ProcessTree(root, kids);
    }

    private static ProcessInfo ToIdentity(ProcessInfo row) => new()
    {
        Pid = row.Pid,
        ParentPid = row.ParentPid,
        ParentAlive = row.ParentAlive,
        Name = row.Name,
        SessionId = row.SessionId,
        ImagePath = row.ImagePath
    };
}
