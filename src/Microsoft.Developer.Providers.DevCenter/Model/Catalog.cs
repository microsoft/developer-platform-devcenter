// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class Catalog : IProjectChild
{
    public string ProjectName { get; set; } = string.Empty;

    public DevCenterInfo? DevCenter { get; set; }

    public required string Name { get; set; }
}
