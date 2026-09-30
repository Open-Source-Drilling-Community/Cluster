using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using System;

namespace OSDC.Drilling.Cluster.Model
{
    /// <summary>
    /// Light weight version of a Cluster.
    /// Used to avoid transferring complete Cluster data when only contextual information is needed.
    /// </summary>
    [Semantic(Concepts.WellCluster)]
    public class ClusterLight
    {
        /// <summary>
        /// a MetaInfo for the ClusterLight
        /// </summary>
        [Semantic(Concepts.ResourceMetadata)]
        public MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// name of the data
        /// </summary>
        [Semantic(Concepts.ResourceName)]
        public string? Name { get; set; }

        /// <summary>
        /// a description of the data
        /// </summary>
        [Semantic(Concepts.ResourceDescription)]
        public string? Description { get; set; }

        /// <summary>
        /// the date when the data was created
        /// </summary>
        [Semantic(Concepts.Instant, Role = Concepts.CreationTime, Reference = Concepts.Utc)]
        public DateTimeOffset? CreationDate { get; set; }

        /// <summary>
        /// the date when the data was last modified
        /// </summary>
        [Semantic(Concepts.Instant, Role = Concepts.LastModificationTime, Reference = Concepts.Utc)]
        public DateTimeOffset? LastModificationDate { get; set; }

        /// <summary>
        /// the ID of the field into which this cluster belongs to
        /// </summary>
        [Semantic(Concepts.ResourceIdentifier)]
        public Guid? FieldID { get; set; }

        /// <summary>
        /// if true, the cluster is not a true cluster, but a single well
        /// </summary>
        [Semantic(Concepts.SingleWellClusterFlag)]
        public bool IsSingleWell { get; set; }

        /// <summary>
        /// the ID of the rig associated with the cluster, if any
        /// </summary>
        [Semantic(Concepts.ResourceIdentifier)]
        public Guid? RigID { get; set; }

        /// <summary>
        /// true if the cluster is associated with a fixed platform
        /// </summary>
        [Semantic(Concepts.FixedPlatformFlag)]
        public bool IsFixedPlatform { get; set; }

        /// <summary>
        /// optional reference point for the cluster in SI and WGS84 references
        /// </summary>
        [Semantic(Concepts.Position, Role = Concepts.ReferenceLocation, Reference = Concepts.Wgs84)]
        public Point3DGlobalCoordinates? ReferencePoint { get; set; }

        /// <summary>
        /// the vertical depth the ground level or the mud line for the cluster in the WGS84 datum
        /// </summary>
        [Semantic(Concepts.GaussianUncertainValue)]
        [GaussianQuantity(Concepts.GroundMudLineDepth, Concepts.LinearStandardUncertainty)]
        public GaussianDrillingProperty? GroundMudLineDepth { get; set; }

        /// <summary>
        /// the vertical depth of the top water level for the cluster in the WGS84 datum
        /// </summary>
        [Semantic(Concepts.GaussianUncertainValue)]
        [GaussianQuantity(Concepts.WaterSurfaceDepth, Concepts.LinearStandardUncertainty)]
        public GaussianDrillingProperty? TopWaterDepth { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public ClusterLight() : base()
        {
        }
    }
}
