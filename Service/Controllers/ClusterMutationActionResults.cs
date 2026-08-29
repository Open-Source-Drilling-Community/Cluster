using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.Cluster.Service.Managers;

namespace OSDC.Drilling.Cluster.Service.Controllers;

internal static class ClusterMutationActionResults
{
    public static ActionResult ToActionResult(this ControllerBase controller, ClusterMutationResult outcome) => outcome.FailureKind switch
    {
        ClusterMutationFailureKind.None => controller.Ok(),
        ClusterMutationFailureKind.InvalidRequest => controller.BadRequest(outcome.Error),
        ClusterMutationFailureKind.NotFound => controller.NotFound(outcome.Error),
        ClusterMutationFailureKind.Conflict => controller.Conflict(outcome.Error),
        _ => controller.StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
    };

    public static ActionResult ToActionResult<T>(this ControllerBase controller, ClusterMutationResult outcome, T? successValue) =>
        outcome.FailureKind == ClusterMutationFailureKind.None ? controller.Ok(successValue) : controller.ToActionResult(outcome);
}
