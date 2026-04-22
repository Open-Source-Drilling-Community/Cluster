using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using NORCE.Drilling.Cluster.ModelShared;




string localHostName = "https://localhost:5001/";
string devHostName = "https://dev.digiwells.no/";
string clusterHostBase = "Cluster/api/";


// Create clients to access databases from dev/ environment
Client clusterClient = ClientSetup(devHostName, clusterHostBase);
// Create clients to access databases from local/ environment
Client clusterLocalClient = ClientSetup(localHostName, clusterHostBase);
// Get all clusters and cartographic projection sets from the APIs
List<Cluster> clusters = (List<Cluster>) (await clusterClient.GetAllClusterAsync()).ToList();

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
List<Cluster> clustersByRigId = (List<Cluster>) (await clusterLocalClient.GetAllClusterByRigIdAsync(rigID)).ToList();
Console.WriteLine($"Number of clusters with slot ID {rigID} in local database: {clustersByRigId.Count}");
List<Cluster> clustersFixed = (List<Cluster>) (await clusterLocalClient.GetAllFixedPlatformClusterAsync(false)).ToList();
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
