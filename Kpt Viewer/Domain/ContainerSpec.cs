namespace Kpt_Viewer.Domain;

public sealed class ContainerSpec(string containerName, string itemName)
{
    public string ContainerName { get; } = containerName; // e.g., "land_records"
    public string ItemName { get; } = itemName; // e.g., "land_record"
}