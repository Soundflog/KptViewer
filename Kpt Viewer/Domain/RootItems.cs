namespace Kpt_Viewer.Domain;

public sealed class RootItems(string displayName)
{
    public string DisplayName { get; } = displayName;
    public List<NodeModel> Items { get; } = new();
}