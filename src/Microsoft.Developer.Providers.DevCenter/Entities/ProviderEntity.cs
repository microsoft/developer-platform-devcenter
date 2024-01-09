// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Developer.Entities;

namespace Microsoft.Developer.Providers.DevCenter;

public static class ProviderEntity
{
    public static Entity Create()
    {
        var entity = new Entity(EntityKind.Provider)
        {
            Metadata = new Metadata
            {
                Name = "devcenter.azure.com",
                Title = "Dev Center",
                Description = "The Dev Center provider..."
            },
            Spec = new Spec
            {
            }
        };

        return entity;
    }
}