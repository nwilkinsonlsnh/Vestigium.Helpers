namespace Vestigium.Helpers.Services;

internal static class ServiceTreeWalker
{
    public const int MaxDepth = 16;
    public const int MaxNodes = 256;

    public static ServiceTree? Build(string name, ServiceTreeDirection direction, ServiceDetailLevel level)
    {
        var walkLevel = level == ServiceDetailLevel.Identity ? ServiceDetailLevel.Slim : level;
        var root = ServiceSnapshotter.CaptureName(name, walkLevel, joinProcess: false);
        if (root is null)
            return null;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return BuildNode(root, direction, walkLevel, seen, 0);
    }

    private static ServiceTree BuildNode(
        ServiceInfo node,
        ServiceTreeDirection direction,
        ServiceDetailLevel level,
        HashSet<string> seen,
        int depth)
    {
        if (!seen.Add(node.Name))
        {
            node.AmbiguousDependency = true;
            return new ServiceTree { Root = node, Children = [] };
        }

        if (depth >= MaxDepth || seen.Count >= MaxNodes)
        {
            node.AmbiguousDependency = true;
            return new ServiceTree { Root = node, Children = [] };
        }

        IEnumerable<string> names = direction switch
        {
            ServiceTreeDirection.DependedBy => node.DependedBy,
            ServiceTreeDirection.Both => node.DependsOn.Concat(node.DependedBy),
            _ => node.DependsOn
        };

        var seenChild = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var children = new List<ServiceTree>();
        foreach (var childName in names)
        {
            if (string.IsNullOrWhiteSpace(childName) || !seenChild.Add(childName))
                continue;
            if (seen.Contains(childName))
                continue;
            if (seen.Count >= MaxNodes)
            {
                node.AmbiguousDependency = true;
                break;
            }

            var child = ServiceSnapshotter.CaptureName(childName, level, joinProcess: false);
            if (child is null)
                continue;
            if (seen.Contains(child.Name))
                continue;

            children.Add(BuildNode(child, direction, level, seen, depth + 1));
        }

        return new ServiceTree { Root = node, Children = children };
    }
}
