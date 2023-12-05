// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Entities;
using Microsoft.Developer.Features;
using Microsoft.Developer.Requests;
using Microsoft.DurableTask.Client;

namespace Microsoft.Developer.Providers.DevCenter.API;

[Authorize]
public class CreateEntities
{
    [Function(nameof(CreateEntitiesFromTemplate))]
    public async Task<IActionResult> CreateEntitiesFromTemplate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "entities")] HttpRequest req,
        [DurableClient] DurableTaskClient taskClient, FunctionContext context, CancellationToken token)
    {
        var payload = await req.GetTemplateRequestAsync(token);
        var templateRef = payload.TemplateRef;

        if (templateRef.Namespace.IsEmpty)
        {
            return new BadRequestObjectResult("Unable to get GitHub organization from templateRef namespace");
        }

        // namespace is always the github organization
        if (templateRef.Namespace.Equals(Entity.Defaults.Namespace))
        {
            return new BadRequestObjectResult($"Invalid namespace '{templateRef.Namespace}'.");
        }

        var inputs = payload.GetInputs();

        var project = inputs.GetRequiredInput("project");
        var environmentName = inputs.GetRequiredInput("environmentName");
        var environmentType = inputs.GetRequiredInput("environmentType");

        var parametersNode = inputs["parameters"];
        var parameters = parametersNode is null ? new object() : JsonSerializer.Deserialize(parametersNode, typeof(object)) ?? new object();

        if (context.Features.GetRequiredFeature<IDeveloperPlatformDevCenterFeature>().DevCenterService is not { } devCenter)
        {
            return new BadRequestObjectResult("Unable to get DevCenter service for user.");
        }

        string? catalogName = null;
        string? environmentDefinitionName = null;

        await foreach (var catalog in devCenter.GetCatalogsAsync(templateRef.Namespace, project, token))
        {
            var prefix = $"{catalog.Name.ToLowerInvariant()}-";
            if (templateRef.Name.StartsWith(prefix))
            {
                catalogName = catalog.Name;
                environmentDefinitionName = templateRef.Name.Replace(prefix, string.Empty);
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(catalogName) || string.IsNullOrWhiteSpace(environmentDefinitionName))
        {
            return new NotFoundObjectResult($"Template not found.");
        }

        var content = new CreateEnvironmentContent
        {
            CatalogName = catalogName,
            EnvironmentDefinitionName = environmentDefinitionName,
            EnvironmentType = environmentType,
            Parameters = parameters
        };

        var input = new EnvironmentOrchestrator.Input(OrchestrationUser.Create(context), templateRef.Namespace, project, environmentName, content);

        var instanceId = await taskClient.ScheduleNewOrchestrationInstanceAsync(nameof(EnvironmentOrchestrator), input, token);

        if (instanceId is not null)
        {
            return new AcceptedResult(req.HttpContext.GetStatusUri(instanceId), new TemplateResponse());
        }

        return new BadRequestObjectResult($"Unhandled Template");
    }
}
