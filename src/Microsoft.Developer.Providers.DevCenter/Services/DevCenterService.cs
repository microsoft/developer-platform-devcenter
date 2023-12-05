// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Core;
using Azure.ResourceManager.ResourceGraph;
using Azure.ResourceManager.ResourceGraph.Models;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.Developer.Azure;
using Microsoft.Developer.Entities;

namespace Microsoft.Developer.Providers.DevCenter;

public class DevCenterService(IUserArmService arm, ClaimsPrincipal user, ILogger<DevCenterService> log) : IDevCenterService
{
    // The AAD object id of the user.
    // If value is 'me', the identity is taken from the authentication context.
    // The default value is "me".
    internal const string DefaultUserId = "me";
    internal static int? DefaultMaxCount = null;
    internal const string DefaultFilter = null;

    private readonly ConcurrentDictionary<string, DevCenterClient> devCenterClients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DevBoxesClient> devBoxClients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DeploymentEnvironmentsClient> deploymentEnvironmentsClients = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, DevCenterInfo> devCenters = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, List<Project>> projectCache = new(StringComparer.OrdinalIgnoreCase);

    public async Task<(DevCenterClient client, DevCenterInfo devCenter)> GetDevCenterClient(string devCenterName, CancellationToken token = default)
        => GetDevCenterClient(await GetDevCenterInfo(devCenterName, token));

    public (DevCenterClient client, DevCenterInfo devCenter) GetDevCenterClient(DevCenterInfo devCenter)
    {
        var key = devCenter.Endpoint.AbsoluteUri;

        if (!devCenterClients.TryGetValue(key, out var client))
        {
            client = new DevCenterClient(devCenter.Endpoint, arm.GetTokenCredential(user));
            devCenterClients[key] = client;
        }

        return (client, devCenter);
    }


    public async Task<(DeploymentEnvironmentsClient client, DevCenterInfo devCenter)> GetDeploymentEnvironmentsClient(string devCenterName, CancellationToken token = default)
        => GetDeploymentEnvironmentsClient(await GetDevCenterInfo(devCenterName, token));

    public (DeploymentEnvironmentsClient client, DevCenterInfo devCenter) GetDeploymentEnvironmentsClient(DevCenterInfo devCenter)
    {
        var key = devCenter.Endpoint.AbsoluteUri;

        if (!deploymentEnvironmentsClients.TryGetValue(key, out var client))
        {
            client = new DeploymentEnvironmentsClient(devCenter.Endpoint, arm.GetTokenCredential(user));
            deploymentEnvironmentsClients[key] = client;
        }

        return (client, devCenter);
    }


    public async Task<(DevBoxesClient client, DevCenterInfo devCenter)> GetDevBoxClient(string devCenterName, CancellationToken token = default)
        => GetDevBoxClient(await GetDevCenterInfo(devCenterName, token));

    public (DevBoxesClient client, DevCenterInfo devCenter) GetDevBoxClient(DevCenterInfo devCenter)
    {
        var key = devCenter.Endpoint.AbsoluteUri;

        if (!devBoxClients.TryGetValue(key, out var client))
        {
            client = new DevBoxesClient(devCenter.Endpoint, arm.GetTokenCredential(user));
            devBoxClients[key] = client;
        }

        return (client, devCenter);
    }


    private async Task<DevCenterInfo> GetDevCenterInfo(string devCenterName, CancellationToken token = default)
    {
        if (!devCenters.TryGetValue(devCenterName, out var info))
        {
            log.LogWarning("Endpoint for DevCenter '{DevCenter}' not found in cache. Querying Azure Resource Graph.", devCenterName);

            var devCenter = await GetDevCenterAsync(devCenterName, token)
                ?? throw new ArgumentException($"DevCenter '{devCenterName}' not found.");

            devCenters[devCenterName] = info = devCenter.Info();
        }

        return info;
    }


