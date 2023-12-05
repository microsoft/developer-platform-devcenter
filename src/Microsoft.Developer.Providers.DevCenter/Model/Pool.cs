// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class Pool : IProjectChild
{
    public DevCenterInfo? DevCenter { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public required string Name { get; set; }

    public required string Location { get; set; }

    public required OsType OsType { get; set; }

    public required string HealthStatus { get; set; }

    public required Enabled HibernateSupport { get; set; }

    public required Enabled LocalAdministrator { get; set; }

    public string? StopOnDisconnect { get; set; }

    public required HardwareProfile HardwareProfile { get; set; }

    public required ImageReference ImageReference { get; set; }

    public required StorageProfile StorageProfile { get; set; }
}
