// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public static class ResourceGraphQueries
{
    public const string DevCenters = $"""
        resources
            | where type =~ 'Microsoft.DevCenter/devcenters'
            | extend devCenterUri = properties.devCenterUri
            | project {DevCenter.Fields}
        """;

    public const string Projects = $"""
        resources
            | where type =~ 'Microsoft.DevCenter/projects'
            | extend description = properties.description
            | extend devCenterUri = properties.devCenterUri
            | extend devCenterId = properties.devCenterId
            | extend devCenterArr = split(devCenterId, '/')
            | extend devCenterName = devCenterArr[array_length(devCenterArr) -1]
            | project {Project.Fields}
        """;

    public static string ProjectsByDevCenter(string devCenterName) => $"""
        resources
            | where type =~ 'Microsoft.DevCenter/projects'
            | extend devCenterId = properties.devCenterId
            | extend devCenterArr = split(devCenterId, '/')
            | extend devCenterName = devCenterArr[array_length(devCenterArr) - 1]
            | where devCenterName =~ '{devCenterName}'
            | extend description = properties.description
            | extend devCenterUri = properties.devCenterUri
            | project {Project.Fields}
        """;

    public static string ProjectByDevCenter(string devCenterName, string projectName) => $"""
        resources
            | where type =~ 'Microsoft.DevCenter/projects'
            | where name =~ '{projectName}'
            | extend devCenterId = properties.devCenterId
            | extend devCenterArr = split(devCenterId, '/')
            | extend devCenterName = devCenterArr[array_length(devCenterArr) - 1]
            | where devCenterName =~ '{devCenterName}'
            | extend description = properties.description
            | extend devCenterUri = properties.devCenterUri
            | project {Project.Fields}

        """;

    // Users will likely only have RBAC roles on the projects
    // not the dev center, so we need to get the dev center uri
    // from the project instead of querying the dev center itself.
    public static string DevCenterByProjects(string devCenterName)
        => $"""
        resources
            | where type =~ 'Microsoft.DevCenter/projects'
            | extend id = properties.devCenterId
            | extend devCenterArr = split(id, '/')
            | extend name = devCenterArr[array_length(devCenterArr) - 1]
            | where name =~ '{devCenterName}'
            | take 1
            | extend type = 'Microsoft.DevCenter/devcenters'
            | extend name = devCenterArr[array_length(devCenterArr) - 1]
            | extend resourceGroup = devCenterArr[array_length(devCenterArr) - 5]
            | extend subscriptionId = devCenterArr[array_length(devCenterArr) - 7]
            | extend devCenterUri = properties.devCenterUri
            | project {DevCenter.Fields}
        """;

    public const string DevCentersByProjects = $"""
        resources
            | where type =~ 'Microsoft.DevCenter/projects'
            | extend devCenterId = tostring(properties.devCenterId)
            | extend devCenterUri = tostring(properties.devCenterUri)
            | distinct devCenterId, devCenterUri, tenantId, tostring(location)
            | extend devCenterArr = split(devCenterId, '/')
            | extend id = devCenterId
            | extend name = devCenterArr[array_length(devCenterArr) - 1]
            | extend type = 'Microsoft.DevCenter/devcenters'
            | extend resourceGroup = devCenterArr[array_length(devCenterArr) - 5]
            | extend subscriptionId = devCenterArr[array_length(devCenterArr) - 7]
            | project {DevCenter.Fields}
        """;
}
