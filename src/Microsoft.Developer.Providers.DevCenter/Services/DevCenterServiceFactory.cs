// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.ResourceManager;
using Microsoft.Developer.Azure;
using System.Security.Claims;

namespace Microsoft.Developer.Providers.DevCenter;

public class DevCenterServiceFactory(IUserTokenCredentialFactory tokenFactory, ILoggerFactory loggerFactory)
    : IUserScopedServiceFactory<IDevCenterService>
{
    public IDevCenterService Create(ClaimsPrincipal user) =>
        new DevCenterService(tokenFactory, user, new ArmClient(tokenFactory.GetTokenCredential(user)), loggerFactory.CreateLogger<DevCenterService>());
}
