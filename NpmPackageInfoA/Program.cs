using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;


//this system need to be running under .Net6 and also install the Newtonsoft.Json package for latest version.

class NpmDependencyInfo
{
    private static HttpClient httpClient = new HttpClient();

    public static async Task<List<string>> GetAllDependencies(string packageName)
    {
        var fetchedPackagesInfo = new HashSet<string>();
        return await FetchDependencies(packageName, fetchedPackagesInfo);
    }

    private static async Task<List<string>> FetchDependencies(string packageName, HashSet<string> fetchedPackagesInfo)
    {
        try
        {
            var response = await httpClient.GetStringAsync($"http://registry.npmjs.org/{packageName}/latest");
            var packageInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<PackageInfo>(response);

            // Finding all direct dependencies
            var dependencies = packageInfo.dependencies ?? new Dependencies();

            // Add the package to the fetched packages group
            fetchedPackagesInfo.Add(packageName);

            var implicitDependenciesInfo = new List<string>();

            // fetch all related implicit dependencies
            foreach (var dependency in dependencies)
            {
                if (!fetchedPackagesInfo.Contains(dependency.Key))
                {
                    var relatedDeps = await FetchDependencies(dependency.Key, fetchedPackagesInfo);
                    implicitDependenciesInfo.AddRange(relatedDeps);
                }
            }

            // Combine both direct and implicit dependencies, and removing duplicates
            return new List<string>(dependencies.Keys) { Capacity = dependencies.Count + implicitDependenciesInfo.Count }
                .Concat(implicitDependenciesInfo)
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
        var dependenciesInfo = await NpmDependencyInfo.GetAllDependencies(packageName);
        Console.WriteLine($"Dependencies for {packageName}:");
        foreach (var dependencyInfo in dependenciesInfo)
        {
            Console.WriteLine(dependencyInfo);
        }
    }
}