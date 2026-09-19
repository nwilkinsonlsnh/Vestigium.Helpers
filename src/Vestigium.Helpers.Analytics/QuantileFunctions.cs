using MathNet.Numerics.Distributions;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// The only Analytics file that may reference MathNet.Numerics.
/// Location-scale stays 0, 1 (standard t / standard normal).
/// </summary>
internal static class QuantileFunctions
{
    public static double StudentTInv(double df, double p)
        => StudentT.InvCDF(0d, 1d, df, p);

    public static double StudentTCdf(double df, double t)
        => StudentT.CDF(0d, 1d, df, t);

    public static double ChiSquaredInv(double df, double p)
        => ChiSquared.InvCDF(df, p);

    public static double NormalInv(double p)
        => Normal.InvCDF(0d, 1d, p);
}
