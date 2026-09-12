namespace Vestigium.Helpers.Services;

internal static class ServiceTreeWalker
{
    public static ServiceTree? Build(string name, ServiceTreeDirection direction, ServiceDetailLevel level)
    {
        var root = ServiceSnapshotter.CaptureName(name, level, joinProcess: false);
        if (root is null)
            return null;
        return BuildNode(root, direction, level, [], 0);
    }

    private static ServiceTree BuildNode(
        ServiceInfo node,
        ServiceTreeDirection direction,
        ServiceDetailLevel level,
        HashSet<string> trail,
        int depth)
    {
        if (depth > 32 || !trail.Add(node.Name))
        {
            node.AmbiguousDependency = true;
            return new ServiceTree { Root = node, Children = [] };
        }

        var names = direction switch
        {
            ServiceTreeDirection.DependedBy => node.DependedBy,
            ServiceTreeDirection.Both => node.DependsOn.Concat(node.DependedBy).Distinct(StringComparer.OrdinalIgnoreCase),
            _ => node.DependsOn
        };

        var children = new List<ServiceTree>();
        foreach (var childName in names)
        {
            var child = ServiceSnapshotter.CaptureName(childName, level == ServiceDetailLevel.Identity ? ServiceDetailLevel.Slim : level, joinProcess: false);
            if (child is null)
                continue;
            children.Add(BuildNode(child, direction, level, [.. trail], depth + 1));
        }

        return new ServiceTree { Root = node, Children = children };
    }
}
