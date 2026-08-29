using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace OSDC.Drilling.Cluster.Model
{
    public class ClusterIdentityAssignment : IIdentityAssignment
    {
        /// <summary>
        /// unique ID of the assignment
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        /// reference to the selected ClusterIdentity
        /// </summary>
        public Guid? IdentityID { get; set; }

        /// <summary>
        /// cluster-specific identity value
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public ClusterIdentityAssignment() : base()
        {
        }
    }
}
