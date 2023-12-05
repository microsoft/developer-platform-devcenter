// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;
using Environment = Microsoft.Developer.Providers.DevCenter.Model.Environment;

namespace Microsoft.Developer.Providers.DevCenter;

public interface IDevCenterService
{
    (DevCenterClient client, DevCenterInfo devCenter) GetDevCenterClient(DevCenterInfo devCenter);
    (DevBoxesClient client, DevCenterInfo devCenter) GetDevBoxClient(DevCenterInfo devCenter);
    (DeploymentEnvironmentsClient client, DevCenterInfo devCenter) GetDeploymentEnvironmentsClient(DevCenterInfo devCenter);


    Task<Model.DevCenter?> GetDevCenterAsync(string devCenterName, CancellationToken token = default);
    Task<List<Model.DevCenter>> GetDevCentersAsync(CancellationToken token = default);


    Task<List<Project>> GetProjectsAsync(CancellationToken token = default);
    Task<List<Project>> GetProjectsAsync(string devCenterName, CancellationToken token = default);
    // Task<List<Project>> GetProjectsAsync(Model.DevCenter devCenter, CancellationToken token = default);
    Task<Project?> GetProjectAsync(string devCenterName, string projectName, CancellationToken token = default);


    IAsyncEnumerable<ProjectSimple> GetProjectsFromDevCenterAsync(string devCenterName, CancellationToken token = default);
    Task<ProjectSimple?> GetProjectFromDevCenterAsync(string devCenterName, string projectName, CancellationToken token = default);


    IAsyncEnumerable<DevBox> GetDevBoxesAsync(CancellationToken token = default);
    IAsyncEnumerable<DevBox> GetDevBoxesAsync(string devCenterName, CancellationToken token = default);
    IAsyncEnumerable<DevBox> GetDevBoxesAsync(string devCenterName, string projectName, CancellationToken token = default);
    IAsyncEnumerable<DevBox> GetDevBoxesAsync(Project project, CancellationToken token = default);
    Task<DevBox?> GetDevBoxAsync(string devCenterName, string projectName, string devBoxName, CancellationToken token = default);
    Task<DevBox?> GetDevBoxAsync(Project project, string devBoxName, CancellationToken token = default);


    IAsyncEnumerable<Pool> GetPoolsAsync(string devCenterName, CancellationToken token = default);
    IAsyncEnumerable<Pool> GetPoolsAsync(string devCenterName, string projectName, CancellationToken token = default);
    IAsyncEnumerable<Pool> GetPoolsAsync(Project project, CancellationToken token = default);
    Task<Pool?> GetPoolAsync(string devCenterName, string projectName, string poolName, CancellationToken token = default);
    Task<Pool?> GetPoolAsync(Project project, string poolName, CancellationToken token = default);


    IAsyncEnumerable<Environment> GetEnvironmentsAsync(CancellationToken token = default);
    IAsyncEnumerable<Environment> GetEnvironmentsAsync(string devCenterName, CancellationToken token = default);
    IAsyncEnumerable<Environment> GetEnvironmentsAsync(string devCenterName, string projectName, CancellationToken token = default);
    IAsyncEnumerable<Environment> GetEnvironmentsAsync(Project project, CancellationToken token = default);
    Task<Environment?> GetEnvironmentAsync(string devCenterName, string projectName, string environmentName, CancellationToken token = default);
    Task<Environment?> GetEnvironmentAsync(DevCenterInfo devCenterInfo, string projectName, string environmentName, CancellationToken token = default);
    Task<Environment?> GetEnvironmentAsync(Project project, string environmentName, CancellationToken token = default);

    Task<DevCenterInfo> BeginCreateEnvironmentAsync(string devCenterName, string projectName, string environmentName, CreateEnvironmentContent content, CancellationToken token = default);


    IAsyncEnumerable<Catalog> GetCatalogsAsync(string devCenterName, string projectName, CancellationToken token = default);
    IAsyncEnumerable<Catalog> GetCatalogsAsync(Project project, CancellationToken token = default);
    Task<Catalog?> GetCatalogAsync(string devCenterName, string projectName, string catalogName, CancellationToken token = default);
    Task<Catalog?> GetCatalogAsync(Project project, string catalogName, CancellationToken token = default);


    IAsyncEnumerable<EnvironmentDefinition> GetEnvironmentDefinitionsAsync(string devCenterName, CancellationToken token = default);
    IAsyncEnumerable<EnvironmentDefinition> GetEnvironmentDefinitionsAsync(IList<Project> projects, CancellationToken token = default);
    IAsyncEnumerable<EnvironmentDefinition> GetEnvironmentDefinitionsAsync(string devCenterName, string projectName, CancellationToken token = default);
    IAsyncEnumerable<EnvironmentDefinition> GetEnvironmentDefinitionsAsync(Project project, CancellationToken token = default);
    Task<EnvironmentDefinition?> GetEnvironmentDefinitionAsync(string devCenterName, string projectName, string catalogName, string definitionName, CancellationToken token = default);
    Task<EnvironmentDefinition?> GetEnvironmentDefinitionAsync(Project project, string catalogName, string definitionName, CancellationToken token = default);


    IAsyncEnumerable<EnvironmentType> GetEnvironmentTypesAsync(string devCenterName, CancellationToken token = default);
    IAsyncEnumerable<EnvironmentType> GetEnvironmentTypesAsync(IList<Project> projects, CancellationToken token = default);
    IAsyncEnumerable<EnvironmentType> GetEnvironmentTypesAsync(string devCenterName, string projectName, CancellationToken token = default);
    IAsyncEnumerable<EnvironmentType> GetEnvironmentTypesAsync(Project project, CancellationToken token = default);


    Task<List<Entity>> GetTemplateEntitiesAsync(string devCenterName, CancellationToken token = default);
    Task<List<Entity>> GetEnvironmentEntitiesAsync(string devCenterName, CancellationToken token = default);
    // Task<Entity?> GetTemplateEntityAsync(string devCenterName, CancellationToken token = default);

}
