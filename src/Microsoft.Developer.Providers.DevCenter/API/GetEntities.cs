﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Entities;
using Microsoft.Developer.Features;

namespace Microsoft.Developer.Providers.DevCenter.API;

[Authorize]
public class GetEntities
{
    private static readonly EntityKind[] supportedKinds = [
        EntityKind.Environment,
        EntityKind.Template
    ];

    [Function(nameof(GetEntities))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.User, "get", Route = "entities")] HttpRequest req,
        FunctionContext context, CancellationToken token)
    {
        var log = context.GetLogger<GetEntities>();

        var devCenter = context.Features.GetRequiredFeature<IDeveloperPlatformDevCenterFeature>().DevCenterService;

        if (devCenter is null)
        {
            log.LogWarning("Unable to get DevCenter service for user.");
            return new NotFoundResult();
        }

        // get and cache the projects
        _ = await devCenter.GetProjectsAsync(TemporaryConstants.DevCenter, token);

        var entities = await Task.WhenAll(
            devCenter.GetEnvironmentEntitiesAsync(TemporaryConstants.DevCenter, token),
            devCenter.GetTemplateEntitiesAsync(TemporaryConstants.DevCenter, token));

        return new EntitiesResult(entities);
    }

    [Function(nameof(GetEntitiesByKind))]
    public async Task<IActionResult> GetEntitiesByKind(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities/{kind}")] HttpRequest req,
        string kind, FunctionContext context, CancellationToken token)
    {
        var log = context.GetLogger<GetEntities>();

        var entityRef = context.Features.Get<IDeveloperPlatformRequestFeature>()
            ?? throw new InvalidOperationException("Unable to get EntityRef from context.Features");

        if (!supportedKinds.Contains(entityRef.Kind))
        {
            return EntitiesResult.Empty;
        }

        var devCenter = context.Features.GetRequiredFeature<IDeveloperPlatformDevCenterFeature>().DevCenterService;

        if (devCenter is null)
        {
            log.LogWarning("Unable to get DevCenter service for user.");
            return new NotFoundResult();
        }

        if (entityRef.Kind == EntityKind.Environment)
        {
            return new EntitiesResult(await devCenter.GetEnvironmentEntitiesAsync(TemporaryConstants.DevCenter, token));
        }

        if (entityRef.Kind == EntityKind.Template)
        {
            return new EntitiesResult(await devCenter.GetTemplateEntitiesAsync(TemporaryConstants.DevCenter, token));
        }

        return EntitiesResult.Empty;
    }
}
