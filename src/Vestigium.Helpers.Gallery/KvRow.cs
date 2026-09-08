namespace Vestigium.Helpers.Gallery;

public sealed class KvRow
{
    public required string Key { get; init; }
    public required string Value { get; init; }
}

public sealed class BandRow
{
    public required string Band { get; init; }
    public int N { get; init; }
    public string Min { get; init; } = "";
    public string P50 { get; init; } = "";
    public string Max { get; init; } = "";
    public string Mean { get; init; } = "";
    public string StdDev { get; init; } = "";
    public string Skew { get; init; } = "";
    public string ExKurt { get; init; } = "";
}

public sealed class IntervalRow
{
    public required string Level { get; init; }
    public required string Parameter { get; init; }
    public string Estimate { get; init; } = "";
    public string Lower { get; init; } = "";
    public string Upper { get; init; } = "";
    public string Width { get; init; } = "";
    public string Method { get; init; } = "";
    public string Defined { get; init; } = "";
}

public sealed class HistBar
{
    public required string Label { get; init; }
    public double Count { get; init; }
    public double Height { get; init; }
    public string Caption { get; init; } = "";
}

public sealed class SamplePoint
{
    public int Index { get; init; }
    public string Value { get; init; } = "";
    public string Timestamp { get; init; } = "";
}

public sealed class ReadCellRow
{
    public int Row { get; init; }
    public string Header { get; init; } = "";
    public string Value { get; init; } = "";
    public string Type { get; init; } = "";
}

public sealed class PieSlice
{
    public double Width { get; init; }
    public System.Windows.Media.Brush Fill { get; init; } = System.Windows.Media.Brushes.SteelBlue;
    public string Caption { get; init; } = "";
}

public sealed class ScatterDot
{
    public double X { get; init; }
    public double Y { get; init; }
    public string Caption { get; init; } = "";
}
