// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Entities;
using Microsoft.Developer.Features;

namespace Microsoft.Developer.Providers.DevCenter.API;

[Authorize]
public class GetEntity
{
    private static readonly EntityKind[] supportedKinds = [
        EntityKind.Environment,
        EntityKind.Template
    ];

    [Function(nameof(GetEntity))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "entities/{kind}/{namespace}/{name}")] HttpRequest req,
        string kind, string @namespace, string name, FunctionContext context, CancellationToken token)
    {
        var log = context.GetLogger<GetEntity>();

        var request = context.Features.Get<IDeveloperPlatformRequestFeature>()
            ?? throw new InvalidOperationException("Unable to get EntityRef from context.Features");

        var entityRef = new EntityRef(request.Kind)
        {
            Name = request.Name,
            Namespace = request.Namespace
        };

        if (!supportedKinds.Contains(entityRef.Kind))
        {
            return new NotFoundResult();
        }

        if (entityRef.Namespace.IsEmpty)
        {
            return new BadRequestObjectResult("Unable to get devcenter from namespace");
        }

        // namespace is always the devcenter name
        if (entityRef.Namespace.Equals(Entity.Defaults.Namespace))
        {
            return new BadRequestObjectResult($"Invalid namespace '{entityRef.Namespace}'.");
        }

        var devCenter = context.Features.GetRequiredFeature<IDeveloperPlatformDevCenterFeature>().DevCenterService;

        if (devCenter is null)
        {
            log.LogWarning("Unable to get DevCenter service for user.");
            return new NotFoundResult();
        }

        if (entityRef.Kind == EntityKind.Environment)
        {
            // TODO
            var environments = await devCenter.GetEnvironmentEntitiesAsync(entityRef.Namespace, token);
            var entity = environments.FirstOrDefault(t => entityRef.Equals(t.GetEntityRef()));

            return entity is null ? new NotFoundResult() : new EntityResult(entity);
        }

        if (entityRef.Kind == EntityKind.Template)
        {
            var templates = await devCenter.GetTemplateEntitiesAsync(entityRef.Namespace, token);
            var template = templates.FirstOrDefault(t => entityRef.Equals(t.GetEntityRef()));

            return template is null ? new NotFoundResult() : new EntityResult(template);
        }

        return new NotFoundResult();
    }
}
