// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Developer.Providers.DevCenter.Model;

public interface IProjectChild : IDevCenterChild
{
    public string ProjectName { get; set; }
}