    internal async Task<List<T>> GetResourceGraphResourcesAsync<T>(string queryString, CancellationToken token = default)
    {
        var tenant = arm
            .GetArmClient(user)
            .GetTenants()
            .First();

        log.LogWarning("Calling Resource Graph\n{queryString}", queryString);

        var query = new ResourceQueryContent(queryString);

        var response = await tenant.GetResourcesAsync(query, token).ConfigureAwait(false);

        var duration = response.ResourceGraphRequestDuration() ?? new();

        var resources = response.Value.Data.ToObject<List<T>>() ?? [];

        while (!string.IsNullOrEmpty(response.Value.SkipToken))
        {
            query.Options.SkipToken = response.Value.SkipToken;

            response = await tenant.GetResourcesAsync(query, token).ConfigureAwait(false);

            duration += response.ResourceGraphRequestDuration() ?? new();

            var newResources = response.Value.Data.ToObject<List<T>>();

            if (newResources?.Count > 0)
            {
                resources.AddRange(newResources);
            }
        }

        log.LogWarning("Resource Graph query took {duration}ms", duration.Milliseconds);

        return resources ?? [];
    }


    // Dev Centers

    public async Task<Model.DevCenter?> GetDevCenterAsync(string devCenterName, CancellationToken token = default)
    {
        var devCenters = await GetResourceGraphResourcesAsync<Model.DevCenter>(ResourceGraphQueries.DevCenterByProjects(devCenterName), token).ConfigureAwait(false);

        var devCenter = devCenters.FirstOrDefault();

        // populate the devcenter endpoint cache
        if (devCenter is not null)
        {
            this.devCenters[devCenter.Name] = devCenter.Info();
        }

        return devCenters.FirstOrDefault();
    }

    public async Task<List<Model.DevCenter>> GetDevCentersAsync(CancellationToken token = default)
    {
        var devCenters = await GetResourceGraphResourcesAsync<Model.DevCenter>(ResourceGraphQueries.DevCentersByProjects, token).ConfigureAwait(false);

        // populate the devcenter endpoint cache
        foreach (var devCenter in devCenters)
        {
            this.devCenters[devCenter.Name] = devCenter.Info();
        }

        return devCenters;
    }


    // Projects

    public async Task<List<Project>> GetProjectsAsync(CancellationToken token = default)
    {
        var projects = await GetResourceGraphResourcesAsync<Project>(ResourceGraphQueries.Projects, token).ConfigureAwait(false);

        // populate the devcenter endpoint cache
        var devCenters = projects
            .Select(p => p.DevCenterInfo())
            .Distinct();

        foreach (var devCenter in devCenters)
        {
            this.devCenters[devCenter.Name] = devCenter;
            projectCache[devCenter.Name] = projects.Where(p => p.DevCenterName.Equals(devCenter.Name, StringComparison.OrdinalIgnoreCase)).ToList() ?? [];
        }

        return projects;
    }

    // private List<Project>? projectCache;

    public async Task<List<Project>> GetProjectsAsync(string devCenterName, CancellationToken token = default)
    {
        if (!projectCache.TryGetValue(devCenterName, out var projects) && projects is null)
        {
            projectCache[devCenterName] = projects = await GetResourceGraphResourcesAsync<Project>(ResourceGraphQueries.ProjectsByDevCenter(devCenterName), token).ConfigureAwait(false);
        }

        // populate the devcenter endpoint cache
        if (projects.Count != 0)
        {
            devCenters[devCenterName] = projects.First().DevCenterInfo();
        }

        return projects;
    }

    public async Task<Project?> GetProjectAsync(string devCenterName, string projectName, CancellationToken token = default)
    {
        var projects = await GetResourceGraphResourcesAsync<Project>(ResourceGraphQueries.ProjectByDevCenter(devCenterName, projectName), token).ConfigureAwait(false);

        var project = projects.FirstOrDefault();

        // populate the devcenter endpoint cache
        if (project is not null)
        {
            devCenters[project.DevCenterName] = project.DevCenterInfo();
        }

        return project;
    }

