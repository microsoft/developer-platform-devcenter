// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter;

public class DevCenterInfo
{
    public required string Name { get; set; }
    public required string Tenant { get; set; }
    public required Uri Endpoint { get; set; }
    public required string Id { get; set; }
}

public static class DevCenterInfoExtensions
{
    public static DevCenterInfo Info(this Model.DevCenter devCenter) => new()
    {
        Tenant = devCenter.TenantId,
        Id = devCenter.Id,
        Name = devCenter.Name,
        Endpoint = devCenter.DevCenterEndpoint
    };

    public static DevCenterInfo DevCenterInfo(this Project project) => new()
    {
        Tenant = project.TenantId,
        Id = project.DevCenterId,
        Name = project.DevCenterName,
        Endpoint = project.DevCenterEndpoint
    };
}
