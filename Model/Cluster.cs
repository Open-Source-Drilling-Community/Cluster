using DWIS.API.DTO;
using DWIS.Vocabulary.Schemas;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using OSDC.UnitConversion.Conversion;
using OSDC.UnitConversion.Conversion.DrillingEngineering;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Cluster.Model
{
    public class Cluster
    {
        /// <summary>
        /// a MetaInfo for the Cluster
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
        ///  the ID of the field into which this cluster belongs to
        /// </summary>
        public Guid? FieldID { get; set; }

        /// <summary>
        /// if true, the cluster is not a true cluster, but a single well
        /// </summary>
        public bool IsSingleWell { get; set; } = false;
        /// <summary>
        /// the ID of the rig associated with the cluster, if any
        /// </summary>
        public Guid? RigID { get; set; } = null;
        /// <summary>
        /// true if the cluster is associated with a fixed platform, false if it is a floating or moveable rig
        /// </summary>
        public bool IsFixedPlatform { get; set; } = false;

        /// <summary>
        /// the selected identities associated with the cluster
        /// </summary>
        public List<ClusterIdentityAssignment>? ClusterIdentityAssignments { get; set; }

        /// <summary>
        /// the selected cluster feature assignments associated with the cluster
        /// </summary>
        public List<ClusterFeatureAssignment>? ClusterFeatureAssignments { get; set; }

        /// <summary>
        /// optional reference point for the cluster in SI and WGS84 references
        /// </summary>
        public Point3DGlobalCoordinates? ReferencePoint { get; set; } = null;

        /// <summary>
        /// the vertical depth the ground level or the mud line for the cluster in the WGS84 datum
        /// </summary>
        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticGaussianVariable("ground_mud_line_depth_cluster", "sigma_ground_mud_line_depth_cluster")]
        [SemanticFact("ground_mud_line_depth_cluster", Nouns.Enum.DrillingSignal)]
        [SemanticFact("ground_mud_line_depth_cluster#01", Nouns.Enum.PhysicalData)]
        [SemanticFact("ground_mud_line_depth_cluster#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("ground_mud_line_depth_cluster#01", Verbs.Enum.HasDynamicValue, "ground_mud_line_depth_cluster")]
        [SemanticFact("ground_mud_line_depth_cluster#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.DepthDrilling)]
        [SemanticFact("MovingAverage", Nouns.Enum.MovingAverage)]
        [SemanticFact("ground_mud_line_depth_cluster#01", Verbs.Enum.IsTransformationOutput, "MovingAverage")]
        [SemanticFact("sigma_ground_mud_line_depth_cluster", Nouns.Enum.DrillingSignal)]
        [SemanticFact("sigma_ground_mud_line_depth_cluster#01", Nouns.Enum.DrillingDataPoint)]
        [SemanticFact("sigma_ground_mud_line_depth_cluster#01", Verbs.Enum.HasValue, "sigma_ground_mud_line_depth_cluster")]
        [SemanticFact("GaussianUncertainty#01", Nouns.Enum.GaussianUncertainty)]
        [SemanticFact("ground_mud_line_depth_cluster#01", Verbs.Enum.HasUncertainty, "GaussianUncertainty#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyStandardDeviation, "sigma_ground_mud_line_depth_cluster#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyMean, "ground_mud_line_depth_cluster#01")]
        [DefaultStandardDeviation(0.01)] // m (1 cm)
        public GaussianDrillingProperty? GroundMudLineDepth { get; set; } = null;

        /// <summary>
        /// the vertical depth of the top water level for the cluster in the WGS84 datum
        /// </summary>
        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticGaussianVariable("top_water_depth_cluster", "sigma_top_water_depth_cluster")]
        [SemanticFact("top_water_depth_cluster", Nouns.Enum.DrillingSignal)]
        [SemanticFact("top_water_depth_cluster#01", Nouns.Enum.PhysicalData)]
        [SemanticFact("top_water_depth_cluster#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("top_water_depth_cluster#01", Verbs.Enum.HasDynamicValue, "top_water_depth_cluster")]
        [SemanticFact("top_water_depth_cluster#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.DepthDrilling)]
        [SemanticFact("MovingAverage", Nouns.Enum.MovingAverage)]
        [SemanticFact("top_water_depth_cluster#01", Verbs.Enum.IsTransformationOutput, "MovingAverage")]
        [SemanticFact("sigma_top_water_depth_cluster", Nouns.Enum.DrillingSignal)]
        [SemanticFact("sigma_top_water_depth_cluster#01", Nouns.Enum.DrillingDataPoint)]
        [SemanticFact("sigma_top_water_depth_cluster#01", Verbs.Enum.HasValue, "sigma_top_water_depth_cluster")]
        [SemanticFact("GaussianUncertainty#01", Nouns.Enum.GaussianUncertainty)]
        [SemanticFact("top_water_depth_cluster#01", Verbs.Enum.HasUncertainty, "GaussianUncertainty#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyStandardDeviation, "sigma_top_water_depth_cluster#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyMean, "top_water_depth_cluster#01")]
        [DefaultStandardDeviation(0.01)] // m (1 cm)
        public GaussianDrillingProperty? TopWaterDepth { get; set; } = null;

        /// <summary>
        /// a dictionary of Slots in the cluster, identified by their ID
        /// </summary>
        public Dictionary<Guid, Slot>? Slots { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public Cluster() : base()
        {
        }
    }
}
