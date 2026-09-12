using Vestigium.Helpers;

namespace Vestigium.Helpers.Services;

public static partial class ServiceHelper
{
    public static string DefaultCampaignRoot => ServiceCampaign.Root();

    public static ServiceCampaign CreateCampaign(ServiceCampaignRecipe recipe)
    {
        var ready = ServiceCampaign.Validate(recipe);
        var folder = Path.Combine(ServiceCampaign.Root(), ready.Name);
        ServiceCampaign.WriteRecipe(folder, ready);
        return new ServiceCampaign(ready, folder);
    }

    public static IReadOnlyList<string> ListCampaigns()
    {
        var root = ServiceCampaign.Root();
        if (!Directory.Exists(root))
            return [];
        return Directory.GetDirectories(root)
            .Where(dir => File.Exists(Path.Combine(dir, "recipe.json")))
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static ServiceCampaign LoadCampaign(string name)
    {
        var id = ServiceCampaign.Sanitize(HelperGuard.NotBlank(name, nameof(name)));
        var folder = Path.Combine(ServiceCampaign.Root(), id);
        if (!File.Exists(Path.Combine(folder, "recipe.json")))
            throw new FileNotFoundException("Campaign recipe not found.", folder);
        var recipe = ServiceCampaign.Validate(ServiceCampaign.ReadRecipe(folder));
        return new ServiceCampaign(recipe, folder);
    }
}
