// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public class ImageReference
{
    public required string Name { get; set; }

    public required string OperatingSystem { get; set; }

    public required string OsBuildNumber { get; set; }

    public required DateTimeOffset PublishedDate { get; set; }

    public required string Version { get; set; }
}
