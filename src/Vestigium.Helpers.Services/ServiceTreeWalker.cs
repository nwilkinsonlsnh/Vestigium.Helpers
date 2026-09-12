namespace Vestigium.Helpers.Services;

internal static class ServiceTreeWalker
{
    public const int MaxDepth = 32;

    public static ServiceTree? Build(string name, ServiceTreeDirection direction, ServiceDetailLevel level)
    {
        var walkLevel = level == ServiceDetailLevel.Identity ? ServiceDetailLevel.Slim : level;
        var root = ServiceSnapshotter.CaptureName(name, walkLevel, joinProcess: false);
        if (root is null)
            return null;
        return BuildNode(root, direction, walkLevel, [], 0);
    }

    private static ServiceTree BuildNode(
        ServiceInfo node,
        ServiceTreeDirection direction,
        ServiceDetailLevel level,
        HashSet<string> trail,
        int depth)
    {
        if (depth > MaxDepth || !trail.Add(node.Name))
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
            var child = ServiceSnapshotter.CaptureName(childName, level, joinProcess: false);
            if (child is null)
                continue;
            children.Add(BuildNode(child, direction, level, new HashSet<string>(trail, StringComparer.OrdinalIgnoreCase), depth + 1));
        }

        return new ServiceTree { Root = node, Children = children };
    }
}
