namespace STEM.Infrastructure.VirtualLab.Components.Providers.Fritzing;

public class FritzingOptions
{
    public const string SectionName = "ComponentProviders:Fritzing";

    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 80;

    // github.com/fritzing/fritzing-parts, "core" folder — verified against
    // the real repo (2026-08 audit). Contents API defaults to the "develop"
    // branch when Ref is unset; pin it explicitly so search results are
    // reproducible.
    public string Owner { get; set; } = "fritzing";
    public string Repo { get; set; } = "fritzing-parts";
    public string Ref { get; set; } = "master";
    public string PartsFolder { get; set; } = "core";

    public int SearchResultLimit { get; set; } = 20;
    public int DirectoryCacheSeconds { get; set; } = 300;
}
