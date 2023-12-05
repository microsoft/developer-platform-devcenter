// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Developer.Entities;
using Microsoft.DurableTask;

namespace Microsoft.Developer.Providers.DevCenter;

[DurableTask(nameof(EnvironmentOrchestrator))]
public class EnvironmentOrchestrator : TaskOrchestrator<EnvironmentOrchestrator.Input, EnvironmentOrchestrator.Output>
{
    public record Input(OrchestrationUser User, string DevCenter, string Project, string Name, CreateEnvironmentContent Payload);

    public record Output(EntityRef? EntityRef);

    public override async Task<Output> RunAsync([OrchestrationTrigger] TaskOrchestrationContext context, Input input)
    {
        const double expireAfterMins = 60;
        const double checkpointIntervalSeconds = 15;

        var log = context.CreateReplaySafeLogger(nameof(EnvironmentOrchestrator));

        log.LogInformation("Starting environment orchestration {environment}", input.Name);

        var create = await context.CallCreateEnvironmentAsync(new CreateEnvironment.Input(input.User, input.DevCenter, input.Project, input.Name, input.Payload));

        log.LogInformation("Started creating environment '{name}'", create.Name);

        var expire = context.CurrentUtcDateTime.AddMinutes(expireAfterMins);

        while (context.CurrentUtcDateTime < expire)
        {
            var status = await context.CallCheckEnvironmentAsync(new CheckEnvironment.Input(input.User, create.DevCenter, input.Project, input.Name));

            log.LogInformation("Environment '{environment}' is '{status}'", create.Name, status.Status);

            if (status.Completed)
            {
                log.LogInformation("Environment '{environment}' completed with '{status}'", create.Name, status.Status);

                return new(status.EntityRef);
            }

            // Wait for the next checkpoint
            var nextCheckpoint = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(checkpointIntervalSeconds));
            log.LogInformation("Environment '{environment}' not finished waiting {wait} seconds.", create.Name, checkpointIntervalSeconds);
            log.LogInformation("Next check for environment '{environment}' at {checkpoint}.", create.Name, nextCheckpoint);

            await context.CreateTimer(nextCheckpoint, CancellationToken.None);
        }

        log.LogError("Monitor for environment '{environment}' expiring after {total} mins.", create.Name, expireAfterMins);

        return new(null);
    }
}