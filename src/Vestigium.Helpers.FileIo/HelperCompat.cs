namespace Vestigium.Helpers.FileIo;

/// <summary>
/// Package-local stand-ins for the old core HelperLog / HelperGuard names.
/// Callers still go through FileIoLog → Vestigium.Logging. Not part of the public API.
/// </summary>
internal static class HelperLog
{
    public static class Subcategories
    {
        public const string Job = FileIoLog.Subcategories.Job;
        public const string Recon = FileIoLog.Subcategories.Recon;
        public const string Copy = FileIoLog.Subcategories.Copy;
        public const string Move = FileIoLog.Subcategories.Move;
        public const string Delete = FileIoLog.Subcategories.Delete;
        public const string Mirror = FileIoLog.Subcategories.Mirror;
        public const string Analyze = FileIoLog.Subcategories.Analyze;
        public const string Compare = FileIoLog.Subcategories.Compare;
        public const string Probe = FileIoLog.Subcategories.Probe;
        public const string SecureDelete = FileIoLog.Subcategories.SecureDelete;
        public const string Prune = FileIoLog.Subcategories.Prune;
        public const string Index = FileIoLog.Subcategories.Index;
        public const string Stats = FileIoLog.Subcategories.Stats;
        public const string Progress = FileIoLog.Subcategories.Progress;
    }

    public static string NewId() => FileIoLog.NewId();
}

internal static class HelperGuard
{
    public static string NotBlank(string? value, string paramName)
        => FileIoLog.RequireNotBlank(value, paramName);
}
