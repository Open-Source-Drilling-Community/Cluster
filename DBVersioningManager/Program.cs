using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using OSDC.Drilling.Cluster.ModelShared;




string localHostName = "https://localhost:5001/";
string devHostName = "https://dev.digiwells.no/";
string clusterHostBase = "Cluster/api/";


// Create clients to access databases from dev/ environment
Client clusterClient = ClientSetup(devHostName, clusterHostBase);
// Create clients to access databases from local/ environment
Client clusterLocalClient = ClientSetup(localHostName, clusterHostBase);
// Get all clusters and cartographic projection sets from the APIs
// This explicit administrative copy reads full records individually after light discovery.
List<Cluster> clusters = [];
foreach (var summary in await clusterClient.ListClusterLightAsync())
    clusters.Add(await clusterClient.GetClusterByIdAsync(summary.MetaInfo!.ID));

// Update local database with data from dev/ database
try{
foreach (var cluster in clusters)
{
    // Update each cluster in local database with the one from dev database
    Console.WriteLine($"Updating cluster with ID {cluster.MetaInfo!.ID} in local database...");
    await clusterLocalClient.PostClusterAsync(cluster);
}
}
catch (Exception ex)
{
    throw;
}
// Test if the new method works...
Guid rigID = clusters[0].RigID!.Value;
List<ClusterLight> clustersByRigId = (List<ClusterLight>) (await clusterLocalClient.ListClusterLightAsync(rigId: rigID)).ToList();
Console.WriteLine($"Number of clusters with slot ID {rigID} in local database: {clustersByRigId.Count}");
List<ClusterLight> clustersFixed = (List<ClusterLight>) (await clusterLocalClient.ListClusterLightAsync(isFixedPlatform: false)).ToList();
Console.WriteLine($"Number of clusters with slot ID {rigID} in local database: {clustersFixed.Count}");



// Functions
Client ClientSetup(string _hostName, string _hostBase)
{
    HttpClient httpClient;
    Client api;
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
    httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(_hostName + _hostBase)
    };
    httpClient.DefaultRequestHeaders.Accept.Clear();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    api = new Client(httpClient.BaseAddress.ToString(), httpClient);
    return api;
}
