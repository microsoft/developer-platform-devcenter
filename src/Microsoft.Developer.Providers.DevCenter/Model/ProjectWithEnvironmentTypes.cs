// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class ProjectWithEnvironmentTypes
{
    public Project Project { get; set; } = default!;

    public List<EnvironmentType> EnvironmentTypes { get; set; } = [];
}
