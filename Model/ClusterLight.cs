using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using System;

namespace NORCE.Drilling.Cluster.Model
{
    /// <summary>
    /// Light weight version of a Cluster.
    /// Used to avoid transferring complete Cluster data when only contextual information is needed.
    /// </summary>
    public class ClusterLight
    {
        /// <summary>
        /// a MetaInfo for the ClusterLight
        /// </summary>
        public MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// name of the data
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// a description of the data
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// the date when the data was created
        /// </summary>
        public DateTimeOffset? CreationDate { get; set; }

        /// <summary>
        /// the date when the data was last modified
        /// </summary>
        public DateTimeOffset? LastModificationDate { get; set; }

        /// <summary>
        /// the ID of the field into which this cluster belongs to
        /// </summary>
        public Guid? FieldID { get; set; }

        /// <summary>
        /// if true, the cluster is not a true cluster, but a single well
        /// </summary>
        public bool IsSingleWell { get; set; }

        /// <summary>
        /// the ID of the rig associated with the cluster, if any
        /// </summary>
        public Guid? RigID { get; set; }

        /// <summary>
        /// true if the cluster is associated with a fixed platform
        /// </summary>
        public bool IsFixedPlatform { get; set; }

        /// <summary>
        /// optional reference point for the cluster in SI and WGS84 references
        /// </summary>
        public Point3DGlobalCoordinates? ReferencePoint { get; set; }

        /// <summary>
        /// the vertical depth the ground level or the mud line for the cluster in the WGS84 datum
        /// </summary>
        public GaussianDrillingProperty? GroundMudLineDepth { get; set; }

        /// <summary>
        /// the vertical depth of the top water level for the cluster in the WGS84 datum
        /// </summary>
        public GaussianDrillingProperty? TopWaterDepth { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public ClusterLight() : base()
        {
        }
    }
}
