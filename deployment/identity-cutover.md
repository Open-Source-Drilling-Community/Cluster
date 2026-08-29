# Cluster identity cutover

This procedure moves the Cluster microservice from the `NORCE.Drilling.Cluster` software identity to `OSDC.Drilling.Cluster` without changing the public routes, persisted database, or resource UUIDs.

## Identity map

| Concern | Previous | New |
| --- | --- | --- |
| Root namespace | `NORCE.Drilling.Cluster` | `OSDC.Drilling.Cluster` |
| WebPages package | `NORCE.Drilling.Cluster.WebPages` | `OSDC.Drilling.Cluster.WebPages` |
| Service image | `digiwells/norcedrillingclusterservice:stable` | `digiwells/osdcdrillingclusterservice:stable` |
| WebApp image | `digiwells/norcedrillingclusterwebappclient:stable` | `digiwells/osdcdrillingclusterwebappclient:stable` |
| Service Helm release | `norcedrillingclusterservice` | `osdcdrillingclusterservice` |
| WebApp Helm release | `norcedrillingclusterwebappclient` | `osdcdrillingclusterwebappclient` |
| Service Deployment/Service | `norcedrillingclusterservice` | `osdcclusterservice` |
| WebApp Deployment/Service | `norcedrillingclusterwebappclient` | `osdcclusterwebappclient` |
| PersistentVolumeClaim | `cluster-claim` | `cluster-claim` (unchanged) |
| REST/MCP path | `/Cluster/api/...` | `/Cluster/api/...` (unchanged) |
| WebApp path | `/Cluster/webapp/...` | `/Cluster/webapp/...` (unchanged) |

There are no compatibility routes or duplicate databases. The new service adopts the existing `cluster-claim` PVC.

## Prerequisites

1. Commit and push the cutover code.
2. Run the Docker publishing workflow and verify both new `stable` images exist on Docker Hub.
3. Publish a new `OSDC.Drilling.Cluster.WebPages` NuGet version if external hosts consume the package.
4. Back up all clusters through the batch-export page or REST API.
5. Update every dependent deployment that uses `http://norcedrillingclusterservice/` to use `http://osdcclusterservice/`. In particular, update and redeploy the Field WebApp before removing the old Cluster service.
6. Perform one Kubernetes context at a time: `dev-context`, then `prod-context`, then `awe-context`.

## Save the current state

Run from `C:\OSDC\Cluster` in PowerShell, changing `$context` for each server:

```powershell
$context = "dev-context"
$namespace = "default"
$stamp = Get-Date -Format "yyyyMMddTHHmmssZ"
$backupDirectory = Join-Path $PWD "deployment\backups\$context-$stamp"
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null

helm --kube-context $context get values norcedrillingclusterservice `
  -n $namespace --all -o yaml |
  Out-File "$backupDirectory\old-service-values.yaml" -Encoding utf8

helm --kube-context $context get manifest norcedrillingclusterservice `
  -n $namespace |
  Out-File "$backupDirectory\old-service-manifest.yaml" -Encoding utf8

helm --kube-context $context get values norcedrillingclusterwebappclient `
  -n $namespace --all -o yaml |
  Out-File "$backupDirectory\old-webapp-values.yaml" -Encoding utf8

helm --kube-context $context get manifest norcedrillingclusterwebappclient `
  -n $namespace |
  Out-File "$backupDirectory\old-webapp-manifest.yaml" -Encoding utf8

kubectl --context $context get pvc cluster-claim -n $namespace -o yaml |
  Out-File "$backupDirectory\cluster-claim.yaml" -Encoding utf8

kubectl --context $context get deployment,service,ingress,pod,pvc `
  -n $namespace -o wide |
  Out-File "$backupDirectory\cluster-resources.txt" -Encoding utf8
```

Also copy the SQLite database from the running old service pod:

```powershell
$oldPod = kubectl --context $context get pod -n $namespace `
  -l "app.kubernetes.io/instance=norcedrillingclusterservice" `
  -o jsonpath='{.items[0].metadata.name}'

kubectl --context $context cp `
  "${namespace}/${oldPod}:/home/Cluster.db" `
  "$backupDirectory\Cluster.db"

if (-not (Test-Path "$backupDirectory\Cluster.db")) {
  throw "The SQLite backup was not copied."
}
```

## Make the old release preserve the PVC

The original chart did not mark `cluster-claim` with Helm's keep policy. Before uninstalling it, upgrade the old release once with the new chart but retain its old Kubernetes names. This records the keep annotation in the Helm release manifest.

```powershell
$serviceChart = Join-Path $PWD "Service\charts\osdcdrillingclusterservice"

