using DWIS.Vocabulary.Schemas;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.UnitConversion.Conversion.DrillingEngineering;
using OSDC.UnitConversion.Conversion;
using System;

namespace NORCE.Drilling.Cluster.Model
{
    /// <summary>
    /// a base class other classes may derive from
    /// </summary>
    public class Slot
    {
        /// <summary>
        /// an ID that uniquely identifies the slot
        /// </summary>
        public Guid ID { get; set; }

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
        /// the latitude of the slot in the WGS84 datum
        /// </summary>
        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticGaussianVariable("relative_north_position_cluster", "sigma_relative_north_position_cluster")]
        [SemanticFact("relative_north_position_cluster", Nouns.Enum.DrillingSignal)]
        [SemanticFact("relative_north_position_cluster#01", Nouns.Enum.PhysicalData)]
        [SemanticFact("relative_north_position_cluster#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("relative_north_position_cluster#01", Verbs.Enum.HasDynamicValue, "relative_north_position_cluster")]
        [SemanticFact("relative_north_position_cluster#01", Verbs.Enum.IsOfMeasurableQuantity, BasePhysicalQuantity.QuantityEnum.PlaneAngleGeodesic)]
        [SemanticFact("MovingAverage", Nouns.Enum.MovingAverage)]
        [SemanticFact("relative_north_position_cluster#01", Verbs.Enum.IsTransformationOutput, "MovingAverage")]
        [SemanticFact("sigma_relative_north_position_cluster", Nouns.Enum.DrillingSignal)]
        [SemanticFact("sigma_relative_north_position_cluster#01", Nouns.Enum.DrillingDataPoint)]
        [SemanticFact("sigma_relative_north_position_cluster#01", Verbs.Enum.HasValue, "sigma_relative_north_position_cluster")]
        [SemanticFact("GaussianUncertainty#01", Nouns.Enum.GaussianUncertainty)]
        [SemanticFact("relative_north_position_cluster#01", Verbs.Enum.HasUncertainty, "GaussianUncertainty#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyStandardDeviation, "sigma_relative_north_position_cluster#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyMean, "relative_north_position_cluster#01")]
        [DefaultStandardDeviation(1.6e-9)] // rad (1 cm at equator)
        public GaussianDrillingProperty? Latitude { get; set; } = null;

        /// <summary>
        /// the longitude of the slot in the WGS84 datum
        /// </summary>
        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticGaussianVariable("relative_east_position_cluster", "sigma_relative_east_position_cluster")]
        [SemanticFact("relative_east_position_cluster", Nouns.Enum.DrillingSignal)]
        [SemanticFact("relative_east_position_cluster#01", Nouns.Enum.PhysicalData)]
        [SemanticFact("relative_east_position_cluster#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("relative_east_position_cluster#01", Verbs.Enum.HasDynamicValue, "relative_east_position_cluster")]
        [SemanticFact("relative_east_position_cluster#01", Verbs.Enum.IsOfMeasurableQuantity, BasePhysicalQuantity.QuantityEnum.LengthStandard)]
        [SemanticFact("MovingAverage", Nouns.Enum.MovingAverage)]
        [SemanticFact("relative_east_position_cluster#01", Verbs.Enum.IsTransformationOutput, "MovingAverage")]
        [SemanticFact("sigma_relative_east_position_cluster", Nouns.Enum.DrillingSignal)]
        [SemanticFact("sigma_relative_east_position_cluster#01", Nouns.Enum.DrillingDataPoint)]
        [SemanticFact("sigma_relative_east_position_cluster#01", Verbs.Enum.HasValue, "sigma_relative_east_position_cluster")]
        [SemanticFact("GaussianUncertainty#01", Nouns.Enum.GaussianUncertainty)]
        [SemanticFact("relative_east_position_cluster#01", Verbs.Enum.HasUncertainty, "GaussianUncertainty#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyStandardDeviation, "sigma_relative_east_position_cluster#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyMean, "relative_east_position_cluster#01")]
        [DefaultStandardDeviation(1.6e-9)] // rad (1 cm at equator)
        public GaussianDrillingProperty? Longitude { get; set; } = null;

        /// <summary>
        /// default constructor required for JSON serialization
        /// typical accuracy is one millimeter
        /// </summary>
        public Slot() : base()
        {
        }
    }
}
