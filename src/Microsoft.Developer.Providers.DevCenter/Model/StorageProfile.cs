// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class StorageProfile
{
    public required StorageProfileOsDisk OsDisk { get; set; }
}

public class StorageProfileOsDisk
{
    public required int DiskSizeGb { get; set; }
}