helm upgrade norcedrillingclusterservice $serviceChart `
  --kube-context $context `
  -n $namespace `
  --reuse-values `
  --set-string nameOverride=norcedrillingclusterservice `
  --set-string fullnameOverride=norcedrillingclusterservice `
  --set-string image.repository=docker.io/digiwells/osdcdrillingclusterservice `
  --set-string image.tag=stable `
  --set-string strategy.type=Recreate `
  --set persistence.enabled=true `
  --set-string persistence.existingClaim= `
  --set-string persistence.claimName=cluster-claim

helm --kube-context $context get manifest norcedrillingclusterservice `
  -n $namespace |
  Select-String "helm.sh/resource-policy: keep"
```

Do not continue unless the keep annotation is present in `helm get manifest` and the old service has successfully rolled out.

## Install and verify the new service without taking over ingress

```powershell
kubectl --context $context scale deployment/norcedrillingclusterservice `
  --replicas=0 -n $namespace

kubectl --context $context wait --for=delete pod `
  -l "app.kubernetes.io/instance=norcedrillingclusterservice" `
  -n $namespace --timeout=180s

helm upgrade --install osdcdrillingclusterservice $serviceChart `
  --kube-context $context `
  -n $namespace `
  --set-string image.repository=docker.io/digiwells/osdcdrillingclusterservice `
  --set-string image.tag=stable `
  --set-string persistence.existingClaim=cluster-claim `
  --set ingress.enabled=false

kubectl --context $context rollout status deployment/osdcclusterservice `
  -n $namespace --timeout=300s

kubectl --context $context get pod -n $namespace `
  -l "app.kubernetes.io/instance=osdcdrillingclusterservice" -o wide

kubectl --context $context logs deployment/osdcclusterservice `
  -n $namespace --since=10m
```

Verify the database through a temporary local port-forward:

```powershell
kubectl --context $context port-forward service/osdcclusterservice `
  -n $namespace 5502:80
```

In a second PowerShell window:

```powershell
$clusters = @(Invoke-RestMethod `
  -Uri "http://localhost:5502/Cluster/api/Cluster/LightData" `
  -Method Get)
$clusters | Select-Object Name, FieldID, RigID | Format-Table -AutoSize
```

Stop the port-forward with Ctrl+C after verification.

## Switch ingress and WebApp

Only continue after dependent services have been updated to the new internal DNS name.

```powershell
helm uninstall norcedrillingclusterservice `
  --kube-context $context -n $namespace --wait

kubectl --context $context get pvc cluster-claim -n $namespace

helm upgrade osdcdrillingclusterservice $serviceChart `
  --kube-context $context `
  -n $namespace `
  --reuse-values `
  --set ingress.enabled=true

$webChart = Join-Path $PWD "WebApp\charts\osdcdrillingclusterwebappclient"

helm upgrade --install osdcdrillingclusterwebappclient $webChart `
  --kube-context $context `
  -n $namespace `
  --set-string image.repository=docker.io/digiwells/osdcdrillingclusterwebappclient `
  --set-string image.tag=stable `
  --set ingress.enabled=false

kubectl --context $context rollout status deployment/osdcclusterwebappclient `
  -n $namespace --timeout=300s

helm uninstall norcedrillingclusterwebappclient `
  --kube-context $context -n $namespace --wait

helm upgrade osdcdrillingclusterwebappclient $webChart `
  --kube-context $context `
  -n $namespace `
  --reuse-values `
  --set ingress.enabled=true
```

## Final verification

```powershell
kubectl --context $context get deployment,service,ingress,pod,pvc `
  -n $namespace -o wide |
  Select-String "cluster"

helm --kube-context $context list -n $namespace --filter "cluster"
```

Verify externally:

- `https://<host>/Cluster/api/Cluster/LightData`
- `https://<host>/Cluster/api/swagger`
- `https://<host>/Cluster/webapp/Cluster`
- `https://<host>/Cluster/api/mcp`

Confirm the field/rig associations and the expected cluster count before proceeding to the next context.

## Rollback

If validation fails before uninstalling the old release, uninstall the new releases and scale the old deployment back to one replica. If failure occurs after the old release is removed, reinstall it from the saved values/previous chart and use `cluster-claim`; the PVC is protected by the keep policy. The independently copied `Cluster.db` and JSON batch export are the final recovery paths.
