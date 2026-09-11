using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Kql;

public sealed class KqlSession : IDisposable
{
    private readonly Dictionary<string, KqlField> _lookup;
    private int _disposed;

    internal KqlSession(IReadOnlyList<KqlPack> packs, KqlGroups groups, IReadOnlyList<KqlField> fields)
    {
        Packs = packs;
        Groups = groups;
        Fields = fields;
        _lookup = new Dictionary<string, KqlField>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            _lookup[field.Canonical] = field;
            foreach (var alias in field.Aliases)
            {
                if (!_lookup.ContainsKey(alias))
                    _lookup[alias] = field;
            }
        }

        HelperLog.Information(
            HelperLog.AppIds.Kql,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Session,
            $"session packs={string.Join(',', packs)} groups={groups} fields={fields.Count}");
    }

    public IReadOnlyList<KqlPack> Packs { get; }
    public KqlGroups Groups { get; }
    public IReadOnlyList<KqlField> Fields { get; }

    public bool TryGetField(string name, out KqlField field)
    {
        field = null!;
        if (string.IsNullOrWhiteSpace(name))
            return false;
        return _lookup.TryGetValue(name.Trim(), out field!);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
    }
}
