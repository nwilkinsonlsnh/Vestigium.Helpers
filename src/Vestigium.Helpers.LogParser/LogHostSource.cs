namespace Vestigium.Helpers.LogParser;

[Flags]
public enum LogHostSource
{
    None = 0,
    Request = 1,
    Redirect = 2,
    Location = 4,
    Page = 8,
    Url = 16
}
