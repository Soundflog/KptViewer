using System.Xml.Linq;
using Kpt_Viewer.Domain;

namespace Kpt_Viewer.Services;

public sealed class XmlIndexBuilder
{
    private static readonly RootDefinition[] _defs = new[]
    {
        new RootDefinition(
            RootKind.Parcels,
            displayName: "Parcels",
            containers: new[] { new ContainerSpec("land_records", "land_record") },
            idSelector: e => FirstNonEmpty(
                e.Element("object")?.Element("common_data")?.Element("cad_number")?.Value,
                e.Element("cad_number")?.Value)
        ),
        new RootDefinition(
            RootKind.ObjectRealty,
            displayName: "ObjectRealty",
            containers: new[]
            {
                new ContainerSpec("build_records", "build_record"),
                new ContainerSpec("construction_records", "construction_record"),
// Some datasets might wrap construction_record singularly — handle gracefully in search
            },
            idSelector: e => FirstNonEmpty(
                e.Element("object")?.Element("common_data")?.Element("cad_number")?.Value,
                e.Element("cad_number")?.Value)
        ),
        new RootDefinition(
            RootKind.SpatialData,
            displayName: "SpatialData",
            containers: new[] { new ContainerSpec("spatial_data", "entity_spatial") },
            idSelector: e => e.Element("sk_id")?.Value
        ),
        new RootDefinition(
            RootKind.Bounds,
            displayName: "Bounds",
            containers: new[] { new ContainerSpec("municipal_boundaries", "municipal_boundary_record") },
            idSelector: e => e.Element("reg_numb_border")?.Value
        ),
        new RootDefinition(
            RootKind.Zones,
            displayName: "Zones",
            containers: new[] { new ContainerSpec("zones_and_territories_records", "zones_and_territories_record") },
            idSelector: e => e.Element("reg_numb_border")?.Value
        ),
    };

    public IndexModel Build(XDocument doc)
    {
        var roots = new List<RootItems>();
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None; // No-op if no namespace


        foreach (var def in _defs)
        {
            var rootItems = new RootItems(def.DisplayName);
            foreach (var container in def.Containers)
            {
// Find <container> anywhere
                var containerEls = doc.Descendants(ns + container.ContainerName).ToList();


                if (!containerEls.Any())
                {
// Some files might not have a dedicated container (e.g., loose construction_record). Search globally.
                    var looseItems = doc.Descendants(ns + container.ItemName);
                    foreach (var item in looseItems)
                        TryAdd(def, container, item, rootItems);
                    continue;
                }


                foreach (var cont in containerEls)
                {
                    var items = cont.Descendants(ns + container.ItemName);
                    foreach (var item in items)
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
        rootItems.Items.Add(new NodeModel(def.Kind, id!, itemElement, container.ContainerName, container.ItemName));
    }


    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
    
}
    