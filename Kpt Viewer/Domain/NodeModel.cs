using System.Xml.Linq;

namespace Kpt_Viewer.Domain;

public sealed class NodeModel(RootKind root, string displayId, XElement element, string containerName, string itemName, string? addressText)
{
    public RootKind Root { get; } = root;
    public string DisplayId { get; } = displayId;
    public XElement Element { get; } = element;
    public string ContainerName { get; } = containerName; // for export routing
    public string ItemName { get; } = itemName;
    public string? AddressText { get; } = addressText; // address for search (readable_address if present)
}