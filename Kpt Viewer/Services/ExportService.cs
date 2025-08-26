using System.Xml.Linq;
using Kpt_Viewer.Domain;

namespace Kpt_Viewer.Services;

public sealed class ExportService
{
    public XDocument Export(IReadOnlyCollection<NodeModel> nodes)
    {
        // Root wrapper as in source files
        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
        var root = new XElement("extract_cadastral_plan_territory");
        doc.Add(root);


        // Group by container and item name to preserve original structure
        var groups = nodes.GroupBy(n => (n.ContainerName, n.ItemName));
        foreach (var g in groups)
        {
            var containerEl = new XElement(g.Key.ContainerName);
            foreach (var node in g)
            {
                // Deep clone node element (with its entire nested content)
                containerEl.Add(new XElement(node.Element));
            }
            root.Add(containerEl);
        }


        return doc;
    }
}