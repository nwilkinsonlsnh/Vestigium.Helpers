using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Gallery;

public sealed partial class SkeletonViewModel : GalleryViewModelBase
{
    public SkeletonViewModel(SkeletonSpec spec)
    {
        Spec = spec;
        StatusText = $"Logger initialized · APPID {spec.AppId}";
    }

    public SkeletonSpec Spec { get; }

    public string StartupSnippet =>
        $"HelperWpfHost.Start(this, HelperLog.AppIds.{Spec.AppId});";

    [RelayCommand]
    private void Probe()
    {
        var identity = Spec.Probe();
        StatusText = $"Probe complete · Identity={identity}";
        RefreshLines();
    }
}
