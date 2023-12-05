// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Providers.DevCenter;

namespace Microsoft.Developer.Features;

public interface IDeveloperPlatformDevCenterFeature
{
    IDevCenterService? DevCenterService { get; }
}
