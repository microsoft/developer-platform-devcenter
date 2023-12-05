// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class EnvironmentDefinition : IProjectChild
{
    public string ProjectName { get; set; } = string.Empty;

    public DevCenterInfo? DevCenter { get; set; }

    public required string Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required string CatalogName { get; set; }

    public required List<EnvironmentDefinitionParameter> Parameters { get; set; } = [];

    public required string ParametersSchema { get; set; }

    public required string TemplatePath { get; set; }
}

public class EnvironmentDefinitionParameter
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public required string Type { get; set; }

    public string? Description { get; set; }

    public string? Default { get; set; }

    public List<string>? Allowed { get; set; }

    public required bool ReadOnly { get; set; }

    public required bool Required { get; set; }
}
