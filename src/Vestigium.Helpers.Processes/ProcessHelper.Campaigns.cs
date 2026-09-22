using Vestigium.Helpers;

namespace Vestigium.Helpers.Processes;

public static partial class ProcessHelper
{
    public static string DefaultCampaignRoot => ProcessCampaign.Root();

    public static ProcessCampaign CreateCampaign(ProcessCampaignRecipe recipe)
    {
        var ready = ProcessCampaign.Validate(recipe);
        var folder = Path.Combine(ProcessCampaign.Root(), ready.Name);
        ProcessCampaign.WriteRecipe(folder, ready);
        return new ProcessCampaign(ready, folder);
    }

    public static IReadOnlyList<string> ListCampaigns()
    {
        var root = ProcessCampaign.Root();
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

    public static ProcessCampaign LoadCampaign(string name)
    {
        var id = ProcessCampaign.Sanitize(HelperGuard.NotBlank(name, nameof(name)));
        var folder = Path.Combine(ProcessCampaign.Root(), id);
        if (!File.Exists(Path.Combine(folder, "recipe.json")))
            throw new FileNotFoundException("Campaign recipe not found.", folder);
        var recipe = ProcessCampaign.Validate(ProcessCampaign.ReadRecipe(folder));
        return new ProcessCampaign(recipe, folder);
    }
}
