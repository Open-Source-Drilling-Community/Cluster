namespace NORCE.Drilling.Cluster.WebPages;

using NORCE.Drilling.Cluster.ModelShared;
using MathPoint3DGlobalCoordinates = OSDC.DotnetLibraries.General.Math.Point3DGlobalCoordinates;

internal class SlotTemplate
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid ID { get; set; } = Guid.Empty;
    public bool IsSelected { get; set; }
    public MathPoint3DGlobalCoordinates Position { get; set; } = new();
    public double? LatitudeWGS84 { get => Position.Latitude; set => Position.Latitude = value; }
    public double? LongitudeWGS84 { get => Position.Longitude; set => Position.Longitude = value; }
    public double? LatitudeDatum { get; set; }
    public double? LongitudeDatum { get; set; }
    public double? Northing { get => Position.RiemannianNorth; set => Position.RiemannianNorth = value; }
    public double? Easting { get => Position.RiemannianEast; set => Position.RiemannianEast = value; }
    public List<SlotFeatureAssignment> SlotFeatureAssignments { get; set; } = [];
}
