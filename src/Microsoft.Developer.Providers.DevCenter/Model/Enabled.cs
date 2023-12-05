// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Developer.Providers.DevCenter.Model;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Enabled
{
    Unknown,
    Enabled,
    Disabled
}
