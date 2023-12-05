// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class DevBox : IProjectChild
{
    public DevCenterInfo? DevCenter { get; set; }

    public required string Name { get; set; }

    public required string Location { get; set; }

    public required OsType OsType { get; set; }

    public required string PoolName { get; set; }

    public required string ProjectName { get; set; }

    public required string UniqueId { get; set; }

    public required string User { get; set; }

    public required string ActionState { get; set; }

    public required string PowerState { get; set; }

    public required string ProvisioningState { get; set; }

    public required DateTimeOffset CreatedTime { get; set; }

    public required Enabled HibernateSupport { get; set; }

    public required Enabled LocalAdministrator { get; set; }

    public required HardwareProfile HardwareProfile { get; set; }

    public required ImageReference ImageReference { get; set; }

    public required StorageProfile StorageProfile { get; set; }

    public string? Error { get; set; }
}
