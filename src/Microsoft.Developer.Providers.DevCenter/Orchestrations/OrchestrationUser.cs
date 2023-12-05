// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Developer.Providers.DevCenter;

public class OrchestrationUser
{
    private ClaimsPrincipal? user = null;

    [JsonIgnore]
    public ClaimsPrincipal User => user ??= CreateUser(UserCache, UserJWT);

    public required string UserCache { get; set; }

    public required string UserJWT { get; set; }

    private static ClaimsPrincipal CreateUser(string base64, string jwt)
    {
        using var ms = new MemoryStream(Convert.FromBase64String(base64));
        using var binary = new BinaryReader(ms);

        var user = new ClaimsPrincipal(binary);

        if (user is { Identity: ClaimsIdentity identity })
        {
            identity.BootstrapContext = new JsonWebToken(jwt);
        }

        return user;
    }

    public static OrchestrationUser Create(ClaimsPrincipal principal)
    {
        if (principal is { Identity: ClaimsIdentity identity })
        {
            if (identity.BootstrapContext is SecurityToken token)
            {
                var ms = new MemoryStream();
                var binary = new BinaryWriter(ms);

                principal.WriteTo(binary);

                return new OrchestrationUser
                {
                    UserCache = Convert.ToBase64String(ms.ToArray()),
                    UserJWT = token.UnsafeToString()
                };
            }
            else
            {
                throw new Exception("Invalid principal. Identity bootstrap context missing");
            }
        }
        else
        {
            throw new Exception("Invalid principal");
        }
    }

    public static OrchestrationUser Create(FunctionContext context) => Create(context.GetRequiredHttpContext().User);
}