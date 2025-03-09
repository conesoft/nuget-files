using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Conesoft.Files;

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class WithPolymorphicTypesExtensions
{
    public static JsonSerializerOptions WithPolymorphicTypesFor<T>(this JsonSerializerOptions jsonSerializerOptions, Action<TypeCollector<T>>? configure = null, string? propertyName = null) => new(jsonSerializerOptions)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(jsonTypeInfo =>
        {
            if (jsonTypeInfo.Type == typeof(T))
            {
                jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = propertyName ?? "type",
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
                };

                foreach (var t in TypeCollector<T>.Collect(configure).Select(t => new JsonDerivedType(t, t.Name.ToLowerInvariant())))
                {
                    jsonTypeInfo.PolymorphismOptions.DerivedTypes.Add(t);
                }
            }
        })
    };

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TypeCollector<Base>(List<Type> types)
    {
        public TypeCollector<Base> IncludeType<T>() where T : Base
        {
            types.Add(typeof(T));
            return this;
        }

        public TypeCollector<Base> IncludeTypesFromNamespaceWith<T>()
        {
            types.AddRange(AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(Base)) && t.Namespace == typeof(T).Namespace)
            );
            return this;
        }

        public static IEnumerable<Type> Collect<T>(Action<TypeCollector<T>>? configure)
        {
            List<Type> types = [];
            configure?.Invoke(new TypeCollector<T>(types));
            if (configure == null)
            {
                types.AddRange(AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsSubclassOf(typeof(T))));
            }
            return types;
        }
    }

}