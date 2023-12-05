// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;
using Microsoft.DurableTask;

namespace Microsoft.Developer.Providers.DevCenter;

[DurableTask(nameof(CheckEnvironment))]
public class CheckEnvironment(IUserScopedServiceFactory<IDevCenterService> factory, ILogger<CheckEnvironment> log)
    : TaskActivity<CheckEnvironment.Input, CheckEnvironment.Output>
{
    public record Input(OrchestrationUser User, DevCenterInfo DevCenter, string Project, string Name);

    public record Output(EntityRef EntityRef, bool Completed, string Status);

    public override async Task<Output> RunAsync(TaskActivityContext context, Input input)
    {
        var token = CancellationToken.None;

        var devCenter = factory.Create(input.User.User);

        log.LogInformation("Checking environment '{name}'", input.Name);

        var environment = await devCenter.GetEnvironmentAsync(input.DevCenter, input.Project, input.Name, token)
            ?? throw new Exception($"Environment {input.Name} does not exist");

        var completed = environment.ProvisioningState is "Succeeded" or "Failed";

        return new(environment.EntityRef(), completed, environment.ProvisioningState);
    }
}
