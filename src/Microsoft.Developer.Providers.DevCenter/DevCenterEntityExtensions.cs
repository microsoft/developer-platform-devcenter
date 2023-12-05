// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Developer.Entities;
using Microsoft.Developer.Providers.JsonSchema;

namespace Microsoft.Developer.Providers.DevCenter;

public static class DevCenterEntityExtensions
{
    public static Entity ToEntity(this EnvironmentDefinition definition, IList<ProjectWithEnvironmentTypes> projectsWithTypes)
    {
        var entityRef = definition.CreateEntityRef();

        var entity = new Entity(EntityKind.Template)
        {
            Metadata = new Metadata
            {
                Name = entityRef.Name,
                Namespace = entityRef.Namespace,
                Title = definition.Name,
                Description = definition.Description,
                Labels = {
                    {Labels.DevCenter, definition.DevCenter!.Name},
                    {Labels.Catalog, definition.CatalogName},
                    {Labels.EnvironmentDefinition, definition.Name}
                }
            },
            Spec = new TemplateSpec
            {
                InputJsonSchema = CreateEnvironmentDefinitionJsonSchema(definition, projectsWithTypes),
                Creates = [
                    new EntityPlan
                    {
                        Kind = EntityKind.Environment,
                        Namespace = entityRef.Namespace,
                    }
                ]
            }
        };

        return entity;
    }

    public static Entity ToEntity(this Model.Environment environment)
    {
        var entityRef = environment.EntityRef();

        var entity = new Entity(entityRef.Kind)
        {
            Metadata = new Metadata
            {
                Name = entityRef.Name,
                Namespace = entityRef.Namespace,
                Title = environment.Name,
                // Description = ,
                Labels = {
                    {Labels.DevCenter, environment.DevCenter!.Name},
                    {Labels.Project, environment.ProjectName},
                    {Labels.EnvironmentType, environment.EnvironmentType},
                    {Labels.Catalog, environment.CatalogName},
                    {Labels.EnvironmentDefinition, environment.EnvironmentDefinitionName},
                    {Labels.ResourceGroupId, environment.ResourceGroupId},
                    {Labels.ResourceGroup, environment.ResourceGroup},
                    {Labels.Subscription, environment.Subscription},
                    {Labels.User, environment.User}
                }
            },
            Spec = new Spec
            {
            }
        };

        return entity;
    }

    public static void WriteEnvironmentDefinition(this Utf8JsonWriter writer, EnvironmentDefinition definition, IList<ProjectWithEnvironmentTypes> projectsWithTypes)
    {
        writer.WriteStartObject();

        // writer.WriteTitle(definition.Name);
        writer.WriteType(JsonSchemaTypes.Object);

        writer.WriteStartPropertiesObject();

        // start: project
        writer.WriteStartObject("project");
        writer.WriteTitle("Project");
        writer.WriteDescription("Select a project");
        writer.WriteType(JsonSchemaTypes.String);
        writer.WriteEnumArray(projectsWithTypes.Select(p => p.Project.Name));
        // writer.WriteEnumArray(projectsWithTypes.Select(p => (p.Project.Name, $"{p.Project.Name} ({p.Project.DevCenterName})")));
        writer.WriteEndObject();
        // end: project

        // start environmentName
        writer.WriteStartObject("environmentName");
        writer.WriteTitle("Environment name");
        writer.WriteDescription("Enter the name of your deployment environment");
        writer.WriteType(JsonSchemaTypes.String);
        writer.WriteEndObject();
        // end environmentName

        if (definition.Parameters.Count != 0)
        {
            writer.WriteEnvironmentDefinitionParameters(definition.Parameters);
        }

        writer.WriteEndObject(); // end properties

        writer.WriteAllProjectsAndEnvironmentTypes(projectsWithTypes);

        writer.WriteEndObject(); // end root
    }

    public static void WriteEnvironmentDefinitionParameters(this Utf8JsonWriter writer, IList<EnvironmentDefinitionParameter> parameters)
    {
        writer.WriteStartParametersObject();
        writer.WriteTitle("Inputs");
        writer.WriteType(JsonSchemaTypes.Object);

        var required = parameters
            .Where(p => p.Required)
            .Select(p => p.Id);

        if (required is not null)
        {
            writer.WriteRequiredArray(required);
        }

        writer.WriteStartPropertiesObject();

        foreach (var parameter in parameters)
        {
            writer.WriteEnvironmentDefinitionParameter(parameter);
        }

        writer.WriteEndObject(); // properties

        writer.WriteEndObject(); // parameters
    }

