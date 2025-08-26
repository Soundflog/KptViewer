using System.Xml.Linq;
using Kpt_Viewer.Domain;

namespace Kpt_Viewer.Services;

public sealed class XmlIndexBuilder
{
    private static readonly RootDefinition[] Defs =
    [
        new(
            RootKind.Parcels,
            displayName: "Parcels",
            containers: [new ContainerSpec("land_records", "land_record")],
            idSelector: e => FirstNonEmpty(
                e.Element("object")?.Element("common_data")?.Element("cad_number")?.Value,
                e.Element("cad_number")?.Value)
        ),
        new(
            RootKind.ObjectRealty,
            displayName: "ObjectRealty",
            containers:
            [
                new ContainerSpec("build_records", "build_record"),
                new ContainerSpec("construction_records", "construction_record")
                // Some datasets might wrap construction_record singularly — handle gracefully in search
            ],
            idSelector: e => FirstNonEmpty(
                e.Element("object")?.Element("common_data")?.Element("cad_number")?.Value,
                e.Element("cad_number")?.Value)
        ),
        new(
            RootKind.SpatialData,
            displayName: "SpatialData",
            containers: [new ContainerSpec("spatial_data", "entity_spatial")],
            idSelector: e => e.Element("sk_id")?.Value
        ),
        new(
            RootKind.Bounds,
            displayName: "Bounds",
            containers: [new ContainerSpec("municipal_boundaries", "municipal_boundary_record")],
            idSelector: e => e.Element("reg_numb_border")?.Value
        ),
        new(
            RootKind.Zones,
            displayName: "Zones",
            containers:
            [
                new ContainerSpec("zones_and_territories_records", "zones_and_territories_record"),
                // чтобы парсер находил записи при любом названии контейнера
                new ContainerSpec("zones_and_territories_boundaries", "zones_and_territories_record")
            ],
            idSelector: e => e.Element("reg_numb_border")?.Value
        )
    ];

    public IndexModel Build(XDocument doc)
    {
        var roots = new List<RootItems>();
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None; // No-op if no namespace


        foreach (var def in Defs)
        {
            var rootItems = new RootItems(def.DisplayName);
            foreach (var container in def.Containers)
            {
                // Find <container> anywhere
                var containerEls = doc.Descendants(ns + container.ContainerName).ToList();

                if (containerEls.Count == 0)
                {
                    // Some files might not have a dedicated container (e.g., loose construction_record). Search globally.
                    var looseItems = doc.Descendants(ns + container.ItemName);
                    foreach (var item in looseItems)
                        TryAdd(def, container, item, rootItems);
                    continue;
                }

                foreach (var item in containerEls
                             .Select(cont => cont.Descendants(ns + container.ItemName))
                             .SelectMany(items => items))
                {
                    TryAdd(def, container, item, rootItems);
                }
            }


            if (rootItems.Items.Count > 0)
                roots.Add(rootItems);
        }


        // Sort IDs lexicographically for stable UI
        foreach (var r in roots)
            r.Items.Sort((a, b) => string.Compare(a.DisplayId, b.DisplayId, StringComparison.Ordinal));

        return new IndexModel(roots);
    }

    private static void TryAdd(RootDefinition def, ContainerSpec container, XElement itemElement, RootItems rootItems)
    {
        var id = def.IdSelector(itemElement);
        if (string.IsNullOrWhiteSpace(id)) return;
        var realContainer = GuessContainerName(itemElement);
        rootItems.Items.Add(new NodeModel(
            def.Kind, id, itemElement, realContainer, container.ItemName, ExtractAddress(itemElement)));
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

    private static string GuessContainerName(XElement item)
    {
        for (var p = item.Parent; p != null; p = p.Parent)
        {
            var name = p.Name.LocalName;
            // известные контейнеры
            if (name is "land_records" or "build_records" or "construction_records"
                or "spatial_data" or "municipal_boundaries"
                or "zones_and_territories_records" or "zones_and_territories_boundaries")
                return name;
        }

        return "unknown_container";
    }
    
    private static string? ExtractAddress(XElement item)
    {
        // 1) Предпочтительно readable_address
        var readable = item.Descendants().FirstOrDefault(x => x.Name.LocalName == "readable_address");
        if (readable != null)
        {
            var txt = (readable.Value ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(txt)) return txt;
        }


        // 2) Иначе попытаться собрать текст из <address>
        var addr = item.Descendants().FirstOrDefault(x => x.Name.LocalName == "address");
        if (addr != null)
        {
            var parts = addr.Descendants()
                .Where(n => !n.HasElements)
                .Select(n => (n.Value ?? string.Empty).Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(50);
            var joined = string.Join(", ", parts);
            if (!string.IsNullOrWhiteSpace(joined)) return joined;
        }
        return null;
    }
}