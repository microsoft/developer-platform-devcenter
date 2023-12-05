// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core.Serialization;

namespace Microsoft.Developer.Providers.DevCenter;

public static class DevCenterSerialization
{
    public static JsonSerializerOptions Options => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static readonly JsonObjectSerializer AzureSerializer = new(Options);
}

public static class DevCenterSerializationExtensions
{
    public static T? ToObject<T>(this BinaryData data)
        => data.ToObjectFromJson<T>(DevCenterSerialization.Options);

    public static T? ToObject<T>(this BinaryData data, Action<T> configure)
    {
        var obj = data.ToObjectFromJson<T>(DevCenterSerialization.Options);
        configure(obj);
        return obj;
    }

    public static T? ToObject<T>(this BinaryData data, DevCenterInfo devCenter)
        where T : IDevCenterChild
    => data.ToObject<T>(o => o.DevCenter = devCenter);

    public static T? ToObject<T>(this BinaryData data, DevCenterInfo devCenter, string projecName)
        where T : IProjectChild
    => data.ToObject<T>(o =>
    {
        o.DevCenter = devCenter;
        o.ProjectName = projecName;
    });
}
