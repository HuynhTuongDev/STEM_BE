using System.Xml.Linq;
using STEM.Application.UseCases.Components.Abstractions;

namespace STEM.Application.UseCases.Components.Providers.Fritzing;

// Parses a Fritzing .fzp part file (real format verified against
// github.com/fritzing/fritzing-parts/blob/master/core/LED-generic-5mm.fzp).
// Pure XML->DTO transform, no I/O — testable without HTTP/DB. Reads only
// <title>/<properties name="family">/<connectors><connector id name>; the
// <views>/<p svgId=".."/terminalId="..">/<layers image="..."> nodes are
// visual/asset metadata ONLY (connectorNpin/connectorNterminal are SVG
// element ids, never treated as simulation identity — see
// ExternalPinCandidate doc comment).
public static class FritzingPartParser
{
    // Returns null (not throws) on malformed/unexpected XML — a provider
    // hiccup on one part must never take down a whole search/import batch.
    public static ExternalComponentCandidate? Parse(
        string fzpXml,
        string externalId,
        string sourceUrl,
        string? primaryAssetUrl)
    {
        if (string.IsNullOrWhiteSpace(fzpXml))
        {
            return null;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(fzpXml);
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }

        var module = document.Root;
        if (module == null || module.Name.LocalName != "module")
        {
            return null;
        }

        var title = module.Element("title")?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var family = module.Element("properties")
            ?.Elements("property")
            .FirstOrDefault(p => (string?)p.Attribute("name") == "family")
            ?.Value?.Trim();

        var moduleId = (string?)module.Attribute("moduleId");
        var version = module.Element("version")?.Value?.Trim();

        var pins = (module.Element("connectors")?.Elements("connector") ?? Enumerable.Empty<XElement>())
            .Select(ParsePin)
            .Where(pin => pin != null)
            .Select(pin => pin!)
            .ToList();

        return new ExternalComponentCandidate(
            Provider: FritzingConstants.ProviderName,
            ExternalId: externalId,
            Name: title,
            Category: family,
            SourceUrl: sourceUrl,
            License: null, // .fzp files don't carry per-part license metadata;
                            // repo-level license only — LicenseStatus stays
                            // Unknown at import time (STEP 28).
            ExternalVersion: version ?? moduleId,
            Checksum: null, // set by the caller from the GitHub blob sha, if known
            Pins: pins,
            PrimaryAssetUrl: primaryAssetUrl);
    }

    private static ExternalPinCandidate? ParsePin(XElement connector)
    {
        var visualPinId = (string?)connector.Attribute("id");
        var name = (string?)connector.Attribute("name");
        if (string.IsNullOrWhiteSpace(visualPinId) || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return new ExternalPinCandidate(visualPinId, name.Trim());
    }
}

public static class FritzingConstants
{
    public const string ProviderName = "fritzing";
}
