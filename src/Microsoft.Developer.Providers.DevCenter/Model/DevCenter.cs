// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class DevCenter
{
    public const string Fields = "id,name,type,location,tenantId,subscriptionId,resourceGroup,devCenterUri";

    public required string Id { get; set; }

    public required string Name { get; set; }

    public required string Type { get; set; }

    public required string Location { get; set; }

    public required string TenantId { get; set; }

    public required string SubscriptionId { get; set; }

    public required string ResourceGroup { get; set; }

    public required string DevCenterUri { get; set; }

    public Dictionary<string, string> Tags { get; set; } = [];

    public Uri DevCenterEndpoint => new(DevCenterUri);
}

public class DevCenterEqualityComparer : IEqualityComparer<DevCenter>
{
    public bool Equals(DevCenter? x, DevCenter? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.DevCenterEndpoint == y.DevCenterEndpoint;
    }

    public int GetHashCode([DisallowNull] DevCenter obj)
    {
        return obj.DevCenterEndpoint.GetHashCode();
    }
}
