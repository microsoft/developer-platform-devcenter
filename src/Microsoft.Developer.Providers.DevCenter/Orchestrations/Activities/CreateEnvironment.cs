// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;
using Microsoft.DurableTask;

namespace Microsoft.Developer.Providers.DevCenter;

[DurableTask(nameof(CreateEnvironment))]
public class CreateEnvironment(IUserScopedServiceFactory<IDevCenterService> factory, ILogger<CreateEnvironment> log)
    : TaskActivity<CreateEnvironment.Input, CreateEnvironment.Output>
{
    public record Input(OrchestrationUser User, string DevCenter, string Project, string Name, CreateEnvironmentContent Payload);

    public record Output(string Name, DevCenterInfo DevCenter);

    public override async Task<Output> RunAsync(TaskActivityContext context, Input input)
    {
        var token = CancellationToken.None;

        var devCenter = factory.Create(input.User.User);

        log.LogInformation("Creating environment '{name}'", input.Name);

        var devCenterInfo = await devCenter.BeginCreateEnvironmentAsync(input.DevCenter, input.Project, input.Name, input.Payload, token);

        return new(input.Name, devCenterInfo);
    }
}
