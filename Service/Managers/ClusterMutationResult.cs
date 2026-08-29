using OSDC.Drilling.Cluster.Model;
using System.Collections.Generic;

namespace OSDC.Drilling.Cluster.Service.Managers;

internal enum ClusterMutationFailureKind { None, InvalidRequest, NotFound, Conflict, StorageFailure }

internal sealed record ClusterMutationResult(ClusterMutationFailureKind FailureKind, ClusterMutationErrorEnvelope? Error = null)
{
    public static ClusterMutationResult Success() => new(ClusterMutationFailureKind.None);
    public static ClusterMutationResult Invalid(string property, string code, string message) =>
        Failure(ClusterMutationFailureKind.InvalidRequest, "invalid_request", "The mutation request is invalid.", property, code, message);
    public static ClusterMutationResult NotFound(string message) => new(ClusterMutationFailureKind.NotFound,
        new ClusterMutationErrorEnvelope { Error = "not_found", Message = message });
    public static ClusterMutationResult ConcurrencyConflict(string message) =>
        Failure(ClusterMutationFailureKind.Conflict, "concurrency_conflict", "The resource was modified by another caller.",
            "expectedModifiedUtc", "concurrency_conflict", message);
    public static ClusterMutationResult ReferenceConflict(ClusterMutationError error) => new(ClusterMutationFailureKind.Conflict,
        new ClusterMutationErrorEnvelope { Error = "reference_conflict", Message = "The mutation would break a Cluster-owned catalog reference.", Errors = [error] });
    public static ClusterMutationResult InvalidReferences(List<ClusterMutationError> errors) => new(ClusterMutationFailureKind.InvalidRequest,
        new ClusterMutationErrorEnvelope { Error = "invalid_reference", Message = "One or more Cluster-owned catalog references are invalid.", Errors = errors });
    public static ClusterMutationResult StorageFailure() => new(ClusterMutationFailureKind.StorageFailure,
        new ClusterMutationErrorEnvelope { Error = "storage_failure", Message = "The mutation could not be committed. No partial change was retained." });

    private static ClusterMutationResult Failure(ClusterMutationFailureKind kind, string error, string summary,
        string property, string code, string message) => new(kind, new ClusterMutationErrorEnvelope
        { Error = error, Message = summary, Errors = [new ClusterMutationError { Property = property, Code = code, Message = message }] });
}
