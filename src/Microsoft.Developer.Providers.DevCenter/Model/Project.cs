// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class ProjectSimple : IDevCenterChild
{
    public DevCenterInfo? DevCenter { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }
}

public class Project //: IDevCenterChild
{
    // public DevCenterInfo? DevCenter { get; set; }

    public const string Fields = "id,name,description,type,location,tenantId,subscriptionId,resourceGroup,devCenterName,devCenterId,devCenterUri,tags";

    public required string Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required string Type { get; set; }

    public required string Location { get; set; }

    public required string TenantId { get; set; }

    public required string SubscriptionId { get; set; }

    public required string ResourceGroup { get; set; }

    public required string DevCenterName { get; set; }

    public required string DevCenterId { get; set; }

    public required string DevCenterUri { get; set; }

    public Dictionary<string, string> Tags { get; set; } = [];

    public Uri DevCenterEndpoint => new(DevCenterUri);
}
