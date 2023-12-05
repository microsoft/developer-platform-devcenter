// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter;

public class CreateEnvironmentContent
{
    public string CatalogName { get; set; } = null!;

    public string EnvironmentDefinitionName { get; set; } = null!;

    public string EnvironmentType { get; set; } = null!;

    public object? Parameters { get; set; }
}
