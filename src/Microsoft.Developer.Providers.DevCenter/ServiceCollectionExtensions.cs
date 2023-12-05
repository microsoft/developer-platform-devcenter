// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Developer.Providers.DevCenter;

public static class ServiceCollectionExtensions
{
    public static IDeveloperPlatformBuilder AddDevCenterProvider(this IDeveloperPlatformBuilder builder, Action<IDevCenterProviderBuilder> configure)
    {
        builder.Services
            .AddScoped<IUserScopedServiceFactory<IDevCenterService>, DevCenterServiceFactory>();

        builder.AddProvider(b => new DcBuilder(b, b.Services), configure);

        return builder;
    }

    private sealed record DcBuilder(IDeveloperPlatformBuilder Builder, IServiceCollection Services) : IDevCenterProviderBuilder;


    public static IDevCenterProviderBuilder AddCosmos(this IDevCenterProviderBuilder builder)
    {
        return builder;
    }

    public static IDevCenterProviderBuilder AddOptions(this IDevCenterProviderBuilder builder, IConfiguration configuration)
    {
        // builder.Services
        //     .Configure<GitHubOptions>(configuration.GetSection(GitHubOptions.Section));

        return builder;
    }
}
