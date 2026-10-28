using System.Windows;
using Vestigium.Themes;

namespace Vestigium.Helpers.Charts.MenuLab;

public partial class App : Application
{
    public static ThemeManager Themes { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        RegisterAll(Themes);
        Themes.Initialize(this, "StandardWPF");
        base.OnStartup(e);
    }

    internal static void RegisterAll(ThemeManager manager)
    {
        manager.Register(ThemeDefinition.FromPack(
            "LightBlue", "Light Blue", "Vestigium.Themes.LightBlue", isDark: false));
        manager.Register(ThemeDefinition.FromPack(
            "DarkMode", "Dark Mode", "Vestigium.Themes.DarkMode", isDark: true));
        manager.Register(ThemeDefinition.FromPack(
            "Terminal", "Terminal", "Vestigium.Themes.Terminal", isDark: true));
        manager.Register(ThemeDefinition.FromPack(
            "SolarizedDark", "Solarized Dark", "Vestigium.Themes.SolarizedDark", isDark: true));
        manager.Register(ThemeDefinition.FromPack(
            "StandardWPF", "Standard WPF", "Vestigium.Themes.StandardWPF", isDark: false));
        manager.Register(ThemeDefinition.FromPack(
            "Monokai", "Monokai", "Vestigium.Themes.Monokai", isDark: true));
        manager.Register(ThemeDefinition.FromPack(
            "Sublime", "Sublime", "Vestigium.Themes.Sublime", isDark: true));
        manager.Register(ThemeDefinition.FromPack(
            "Dracula", "Dracula", "Vestigium.Themes.Dracula", isDark: true));
        manager.Register(ThemeDefinition.FromPack(
            "Nord", "Nord", "Vestigium.Themes.Nord", isDark: true));
    }
}
