namespace NORCE.Drilling.Cluster.WebPages;

internal class SlotTemplate
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid ID { get; set; } = Guid.Empty;
    public double? LatitudeWGS84 { get; set; }
    public double? LongitudeWGS84 { get; set; }
    public double? LatitudeDatum { get; set; }
    public double? LongitudeDatum { get; set; }
    public double? Northing { get; set; }
    public double? Easting { get; set; }
}