    private static void WriteEnvironmentDefinitionParameter(this Utf8JsonWriter writer, EnvironmentDefinitionParameter parameter)
        => writer.WriteInputParameter(id: parameter.Id, type: parameter.Type, name: parameter.Name, description: parameter.Description, @default: parameter.Default, options: parameter.Allowed);

    public static void WriteAllProjectsAndEnvironmentTypes(this Utf8JsonWriter writer, IList<ProjectWithEnvironmentTypes> pets)
    {
        writer.WriteStartAllOfArray();

        foreach (var pet in pets)
        {
            writer.WriteProjectAndEnvironmentTypes(pet);
        }

        writer.WriteStartObject();
        writer.WriteRequiredArray("environmentName", "project");
        writer.WriteEndObject();

        writer.WriteEndArray();
    }

    public static void WriteProjectAndEnvironmentTypes(this Utf8JsonWriter writer, ProjectWithEnvironmentTypes pet)
    {
        writer.WriteStartObject();

        // if
        writer.WriteStartIfObject();

        writer.WriteStartPropertiesObject();
        writer.WriteStartObject("project");
        writer.WriteConst(pet.Project.Name);
        writer.WriteEndObject(); // end project
        writer.WriteEndObject(); // end properties

        writer.WriteEndObject(); // end if

        // then
        writer.WriteStartThenObject();

        writer.WriteStartPropertiesObject();
        writer.WriteStartObject("environmentType");
        writer.WriteTitle("Environment type");
        writer.WriteDescription("Select an environment type");
        writer.WriteType(JsonSchemaTypes.String);
        writer.WriteEnumArray(pet.EnvironmentTypes.Select(p => p.Name));
        writer.WriteEndObject(); // end environmentType
        writer.WriteEndObject(); // end properties

        writer.WriteRequiredArray("environmentType");

        writer.WriteEndObject(); // end then

        writer.WriteEndObject(); // end root
    }

    private static string CreateEnvironmentDefinitionJsonSchema(EnvironmentDefinition definition, IList<ProjectWithEnvironmentTypes> projectWithTypes)
    {
        JsonWriterOptions writerOptions = new() { Indented = false, };

        using MemoryStream stream = new();
        using Utf8JsonWriter writer = new(stream, writerOptions);

        writer.WriteEnvironmentDefinition(definition, projectWithTypes);

        writer.Flush();

        var json = Encoding.UTF8.GetString(stream.ToArray());

        return json;
    }


    public static TemplateRef CreateEntityRef(this EnvironmentDefinition definition)
    {
        var devCenterName = definition.DevCenter?.Name ?? throw new ArgumentException("Could not resolve devcenter name from environmentDefinition");
        var catalogName = definition.CatalogName ?? throw new ArgumentException("Could not resolve catalog name from environmentDefinition");
        var definitionName = definition.Name ?? throw new ArgumentException("Could not resolve catalog name from environmentDefinition");

        // TODO: this doesn't account for special chars
        return TemplateRef.Create($"{catalogName}-{definitionName}", devCenterName);
    }

    public static EntityRef EntityRef(this Model.Environment environment)
    {
        var devCenterName = environment.DevCenter?.Name ?? throw new ArgumentException("Could not resolve devcenter name from devcenter environment");
        // var projectName = environment.CatalogName ?? throw new ArgumentException("Could not resolve projecxt name from devcenter environment");
        var name = environment.Name ?? throw new ArgumentException("Could not resolve name from devcenter environment");

        // TODO: this doesn't account for special chars
        return new EntityRef(EntityKind.Environment) { Namespace = devCenterName, Name = name };
    }
}

internal static class Labels
{
    public const string ProviderId = "devcenter.azure.com";

    public static ProviderKey GetLabelKey(string key) => new(ProviderId, key.ToLowerInvariant());
    public static readonly ProviderKey DevCenter = GetLabelKey("devcenter");
    public static readonly ProviderKey Project = GetLabelKey("project");
    public static readonly ProviderKey Catalog = GetLabelKey("catalog");
    public static readonly ProviderKey EnvironmentDefinition = GetLabelKey("definition");
    public static readonly ProviderKey EnvironmentType = GetLabelKey("type");
    public static readonly ProviderKey ResourceGroupId = GetLabelKey("resource-group-id");
    public static readonly ProviderKey ResourceGroup = GetLabelKey("resource-group");
    public static readonly ProviderKey Subscription = GetLabelKey("subscription");
    public static readonly ProviderKey User = GetLabelKey("user");
}