    public async IAsyncEnumerable<ProjectSimple> GetProjectsFromDevCenterAsync(string devCenterName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = await GetDevCenterClient(devCenterName, token).ConfigureAwait(false);

        await foreach (var data in client.GetProjectsAsync(DefaultFilter, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var project = data.ToObject<ProjectSimple>(devCenter);
            if (project is not null)
            {
                yield return project;
            }
        }
    }

    public async Task<ProjectSimple?> GetProjectFromDevCenterAsync(string devCenterName, string projectName, CancellationToken token = default)
    {
        var (client, devCenter) = await GetDevCenterClient(devCenterName, token).ConfigureAwait(false);

        var response = await client.GetProjectAsync(projectName, GetContext(token)).ConfigureAwait(false);
        var project = response.Content.ToObject<ProjectSimple>(devCenter);

        return project;
    }


    // Dev Boxes

    public async IAsyncEnumerable<DevBox> GetDevBoxesAsync([EnumeratorCancellation] CancellationToken token = default)
    {
        var projects = await GetProjectsAsync(token).ConfigureAwait(false);

        foreach (var project in projects)
        {
            await foreach (var box in GetDevBoxesAsync(project, token).ConfigureAwait(false))
            {
                yield return box;
            }
        }
    }

    public async IAsyncEnumerable<DevBox> GetDevBoxesAsync(string devCenterName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = await GetDevBoxClient(devCenterName, token).ConfigureAwait(false);

        await foreach (var box in client.GetAllDevBoxesByUserAsync(DefaultUserId, DefaultFilter, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var devbox = box.ToObject<DevBox>(devCenter);
            if (devbox is not null)
            {
                yield return devbox;
            }
        }
    }

    public async IAsyncEnumerable<DevBox> GetDevBoxesAsync(string devCenterName, string projectName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = await GetDevBoxClient(devCenterName, token).ConfigureAwait(false);

        await foreach (var box in client.GetDevBoxesAsync(projectName, DefaultUserId, DefaultFilter, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var devbox = box.ToObject<DevBox>(devCenter, projectName);
            if (devbox is not null)
            {
                yield return devbox;
            }
        }
    }

    public async IAsyncEnumerable<DevBox> GetDevBoxesAsync(Project project, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = GetDevBoxClient(project.DevCenterInfo());

        await foreach (var box in client.GetDevBoxesAsync(project.Name, DefaultUserId, DefaultFilter, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var devbox = box.ToObject<DevBox>(devCenter, project.Name);
            if (devbox is not null)
            {
                yield return devbox;
            }
        }
    }

    public async Task<DevBox?> GetDevBoxAsync(string devCenterName, string projectName, string devBoxName, CancellationToken token = default)
    {
        var (client, devCenter) = await GetDevBoxClient(devCenterName, token).ConfigureAwait(false);

        var response = await client.GetDevBoxAsync(projectName, DefaultUserId, devBoxName, GetContext(token)).ConfigureAwait(false);
        var devbox = response.Content.ToObject<DevBox>(devCenter, projectName);

        return devbox;
    }

    public async Task<DevBox?> GetDevBoxAsync(Project project, string devBoxName, CancellationToken token = default)
    {
        var (client, devCenter) = GetDevBoxClient(project.DevCenterInfo());

        var response = await client.GetDevBoxAsync(project.Name, DefaultUserId, devBoxName, GetContext(token)).ConfigureAwait(false);
        var devbox = response.Content.ToObject<DevBox>(devCenter, project.Name);

        return devbox;
    }


    // Pools

    public async IAsyncEnumerable<Pool> GetPoolsAsync(string devCenterName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var projects = await GetProjectsAsync(devCenterName, token).ConfigureAwait(false);

        foreach (var project in projects)
        {
            await foreach (var pool in GetPoolsAsync(project, token).ConfigureAwait(false))
            {
                yield return pool;
            }
        }
    }

    public async IAsyncEnumerable<Pool> GetPoolsAsync(string devCenterName, string projectName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = await GetDevBoxClient(devCenterName, token).ConfigureAwait(false);

        await foreach (var item in client.GetPoolsAsync(projectName, DefaultFilter, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var pool = item.ToObject<Pool>(devCenter, projectName);
            if (pool is not null)
            {
                yield return pool;
            }
        }
    }

    public async IAsyncEnumerable<Pool> GetPoolsAsync(Project project, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = GetDevBoxClient(project.DevCenterInfo());

        await foreach (var item in client.GetPoolsAsync(project.Name, DefaultFilter, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var pool = item.ToObject<Pool>(devCenter, project.Name);
            if (pool is not null)
            {
                yield return pool;
            }
        }
    }

    public async Task<Pool?> GetPoolAsync(string devCenterName, string projectName, string poolName, CancellationToken token = default)
    {
        var (client, devCenter) = await GetDevBoxClient(devCenterName, token).ConfigureAwait(false);

        var response = await client.GetPoolAsync(projectName, poolName, GetContext(token)).ConfigureAwait(false);
        var pool = response.Content.ToObject<Pool>(devCenter, projectName);

        return pool;
    }

    public async Task<Pool?> GetPoolAsync(Project project, string poolName, CancellationToken token = default)
    {
        var (client, devCenter) = GetDevBoxClient(project.DevCenterInfo());

        var response = await client.GetPoolAsync(project.Name, poolName, GetContext(token)).ConfigureAwait(false);
        var pool = response.Content.ToObject<Pool>(devCenter, project.Name);

        return pool;
    }


    // Environments

    public async IAsyncEnumerable<Model.Environment> GetEnvironmentsAsync([EnumeratorCancellation] CancellationToken token = default)
    {
        var projects = await GetProjectsAsync(token).ConfigureAwait(false);

        foreach (var project in projects)
        {
            await foreach (var env in GetEnvironmentsAsync(project, token).ConfigureAwait(false))
            {
                yield return env;
            }
        }
    }

    public async IAsyncEnumerable<Model.Environment> GetEnvironmentsAsync(string devCenterName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var projects = await GetProjectsAsync(devCenterName, token).ConfigureAwait(false);

        foreach (var project in projects)
        {
            await foreach (var env in GetEnvironmentsAsync(project, token).ConfigureAwait(false))
            {
                yield return env;
            }
        }
    }

    public async IAsyncEnumerable<Model.Environment> GetEnvironmentsAsync(string devCenterName, string projectName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = await GetDeploymentEnvironmentsClient(devCenterName, token);

        await foreach (var env in client.GetEnvironmentsAsync(projectName, DefaultUserId, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var environment = env.ToObject<Model.Environment>(devCenter, projectName);
            if (environment is not null)
            {
                yield return environment;
            }
        }
    }

    public async IAsyncEnumerable<Model.Environment> GetEnvironmentsAsync(Project project, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = GetDeploymentEnvironmentsClient(project.DevCenterInfo());

        await foreach (var env in client.GetEnvironmentsAsync(project.Name, DefaultUserId, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var environment = env.ToObject<Model.Environment>(devCenter, project.Name);
            if (environment is not null)
            {
                yield return environment;
            }
        }
    }

    public async Task<Model.Environment?> GetEnvironmentAsync(string devCenterName, string projectName, string environmentName, CancellationToken token = default)
    {
        var (client, devCenter) = await GetDeploymentEnvironmentsClient(devCenterName, token);

        var response = await client.GetEnvironmentAsync(projectName, DefaultUserId, environmentName, GetContext(token));
        var environment = response.Content.ToObject<Model.Environment>(devCenter, projectName);

        return environment;
    }

    public async Task<Model.Environment?> GetEnvironmentAsync(DevCenterInfo devCenterInfo, string projectName, string environmentName, CancellationToken token = default)
    {
        var (client, devCenter) = GetDeploymentEnvironmentsClient(devCenterInfo);

        var response = await client.GetEnvironmentAsync(projectName, DefaultUserId, environmentName, GetContext(token));
        var environment = response.Content.ToObject<Model.Environment>(devCenter, projectName);

        return environment;
    }

    public async Task<Model.Environment?> GetEnvironmentAsync(Project project, string environmentName, CancellationToken token = default)
    {
        var (client, devCenter) = GetDeploymentEnvironmentsClient(project.DevCenterInfo());

        var response = await client.GetEnvironmentAsync(project.Name, DefaultUserId, environmentName, GetContext(token)).ConfigureAwait(false);
        var environment = response.Content.ToObject<Model.Environment>(devCenter, project.Name);

        return environment;
    }


    // Catalog

    public async IAsyncEnumerable<Catalog> GetCatalogsAsync(string devCenterName, string projectName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = await GetDeploymentEnvironmentsClient(devCenterName, token).ConfigureAwait(false);

        await foreach (var item in client.GetCatalogsAsync(projectName, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var catalog = item.ToObject<Catalog>(devCenter, projectName);
            if (catalog is not null)
            {
                yield return catalog;
            }
        }
    }

    public async IAsyncEnumerable<Catalog> GetCatalogsAsync(Project project, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = GetDeploymentEnvironmentsClient(project.DevCenterInfo());

        await foreach (var item in client.GetCatalogsAsync(project.Name, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var catalog = item.ToObject<Catalog>(devCenter, project.Name);
            if (catalog is not null)
            {
                yield return catalog;
            }
        }
    }

    public async Task<Catalog?> GetCatalogAsync(string devCenterName, string projectName, string catalogName, CancellationToken token = default)
    {
        var (client, devCenter) = await GetDeploymentEnvironmentsClient(devCenterName, token).ConfigureAwait(false);

        var response = await client.GetCatalogAsync(projectName, catalogName, GetContext(token)).ConfigureAwait(false);
        var catalog = response.Content.ToObject<Catalog>(devCenter, projectName);

        return catalog;
    }

    public async Task<Catalog?> GetCatalogAsync(Project project, string catalogName, CancellationToken token = default)
    {
        var (client, devCenter) = GetDeploymentEnvironmentsClient(project.DevCenterInfo());

        var response = await client.GetCatalogAsync(project.Name, catalogName, GetContext(token)).ConfigureAwait(false);
        var catalog = response.Content.ToObject<Catalog>(devCenter, project.Name);

        return catalog;
    }


    // Environment Definitions

    public async IAsyncEnumerable<EnvironmentDefinition> GetEnvironmentDefinitionsAsync(string devCenterName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var projects = await GetProjectsAsync(devCenterName, token).ConfigureAwait(false);

        foreach (var project in projects)
        {
            await foreach (var definition in GetEnvironmentDefinitionsAsync(project, token).ConfigureAwait(false))
            {
                yield return definition;
            }
        }
    }

    public async IAsyncEnumerable<EnvironmentDefinition> GetEnvironmentDefinitionsAsync(IList<Project> projects, [EnumeratorCancellation] CancellationToken token = default)
    {
        foreach (var project in projects)
        {
            await foreach (var definition in GetEnvironmentDefinitionsAsync(project, token).ConfigureAwait(false))
            {
                yield return definition;
            }
        }
    }

    public async IAsyncEnumerable<EnvironmentDefinition> GetEnvironmentDefinitionsAsync(string devCenterName, string projectName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = await GetDeploymentEnvironmentsClient(devCenterName, token).ConfigureAwait(false);

        await foreach (var env in client.GetEnvironmentDefinitionsAsync(projectName, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var definition = env.ToObject<EnvironmentDefinition>(devCenter, projectName);
            if (definition is not null)
            {
                yield return definition;
            }
        }
    }

    public async IAsyncEnumerable<EnvironmentDefinition> GetEnvironmentDefinitionsAsync(Project project, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = GetDeploymentEnvironmentsClient(project.DevCenterInfo());

        await foreach (var env in client.GetEnvironmentDefinitionsAsync(project.Name, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var definition = env.ToObject<EnvironmentDefinition>(devCenter, project.Name);
            if (definition is not null)
            {
                yield return definition;
            }
        }
    }

    public async Task<EnvironmentDefinition?> GetEnvironmentDefinitionAsync(string devCenterName, string projectName, string catalogName, string definitionName, CancellationToken token = default)
    {
        var (client, devCenter) = await GetDeploymentEnvironmentsClient(devCenterName, token).ConfigureAwait(false);

        var response = await client.GetEnvironmentDefinitionAsync(projectName, catalogName, definitionName, GetContext(token)).ConfigureAwait(false);
        var definition = response.Content.ToObject<EnvironmentDefinition>(devCenter, projectName);

        return definition;
    }

    public async Task<EnvironmentDefinition?> GetEnvironmentDefinitionAsync(Project project, string catalogName, string definitionName, CancellationToken token = default)
    {
        var (client, devCenter) = GetDeploymentEnvironmentsClient(project.DevCenterInfo());

        var response = await client.GetEnvironmentDefinitionAsync(project.Name, catalogName, definitionName, GetContext(token)).ConfigureAwait(false);
        var definition = response.Content.ToObject<EnvironmentDefinition>(devCenter, project.Name);

        return definition;
    }


    // Environment Types

    public async IAsyncEnumerable<EnvironmentType> GetEnvironmentTypesAsync(string devCenterName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var projects = await GetProjectsAsync(devCenterName, token).ConfigureAwait(false);

        foreach (var project in projects)
        {
            await foreach (var type in GetEnvironmentTypesAsync(project, token).ConfigureAwait(false))
            {
                yield return type;
            }
        }
    }

    public async IAsyncEnumerable<EnvironmentType> GetEnvironmentTypesAsync(IList<Project> projects, [EnumeratorCancellation] CancellationToken token = default)
    {
        foreach (var project in projects)
        {
            await foreach (var type in GetEnvironmentTypesAsync(project, token).ConfigureAwait(false))
            {
                yield return type;
            }
        }
    }

    public async IAsyncEnumerable<EnvironmentType> GetEnvironmentTypesAsync(string devCenterName, string projectName, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = await GetDeploymentEnvironmentsClient(devCenterName, token).ConfigureAwait(false);

        await foreach (var type in client.GetEnvironmentTypesAsync(projectName, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var envType = type.ToObject<EnvironmentType>(devCenter, projectName);
            if (envType is not null)
            {
                yield return envType;
            }
        }
    }

    public async IAsyncEnumerable<EnvironmentType> GetEnvironmentTypesAsync(Project project, [EnumeratorCancellation] CancellationToken token = default)
    {
        var (client, devCenter) = GetDeploymentEnvironmentsClient(project.DevCenterInfo());

        await foreach (var type in client.GetEnvironmentTypesAsync(project.Name, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            var envType = type.ToObject<EnvironmentType>(devCenter, project.Name);
            if (envType is not null)
            {
                yield return envType;
            }
        }
    }


    public async Task<DevCenterInfo> BeginCreateEnvironmentAsync(string devCenterName, string projectName, string environmentName, CreateEnvironmentContent content, CancellationToken token = default)
    {
        var (client, devCenter) = await GetDeploymentEnvironmentsClient(devCenterName, token).ConfigureAwait(false);

        var requestContent = RequestContent.Create(content);

        var _ = await client
            .CreateOrUpdateEnvironmentAsync(WaitUntil.Started, projectName, DefaultUserId, environmentName, requestContent, GetContext(token))
            .ConfigureAwait(false);

        return devCenter;
    }

    public async Task<List<Entity>> GetTemplateEntitiesAsync(string devCenterName, CancellationToken token = default)
    {
        // log.LogWarning("START DoStuffAsync ({devCenterName})", devCenterName);

        var projects = await GetProjectsAsync(devCenterName, token)
            .ConfigureAwait(false);

        var project = projects.First();

        var (client, devCenter) = GetDeploymentEnvironmentsClient(project.DevCenterInfo());

        // currently environment definitions aren't project-specific,
        // they apply to all projects in a dev center, so just get
        // the environment definitions for the first project
        var definitions = new List<EnvironmentDefinition>();

        await foreach (var env in client.GetEnvironmentDefinitionsAsync(project.Name, DefaultMaxCount, GetContext(token)).ConfigureAwait(false))
        {
            definitions.Add(env.ToObject<EnvironmentDefinition>(devCenter)!);
        }

        var tasks = projects.Select(p => GetProjectWithEnvironmentTypesAsync(p, token));

        var projectsWithTypes = await Task.WhenAll(tasks)
            .ConfigureAwait(false);

        var things = definitions.Select(d => d.ToEntity(projectsWithTypes)).ToList();

        return things;
    }

    public async Task<List<Entity>> GetEnvironmentEntitiesAsync(string devCenterName, CancellationToken token = default)
    {
        var entities = new List<Entity>();

        await foreach (var env in GetEnvironmentsAsync(devCenterName, token).ConfigureAwait(false))
        {
            entities.Add(env.ToEntity());
            // yield return env.ToEntity();
        }

        return entities;
    }

    private async Task<ProjectWithEnvironmentTypes> GetProjectWithEnvironmentTypesAsync(Project project, CancellationToken token = default)
    {
        var types = new List<EnvironmentType>();

        await foreach (var type in GetEnvironmentTypesAsync(project, token).ConfigureAwait(false))
        {
            types.Add(type);
        }

        var projectWithTypes = new ProjectWithEnvironmentTypes
        {
            Project = project,
            EnvironmentTypes = types
        };

        return projectWithTypes;
    }

    private static RequestContext? GetContext(CancellationToken token = default)
        => token == default ? null : new RequestContext { CancellationToken = token };
}
