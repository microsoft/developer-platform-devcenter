// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Developer.Features;
using Microsoft.Developer.Providers.DevCenter;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Developer.Hosting.Middleware;

public class DeveloperPlatformDevCenterMiddleware : IFunctionsWorkerMiddleware
{
    public Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        if (context.Features.Get<IDeveloperPlatformDevCenterFeature>() is null)
        {
            var httpContext = context.GetRequiredHttpContext();

            var devcenter = context.InstanceServices
                .GetRequiredService<IUserScopedServiceFactory<IDevCenterService>>()
                .Create(httpContext.User);

            var feature = new DevPlatformFeature
            {
                DevCenterService = devcenter
            };

            context.Features.Set<IDeveloperPlatformDevCenterFeature>(feature);
        }

        return next(context);
    }

    private sealed record DevPlatformFeature : IDeveloperPlatformDevCenterFeature
    {
        public IDevCenterService? DevCenterService { get; set; }
    }
}
