using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.Cluster.Model
{
    public class ClusterFeatureAssignment : IFeatureAssignment
    {
        /// <summary>
        /// stable identifier for the assignment
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// the selected cluster feature category
        /// </summary>
        public Guid? FeatureCategoryID { get; set; }

        /// <summary>
        /// the selected cluster feature option
        /// </summary>
        public Guid? FeatureOptionID { get; set; }

        /// <summary>
        /// first date for which the assignment is valid
        /// </summary>
        public DateTimeOffset? FromDate { get; set; }

        /// <summary>
        /// last date for which the assignment is valid
        /// </summary>
        public DateTimeOffset? ToDate { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public ClusterFeatureAssignment() : base()
        {
        }
    }
}
