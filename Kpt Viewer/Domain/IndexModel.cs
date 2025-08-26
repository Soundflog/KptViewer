namespace Kpt_Viewer.Domain;

public sealed class IndexModel(IEnumerable<RootItems> roots)
{
    public IReadOnlyList<RootItems> Roots { get; } = roots.ToList();
}