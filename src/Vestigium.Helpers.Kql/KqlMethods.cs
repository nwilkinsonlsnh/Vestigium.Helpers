namespace Vestigium.Helpers.Kql;

/// <summary>
/// Library-owned methods. A catalog subscribes by declaring a field type. It does not register methods.
/// </summary>
public static class KqlMethods
{
    public static bool TrySplit(string ident, out string field, out string method)
    {
        field = ident;
        method = string.Empty;
        var dot = ident.LastIndexOf('.');
        if (dot <= 0 || dot == ident.Length - 1)
            return false;
        var tail = ident[(dot + 1)..];
        if (!IsMethod(tail))
            return false;
        field = ident[..dot];
        method = tail;
        return true;
    }

    public static bool IsMethod(string name)
        => Canonical(name) is not null;

    public static string? Canonical(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "contains" => "contains",
            "equals" => "equals",
            "startswith" => "startsWith",
            "endswith" => "endsWith",
            "isempty" => "isEmpty",
            "like" => "like",
            "istrue" => "isTrue",
            "isfalse" => "isFalse",
            "greaterthan" => "greaterThan",
            "greaterthanorequal" => "greaterThanOrEqual",
            "lessthan" => "lessThan",
            "lessthanorequal" => "lessThanOrEqual",
            "between" => "between",
            "after" => "after",
            "before" => "before",
            "on" => "on",
            _ => null
        };
    }

    public static bool Allows(string method, KqlType type, KqlCompareAs compareAs)
    {
        var name = Canonical(method);
        if (name is null)
            return false;
        if (compareAs is KqlCompareAs.IpAddress or KqlCompareAs.MacAddress || type is KqlType.IpAddress or KqlType.MacAddress)
            return name is "equals" or "contains" or "startsWith" or "endsWith" or "isEmpty";

        return name switch
        {
            "contains" or "startsWith" or "endsWith" or "like" => type is KqlType.String,
            "isEmpty" => type is KqlType.String or KqlType.Char or KqlType.DateTime,
            "isTrue" or "isFalse" => type == KqlType.Boolean,
            "after" or "before" or "on" => type == KqlType.DateTime,
            "between" => type is KqlType.Integer or KqlType.Long or KqlType.Number or KqlType.Decimal or KqlType.TimeSpan or KqlType.DateTime,
            "greaterThan" or "greaterThanOrEqual" or "lessThan" or "lessThanOrEqual"
                => type is KqlType.Integer or KqlType.Long or KqlType.Number or KqlType.Decimal or KqlType.TimeSpan,
            "equals" => true,
            _ => false
        };
    }

    public static IReadOnlyList<string> For(KqlType type, KqlCompareAs compareAs)
    {
        if (compareAs is KqlCompareAs.IpAddress or KqlCompareAs.MacAddress || type is KqlType.IpAddress or KqlType.MacAddress)
            return ["equals", "contains", "startsWith", "endsWith", "isEmpty"];

        return type switch
        {
            KqlType.String => ["contains", "equals", "startsWith", "endsWith", "isEmpty", "like"],
            KqlType.Char => ["equals", "isEmpty"],
            KqlType.Boolean => ["equals", "isTrue", "isFalse"],
            KqlType.Integer or KqlType.Long or KqlType.Number or KqlType.Decimal or KqlType.TimeSpan
                => ["equals", "greaterThan", "greaterThanOrEqual", "lessThan", "lessThanOrEqual", "between"],
            KqlType.DateTime => ["after", "before", "on", "between", "isEmpty"],
            _ => ["equals"]
        };
    }

    public static bool IsNullary(string method)
        => Canonical(method) is "isEmpty" or "isTrue" or "isFalse";

    public static bool IsBetween(string method)
        => Canonical(method) == "between";

    public static KqlCompareOp ToOp(string method)
    {
        return Canonical(method) switch
        {
            "contains" => KqlCompareOp.Contains,
            "startsWith" => KqlCompareOp.BeginsWith,
            "endsWith" => KqlCompareOp.EndsWith,
            "like" => KqlCompareOp.Like,
            "greaterThan" or "after" => KqlCompareOp.Gt,
            "greaterThanOrEqual" => KqlCompareOp.Ge,
            "lessThan" or "before" => KqlCompareOp.Lt,
            "lessThanOrEqual" => KqlCompareOp.Le,
            "isFalse" => KqlCompareOp.Eq,
            _ => KqlCompareOp.Eq
        };
    }
}

/// <summary>Boolean words accepted on a Boolean field. Case does not matter.</summary>
public static class KqlBooleans
{
    public static bool TryParse(string? text, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        switch (text.Trim().ToLowerInvariant())
        {
            case "true":
            case "t":
            case "yes":
            case "y":
            case "1":
                value = true;
                return true;
            case "false":
            case "f":
            case "no":
            case "n":
            case "0":
                return true;
            default:
                return false;
        }
    }
}
