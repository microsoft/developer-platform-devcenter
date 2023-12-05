// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Azure;
using System.Security.Claims;

namespace Microsoft.Developer.Providers.DevCenter;

public class DevCenterServiceFactory(IUserArmService armService, ILoggerFactory loggerFactory)
    : IUserScopedServiceFactory<IDevCenterService>
{
    public IDevCenterService Create(ClaimsPrincipal user) =>
        new DevCenterService(armService, user, loggerFactory.CreateLogger<DevCenterService>());
}
