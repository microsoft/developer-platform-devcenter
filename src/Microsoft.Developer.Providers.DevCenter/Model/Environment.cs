// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class Environment : IProjectChild
{
    public string ProjectName { get; set; } = string.Empty;

    public DevCenterInfo? DevCenter { get; set; }

    public required string Name { get; set; }

    public required string CatalogName { get; set; }

    // [JsonPropertyName("environmentDefinitionName")]
    public required string EnvironmentDefinitionName { get; set; }

    public required string EnvironmentType { get; set; }

    // public string? Error { get; set; }

    // public string? Parameters { get; set; }

    public required string ProvisioningState { get; set; }

    public string ResourceGroupId { get; set; } = null!;

    public required string User { get; set; }

    public string ResourceGroup => string.IsNullOrEmpty(ResourceGroupId) ? string.Empty : ResourceGroupId.Split('/').Last();
    public string Subscription => string.IsNullOrEmpty(ResourceGroupId) ? string.Empty : ResourceGroupId.Split('/')[2];
}
