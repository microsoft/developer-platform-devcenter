// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class EnvironmentType : IProjectChild
{
    public string ProjectName { get; set; } = string.Empty;

    public DevCenterInfo? DevCenter { get; set; }

    public required string Name { get; set; }

    public required Enabled Status { get; set; }

    public required string DeploymentTargetId { get; set; }
}
