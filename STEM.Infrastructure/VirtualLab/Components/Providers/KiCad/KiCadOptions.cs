namespace STEM.Infrastructure.VirtualLab.Components.Providers.KiCad;

public class KiCadOptions
{
    public const string SectionName = "ComponentProviders:KiCad";

    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 70;

    // github.com/KiCad/kicad-symbols — verified real, public, CC-BY-SA-4.0
    // (2026-08 audit). A curated, small set of library files relevant to
    // this phase's demo categories (LED/Buzzer/Button) — the repo has ~200
    // library files; scanning all of them for a search box is out of scope
    // for "chứng minh kiến trúc", not a technical limitation of the parser.
    public string Owner { get; set; } = "KiCad";
    public string Repo { get; set; } = "kicad-symbols";
    public string Ref { get; set; } = "master";
    public string[] LibraryFiles { get; set; } = { "Device.lib", "Switch.lib", "LED.lib" };

    public int SearchResultLimit { get; set; } = 20;
    public int LibraryCacheSeconds { get; set; } = 300;
}
