using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;


//this system need to be running under .Net6 and also install the Newtonsoft.Json package for latest version.

class NpmDependencyFetcher
{
    private static HttpClient httpClient = new HttpClient();

    public static async Task<List<string>> GetAllDependencies(string packageName)
    {
        var fetchedPackages = new HashSet<string>();
        return await FetchDependencies(packageName, fetchedPackages);
    }

    private static async Task<List<string>> FetchDependencies(string packageName, HashSet<string> fetchedPackages)
    {
        try
        {

            var response = await httpClient.GetStringAsync($"http://registry.npmjs.org/{packageName}/latest");
            var packageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<PackageInfo>(response);

            // Extract direct dependencies
            var dependencies = packageInfo.dependencies ?? new Dependencies();

            // Add the current package to the set of fetched packages
            fetchedPackages.Add(packageName);

            var implicitDependencies = new List<string>();

            // Recursively fetch implicit dependencies
            foreach (var dependency in dependencies)
            {
                if (!fetchedPackages.Contains(dependency.Key))
                {
                    var recursiveDeps = await FetchDependencies(dependency.Key, fetchedPackages);
                    implicitDependencies.AddRange(recursiveDeps);
                }
            }

            // Combine direct and implicit dependencies, removing duplicates
            return new List<string>(dependencies.Keys) { Capacity = dependencies.Count + implicitDependencies.Count }
                .Concat(implicitDependencies)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching dependencies for {packageName}: {ex.Message}");
            return new List<string>();
        }
    }

    class PackageInfo
    {
        public Dependencies dependencies { get; set; }
    }

    class Dependencies : Dictionary<string, string> { }
}

class Program
{
    static async Task Main(string[] args)
    {
        var packageName = "forever";
        var dependencies = await NpmDependencyFetcher.GetAllDependencies(packageName);
        Console.WriteLine($"Dependencies for {packageName}:");
        foreach (var dependency in dependencies)
        {
            Console.WriteLine(dependency);
        }
    }
}