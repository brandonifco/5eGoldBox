using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace FiveEGoldBox.Application.Content.Schema;

/// Derives a JSON Schema (2020-12) for one of the V1 content DTO roots by
/// reflecting over its record graph, rather than hand-maintaining one.
///
/// A hand-written schema drifts the moment a V1 DTO gains, loses, or renames
/// a property; reflecting it keeps the schema and the type it describes from
/// ever disagreeing. Deliberately narrow: it only understands the shapes
/// these DTOs actually use (records of primitives, enums, nested records,
/// IReadOnlyList<T>, IReadOnlyDictionary<TKey,TValue>) -- not JSON Schema or
/// C# in general. A C#-`required` property becomes a schema "required"
/// entry; everything else is treated as omittable, which is the actual
/// contract these DTOs already have via their own default-valued
/// initializers. Whether a present property's value may itself be JSON
/// `null` is deliberately not modeled -- authored content never writes an
/// explicit null today, and the schema's job is catching a missing or
/// misspelled field, not exhaustively encoding nullability.
internal static class JsonSchemaGenerator
{
    private const string SchemaDialect =
        "https://json-schema.org/draft/2020-12/schema";

    internal static JsonObject Generate(
        Type rootType,
        string title)
    {
        ArgumentNullException.ThrowIfNull(rootType);
        ArgumentNullException.ThrowIfNull(title);

        JsonObject defs = [];
        HashSet<Type> defining = [];

        JsonObject rootSchema = BuildObjectSchema(
            rootType,
            defs,
            defining);

        JsonObject root = new()
        {
            ["$schema"] = SchemaDialect,
            ["title"] = title
        };

        foreach ((string key, JsonNode? value) in rootSchema)
        {
            root[key] = value?.DeepClone();
        }

        // Editors (VS Code, JetBrains IDEs) associate an instance document
        // with a schema via a top-level "$schema" property on the instance
        // itself -- a convention layered on top of JSON Schema, not part of
        // it. The root object schema's own additionalProperties: false would
        // otherwise reject exactly the property this schema is meant to be
        // referenced by.
        ((JsonObject)root["properties"]!)["$schema"] =
            new JsonObject { ["type"] = "string" };

        if (defs.Count > 0)
        {
            root["$defs"] = defs;
        }

        return root;
    }

    private static JsonNode BuildSchema(
        Type type,
        JsonObject defs,
        HashSet<Type> defining)
    {
        Type? underlyingNullable = Nullable.GetUnderlyingType(type);
        Type effectiveType = underlyingNullable ?? type;

        if (effectiveType == typeof(string))
        {
            return new JsonObject { ["type"] = "string" };
        }

        if (effectiveType == typeof(int))
        {
            return new JsonObject { ["type"] = "integer" };
        }

        if (effectiveType == typeof(decimal)
            || effectiveType == typeof(double)
            || effectiveType == typeof(float))
        {
            return new JsonObject { ["type"] = "number" };
        }

        if (effectiveType == typeof(bool))
        {
            return new JsonObject { ["type"] = "boolean" };
        }

        if (effectiveType.IsEnum)
        {
            JsonArray names = [];

            foreach (string name in Enum.GetNames(effectiveType))
            {
                names.Add(name);
            }

            return new JsonObject
            {
                ["type"] = "string",
                ["enum"] = names
            };
        }

        Type? dictionaryValueType = TryGetDictionaryValueType(effectiveType);

        if (dictionaryValueType is not null)
        {
            return new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = BuildSchema(
                    dictionaryValueType,
                    defs,
                    defining)
            };
        }

        Type? elementType = TryGetListElementType(effectiveType);

        if (elementType is not null)
        {
            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = BuildSchema(elementType, defs, defining)
            };
        }

        return BuildDefinitionRef(effectiveType, defs, defining);
    }

    private static JsonNode BuildDefinitionRef(
        Type type,
        JsonObject defs,
        HashSet<Type> defining)
    {
        string defName = type.Name;

        if (!defs.ContainsKey(defName))
        {
            if (!defining.Add(type))
            {
                throw new InvalidOperationException(
                    $"Content DTO graph is cyclic at '{type.Name}'; the schema generator only supports acyclic DTO graphs.");
            }

            // Reserve the slot before recursing so a type that (indirectly)
            // references itself is caught by the guard above rather than
            // recursing forever.
            defs[defName] = new JsonObject();
            defs[defName] = BuildObjectSchema(type, defs, defining);

            defining.Remove(type);
        }

        return new JsonObject
        {
            ["$ref"] = $"#/$defs/{defName}"
        };
    }

    private static JsonObject BuildObjectSchema(
        Type type,
        JsonObject defs,
        HashSet<Type> defining)
    {
        JsonObject properties = [];
        JsonArray required = [];

        foreach (PropertyInfo property in type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            properties[property.Name] = BuildSchema(
                property.PropertyType,
                defs,
                defining);

            if (IsRequiredMember(property))
            {
                required.Add(property.Name);
            }
        }

        JsonObject schema = new()
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["additionalProperties"] = false
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
    }

    private static bool IsRequiredMember(
        PropertyInfo property)
    {
        return property.GetCustomAttribute<RequiredMemberAttribute>() is not null;
    }

    private static Type? TryGetListElementType(
        Type type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        Type definition = type.GetGenericTypeDefinition();

        return definition == typeof(IReadOnlyList<>)
            ? type.GetGenericArguments()[0]
            : null;
    }

    private static Type? TryGetDictionaryValueType(
        Type type)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        Type definition = type.GetGenericTypeDefinition();

        return definition == typeof(IReadOnlyDictionary<,>)
            ? type.GetGenericArguments()[1]
            : null;
    }
}
