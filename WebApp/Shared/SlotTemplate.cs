namespace WebApp.Shared
{
    internal class SlotTemplate
    {
        public string? Name { get; set; } = null;
        public string? Description { get; set; } = null;
        public Guid ID { get; set; } = Guid.Empty;
        public double? LatitudeWGS84 { get; set; } = null;
        public double? LongitudeWGS84 { get; set; } = null;
        public double? LatitudeDatum { get; set; } = null; 
        public double? LongitudeDatum { get; set; } = null;
        public double? Northing { get; set; } = null;
        public double? Easting { get; set; } = null;
    }
}
