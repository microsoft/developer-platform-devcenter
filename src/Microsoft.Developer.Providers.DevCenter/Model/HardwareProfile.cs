// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class HardwareProfile
{
    public required int MemoryGb { get; set; }

    public required string SkuName { get; set; }

    [JsonPropertyName("vCpUs")]
    public required int VCPUs { get; set; }
}
