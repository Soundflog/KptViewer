using System.Xml.Linq;

namespace Kpt_Viewer.Domain;

public sealed class RootDefinition(
    RootKind kind,
    string displayName,
    IEnumerable<ContainerSpec> containers,
    Func<XElement, string?> idSelector)
{
    public RootKind Kind { get; } = kind;
    public string DisplayName { get; } = displayName;
    
    // One group can aggregate multiple containers (e.g., ObjectRealty: build_records + construction_records)
    public IReadOnlyList<ContainerSpec> Containers { get; } = containers.ToList();
    
    public Func<XElement, string?> IdSelector { get; } = idSelector;
}
