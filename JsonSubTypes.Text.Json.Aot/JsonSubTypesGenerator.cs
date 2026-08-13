using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace JsonSubTypes.Text.Json.Aot
{
    [Generator]
    public sealed class JsonSubTypesGenerator : IIncrementalGenerator
    {
        private const string AttributeNamespace = "JsonSubTypes.Text.Json";
        private const string JsonSubTypesAotConverterAttributeName = "JsonSubTypesAotConverterAttribute";
        private const string KnownSubTypeAttributeName = "KnownSubTypeAttribute";
        private const string KnownSubTypeWithPropertyAttributeName = "KnownSubTypeWithPropertyAttribute";
        private const string FallBackSubTypeAttributeName = "FallBackSubTypeAttribute";
        private const string SystemTextJsonSerializationNamespace = "System.Text.Json.Serialization";
        private const string DiagnosticId = "JSTAOT001";
        private const string DuplicateDiscriminatorDiagnosticId = "JSTAOT002";
        private const string PresenceModeIgnoresValueDiagnosticId = "JSTAOT003";

        // Matches an attribute by its declaring namespace and short name, so a
        // homonymous attribute from another library is never mistaken for the one
        // the generator understands.
        private static bool IsAttribute(AttributeData attribute, string containingNamespace, string name)
        {
            INamedTypeSymbol? attributeClass = attribute.AttributeClass;
            return attributeClass != null
                && attributeClass.Name == name
                && attributeClass.ContainingNamespace?.ToDisplayString() == containingNamespace;
        }

        private static readonly DiagnosticDescriptor UnsupportedDiscriminator =
            new(
                DiagnosticId,
                "Discriminator value not supported by JsonSubTypes.Text.Json.Aot",
                "The discriminator value of type '{0}' on subtype '{1}' is not supported by JsonSubTypes.Text.Json.Aot. The subtype is not generated. Use the runtime converter for this hierarchy.",
                "JsonSubTypes.Text.Json.Aot",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor DuplicateDiscriminators =
            new(
                DuplicateDiscriminatorDiagnosticId,
                "Multiple discriminators on one type are not supported by JsonSubTypes.Text.Json.Aot",
                "Type '{0}' is registered with several discriminator values; only the last one is used for writing. The runtime converter's Build() rejects this configuration.",
                "JsonSubTypes.Text.Json.Aot",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor PresenceModeIgnoresValueRegistration =
            new(
                PresenceModeIgnoresValueDiagnosticId,
                "Value registration ignored in property-presence mode",
                "Type '{0}' is registered with a discriminator value but the hierarchy uses property-presence mode ([JsonSubTypesAotConverter] without a discriminator name). The value registration is ignored.",
                "JsonSubTypes.Text.Json.Aot",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<INamedTypeSymbol> baseTypes = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    $"{AttributeNamespace}.{JsonSubTypesAotConverterAttributeName}",
                    predicate: static (_, _) => true,
                    transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol!);

            context.RegisterSourceOutput(baseTypes.Collect(), static (spc, types) =>
            {
                if (types.Length == 0)
                {
                    return;
                }

                List<BaseTypeInfo> bases = [];
                foreach (ISymbol? symbol in types.Distinct(SymbolEqualityComparer.Default))
                {
                    INamedTypeSymbol? baseType = (INamedTypeSymbol?)symbol;
                    bases.Add(BuildBaseTypeInfo(baseType!, spc));
                }

                List<BaseTypeInfo> generated =
                [
                    .. bases
                        .Where(b => b.Subtypes.Count > 0 || b.HasPropertyPresence)
                ];
                if (generated.Count == 0)
                {
                    return;
                }

                BuildGlobalModel(generated);
                AssignUniqueConverterNames(generated);

                spc.AddSource("JsonSubTypesAotConverters.g.cs", SourceText.From(EmitRegistry(generated), System.Text.Encoding.UTF8));
                spc.AddSource("JsonSubTypesAotConverterBases.g.cs", SourceText.From(JsonSubTypesAotConverterBasesSource, System.Text.Encoding.UTF8));
                foreach (BaseTypeInfo baseInfo in generated)
                {
                    spc.AddSource($"{baseInfo.ConverterName}.g.cs", SourceText.From(EmitConverter(baseInfo), System.Text.Encoding.UTF8));
                }
            });
        }

        private sealed class BaseTypeInfo
        {
            public string FullyQualifiedName { get; set; } = "";
            public string TypeName { get; set; } = "";
            public string ConverterName { get; set; } = "";
            public string RegistryMemberName { get; set; } = "";
            public string? DiscriminatorPropertyName { get; set; }
            public bool AddDiscriminatorFirst { get; set; } = true;
            public bool HasPropertyPresence { get; set; }
            public List<SubtypeRegistration> Subtypes { get; } = [];
            public List<PropertyPresenceRegistration> PropertyPresences { get; } = [];
            public string? FallbackFullyQualifiedName { get; set; }

            public List<BaseProperty> Properties { get; } = [];
            public List<NestedChain> NestedTypes { get; } = [];

            public string FallbackType => FallbackFullyQualifiedName ?? FullyQualifiedName;
            public bool IsValueMode => DiscriminatorPropertyName != null;
            public bool BaseIsAbstractOrInterface { get; set; }
            public bool BaseHasParameterlessConstructor { get; set; }
        }

        private sealed class NestedChain
        {
            public string RuntimeTypeName { get; set; } = "";
            public List<ChainEntry> Chain { get; } = [];
        }

        private sealed class ChainEntry
        {
            public string DiscriminatorName { get; set; } = "";
            public SubtypeRegistration Discriminator { get; set; } = null!;
        }

        private sealed class SubtypeRegistration
        {
            public string FullyQualifiedName { get; set; } = "";
            public string DiscriminatorKind { get; set; } = ""; // "string" | "int" | "enum" | "null"
            public string DiscriminatorLiteral { get; set; } = "";
            public string? EnumMemberName { get; set; }
            public string? EnumUnderlyingValue { get; set; }
            public string? EnumTypeName { get; set; }
            public string? EnumReference { get; set; }
        }

        private sealed class PropertyPresenceRegistration
        {
            public string FullyQualifiedName { get; set; } = "";
            public string PropertyName { get; set; } = "";
            public bool StopLookupOnMatch { get; set; }
        }

        private sealed class BaseProperty
        {
            public string Name { get; set; } = "";
            public string JsonName { get; set; } = "";
            public string PropertyTypeName { get; set; } = "";
            public bool HasCustomName { get; set; }
            public bool HasGetter { get; set; }
            public bool HasSetter { get; set; }
            public string IgnoreCondition { get; set; } = "Never"; // Never | Always | WhenWritingNull | WhenWritingDefault
            public bool IsReferenceType { get; set; }
        }

        private static BaseTypeInfo BuildBaseTypeInfo(INamedTypeSymbol baseType, SourceProductionContext spc)
        {
            BaseTypeInfo info = new()
            {
                FullyQualifiedName = baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TypeName = baseType.Name,
                BaseIsAbstractOrInterface = baseType.TypeKind == TypeKind.Interface || baseType.IsAbstract,
                BaseHasParameterlessConstructor = baseType.InstanceConstructors.Any(c =>
                    c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)
            };

            ReadMarkerAttribute(baseType, info);
            ProcessRegistrationAttributes(baseType, info, spc);
            ReportDuplicateDiscriminators(baseType, info, spc, spc.CancellationToken);
            CollectBaseProperties(baseType, info);

            return info;
        }

        private static void ReadMarkerAttribute(INamedTypeSymbol baseType, BaseTypeInfo info)
        {
            foreach (AttributeData attr in baseType.GetAttributes())
            {
                if (!IsAttribute(attr, AttributeNamespace, JsonSubTypesAotConverterAttributeName))
                {
                    continue;
                }

                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string discName)
                {
                    info.DiscriminatorPropertyName = discName;
                }

                foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
                {
                    if (namedArg is { Key: "AddDiscriminatorFirst", Value.Value: bool b })
                    {
                        info.AddDiscriminatorFirst = b;
                    }
                }
            }
        }

        private static void ProcessRegistrationAttributes(INamedTypeSymbol baseType, BaseTypeInfo info,
            SourceProductionContext spc)
        {
            foreach (AttributeData attr in baseType.GetAttributes())
            {
                spc.CancellationToken.ThrowIfCancellationRequested();
                if (!IsAttribute(attr, AttributeNamespace, attr.AttributeClass?.Name ?? ""))
                {
                    continue;
                }
                switch (attr.AttributeClass?.Name)
                {
                    case KnownSubTypeAttributeName:
                        ProcessKnownSubType(attr, info, spc, spc.CancellationToken);
                        break;
                    case KnownSubTypeWithPropertyAttributeName:
                        ProcessKnownSubTypeWithProperty(attr, info);
                        break;
                    case FallBackSubTypeAttributeName:
                        ProcessFallBackSubType(attr, info);
                        break;
                }
            }
        }

        private static void ProcessKnownSubType(AttributeData attr, BaseTypeInfo info, SourceProductionContext spc,
            CancellationToken cancellationToken)
        {
            ITypeSymbol? subtype = attr.ConstructorArguments[0].Value as ITypeSymbol;
            if (subtype == null)
            {
                return;
            }
            
            if (info.DiscriminatorPropertyName == null)
            {
                // presence mode ignores value registrations; say so instead of staying silent
                spc.ReportDiagnostic(Diagnostic.Create(PresenceModeIgnoresValueRegistration,
                    attr.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                    subtype.Name));
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            SubtypeRegistration registration = new()
            {
                FullyQualifiedName = subtype.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            };
            if (TryGetDiscriminator(attr.ConstructorArguments[1], registration))
            {
                info.Subtypes.Add(registration);
            }
            else
            {
                ReportUnsupportedDiscriminator(spc, attr, subtype, cancellationToken);
            }
        }

        private static void ReportUnsupportedDiscriminator(SourceProductionContext spc, AttributeData attr,
            ITypeSymbol subtype, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            spc.ReportDiagnostic(Diagnostic.Create(UnsupportedDiscriminator,
                attr.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation(),
                attr.ConstructorArguments[1].Type?.Name ?? "null",
                subtype.Name));
        }

        private static void ProcessKnownSubTypeWithProperty(AttributeData attr, BaseTypeInfo info)
        {
            if (attr.ConstructorArguments[0].Value is not ITypeSymbol subtype || attr.ConstructorArguments[1].Value is not string propertyName)
            {
                return;
            }

            bool stopLookup = false;
            foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
            {
                if (namedArg is { Key: "StopLookupOnMatch", Value.Value: bool b })
                {
                    stopLookup = b;
                }
            }

            info.HasPropertyPresence = true;
            info.PropertyPresences.Add(new PropertyPresenceRegistration
            {
                FullyQualifiedName = subtype.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                PropertyName = propertyName,
                StopLookupOnMatch = stopLookup
            });
        }

        private static void ProcessFallBackSubType(AttributeData attr, BaseTypeInfo info)
        {
            if (attr.ConstructorArguments[0].Value is ITypeSymbol fallback)
            {
                info.FallbackFullyQualifiedName = fallback.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        private static void ReportDuplicateDiscriminators(INamedTypeSymbol baseType, BaseTypeInfo info,
            SourceProductionContext spc, CancellationToken cancellationToken)
        {
            foreach (IGrouping<string, SubtypeRegistration> duplicates in info.Subtypes.GroupBy(s => s.FullyQualifiedName))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (duplicates.Count() > 1)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(DuplicateDiscriminators,
                        baseType.Locations.FirstOrDefault(),
                        duplicates.Key));
                }
            }
        }

        private static void CollectBaseProperties(INamedTypeSymbol baseType, BaseTypeInfo info)
        {
            foreach (ISymbol member in baseType.GetMembers())
            {
                if (member is not IPropertySymbol property ||
                    property.IsStatic ||
                    property.DeclaredAccessibility != Accessibility.Public ||
                    (property.GetMethod == null && property.SetMethod == null))
                {
                    continue;
                }

                // The base object is written with the same rules System.Text.Json uses
                // for any other type: an always-ignored property is dropped from both
                // WriteBaseObject and DeserializeBase, while a conditionally-ignored one
                // (WhenWritingNull / WhenWritingDefault) is still read and written unless
                // the condition holds. Getters drive the write, setters the read.
                string ignoreCondition = "Never";
                string? jsonName = null;
                foreach (AttributeData attr in property.GetAttributes())
                {
                    if (IsAttribute(attr, SystemTextJsonSerializationNamespace, "JsonIgnoreAttribute"))
                    {
                        ignoreCondition = "Always";
                        foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
                        {
                            if (namedArg is { Key: "Condition", Value.Value: int condition })
                            {
                                ignoreCondition = condition switch
                                {
                                    0 => "Never",
                                    1 => "Always",
                                    2 => "WhenWritingDefault",
                                    3 => "WhenWritingNull",
                                    _ => "Always"
                                };
                            }
                        }
                    }
                    else if (IsAttribute(attr, SystemTextJsonSerializationNamespace, "JsonPropertyNameAttribute") &&
                        attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string name)
                    {
                        jsonName = name;
                    }
                }

                if (ignoreCondition == "Always")
                {
                    continue;
                }

                info.Properties.Add(new BaseProperty
                {
                    Name = property.Name,
                    JsonName = jsonName ?? property.Name,
                    PropertyTypeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    HasCustomName = jsonName != null,
                    HasGetter = property.GetMethod != null && property.GetMethod.DeclaredAccessibility == Accessibility.Public,
                    HasSetter = property.SetMethod != null &&
                        property.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                        !property.SetMethod.IsInitOnly,
                    IgnoreCondition = ignoreCondition,
                    IsReferenceType = !property.Type.IsValueType
                });
            }
        }

        private static bool TryGetDiscriminator(TypedConstant value, SubtypeRegistration registration)
        {
            if (value.Value == null)
            {
                registration.DiscriminatorKind = "null";
                return true;
            }

            if (value.Kind != TypedConstantKind.Primitive && value.Kind != TypedConstantKind.Enum)
            {
                return false;
            }

            if (value.Type?.SpecialType == SpecialType.System_String)
            {
                registration.DiscriminatorKind = "string";
                registration.DiscriminatorLiteral = SymbolDisplay.FormatLiteral((string)value.Value!, quote: true);
                return true;
            }

            if (value.Type is INamedTypeSymbol enumType && value.Type.TypeKind == TypeKind.Enum)
            {
                long underlying = Convert.ToInt64(value.Value);
                IFieldSymbol? member = enumType.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => f.HasConstantValue && Convert.ToInt64(f.ConstantValue) == underlying);
                if (member == null)
                {
                    return false;
                }

                registration.DiscriminatorKind = "enum";
                registration.DiscriminatorLiteral = underlying.ToString();
                registration.EnumMemberName = member.Name;
                registration.EnumUnderlyingValue = underlying.ToString();
                registration.EnumTypeName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                registration.EnumReference = registration.EnumTypeName + "." + member.Name;
                return true;
            }

            if (value.Type?.SpecialType == SpecialType.System_Int32)
            {
                registration.DiscriminatorKind = "int";
                registration.DiscriminatorLiteral = Convert.ToInt32(value.Value).ToString();
                return true;
            }

            return false;
        }

        // ---------------------------------------------------------- global model

        private static void AssignUniqueConverterNames(List<BaseTypeInfo> bases)
        {
            // Default: the short type name. If two base types share the same short name
            // (different namespaces), qualify them with the sanitized namespace so the
            // generated classes and file names do not collide.
            foreach (IGrouping<string, BaseTypeInfo> group in bases.GroupBy(b => b.TypeName))
            {
                foreach (BaseTypeInfo b in group)
                {
                    if (group.Count() == 1)
                    {
                        b.ConverterName = b.TypeName + "JsonSubTypesConverter";
                        b.RegistryMemberName = b.TypeName;
                    }
                    else
                    {
                        b.ConverterName = Sanitize(b.FullyQualifiedName) + "JsonSubTypesConverter";
                        b.RegistryMemberName = Sanitize(b.FullyQualifiedName);
                    }
                }
            }
        }

        private static string Sanitize(string fullyQualifiedName)
        {
            var builder = new System.Text.StringBuilder(fullyQualifiedName.Length);
            foreach (char c in fullyQualifiedName)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }
            return builder.ToString();
        }

        private static void BuildGlobalModel(List<BaseTypeInfo> bases)
        {
            Dictionary<string, BaseTypeInfo> baseByType = bases
                .Where(b => b.IsValueMode)
                .ToDictionary(b => b.FullyQualifiedName, b => b, StringComparer.Ordinal);

            Dictionary<string, List<BaseTypeInfo>> parents = BuildParentMap(bases);
            Dictionary<string, List<string>> ancestorCache = new(StringComparer.Ordinal);

            foreach (BaseTypeInfo b in bases)
            {
                if (!b.IsValueMode)
                {
                    continue;
                }

                HashSet<string> direct = [.. b.Subtypes.Select(s => s.FullyQualifiedName)];
                foreach ((string type, _) in BuildDescendants(b, baseByType))
                {
                    if (direct.Contains(type))
                    {
                        continue; // handled by the normal discriminator path
                    }

                    List<ChainEntry> chain = ComputeChain(b, type, baseByType, parents, ancestorCache);
                    if (chain.Count > 0)
                    {
                        NestedChain nested = new() { RuntimeTypeName = type };
                        nested.Chain.AddRange(chain);
                        b.NestedTypes.Add(nested);
                    }
                }
            }
        }

        private static Dictionary<string, List<BaseTypeInfo>> BuildParentMap(List<BaseTypeInfo> bases)
        {
            // subtype -> hierarchy bases where it is a direct subtype
            Dictionary<string, List<BaseTypeInfo>> parents = new(StringComparer.Ordinal);
            foreach (BaseTypeInfo b in bases)
            {
                foreach (SubtypeRegistration s in b.Subtypes)
                {
                    if (!parents.TryGetValue(s.FullyQualifiedName, out List<BaseTypeInfo>? list))
                    {
                        parents[s.FullyQualifiedName] = list = [];
                    }
                    list.Add(b);
                }
            }

            return parents;
        }

        private static List<string> TypeAncestors(string type, Dictionary<string, List<BaseTypeInfo>> parents,
            Dictionary<string, List<string>> cache)
        {
            if (cache.TryGetValue(type, out List<string>? cached))
            {
                return cached;
            }

            // ordered [T, parent bases, grandparent bases, ...]
            List<string> result = [type];
            HashSet<string> seen = [type];
            List<string> frontier = [type];
            while (frontier.Count > 0)
            {
                List<string> next = [];
                foreach (string f in frontier)
                {
                    if (parents.TryGetValue(f, out List<BaseTypeInfo>? hs))
                    {
                        foreach (BaseTypeInfo h in hs)
                        {
                            if (seen.Add(h.FullyQualifiedName))
                            {
                                result.Add(h.FullyQualifiedName);
                                next.Add(h.FullyQualifiedName);
                            }
                        }
                    }
                }

                frontier = next;
            }

            cache[type] = result;
            return result;
        }

        private static List<(string Type, int Depth)> BuildDescendants(BaseTypeInfo b,
            Dictionary<string, BaseTypeInfo> baseByType)
        {
            List<(string Type, int Depth)> descendants = [];
            Queue<(string Type, int Depth)> queue = new();
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (SubtypeRegistration s in b.Subtypes)
            {
                if (seen.Add(s.FullyQualifiedName))
                {
                    queue.Enqueue((s.FullyQualifiedName, 1));
                }
            }

            while (queue.Count > 0)
            {
                (string type, int depth) = queue.Dequeue();
                descendants.Add((type, depth));
                if (baseByType.TryGetValue(type, out BaseTypeInfo? intermediate))
                {
                    foreach (SubtypeRegistration s in intermediate.Subtypes)
                    {
                        if (seen.Add(s.FullyQualifiedName))
                        {
                            queue.Enqueue((s.FullyQualifiedName, depth + 1));
                        }
                    }
                }
            }

            return descendants;
        }

        private static List<ChainEntry> ComputeChain(BaseTypeInfo declaredBase, string type,
            Dictionary<string, BaseTypeInfo> baseByType, Dictionary<string, List<BaseTypeInfo>> parents,
            Dictionary<string, List<string>> ancestorCache)
        {
            List<string> ancestors = TypeAncestors(type, parents, ancestorCache);
            // hierarchy bases on the path strictly below-or-at the declared base, ordered
            // outer-first (declared base first, innermost last)
            List<string> pathBases =
            [
                .. ancestors
                    .Where(baseByType.ContainsKey)
                    .Where(t => t == declaredBase.FullyQualifiedName ||
                                TypeAncestors(t, parents, ancestorCache).Contains(declaredBase.FullyQualifiedName))
            ];
            pathBases.Reverse();

            List<ChainEntry> chain = [];
            foreach (string hName in pathBases)
            {
                BaseTypeInfo h = baseByType[hName];
                SubtypeRegistration? nearest = ancestors
                    .Select(a => h.Subtypes.FirstOrDefault(s => s.FullyQualifiedName == a))
                    .FirstOrDefault(s => s != null);
                if (nearest != null)
                {
                    chain.Add(new ChainEntry
                    {
                        DiscriminatorName = h.DiscriminatorPropertyName!,
                        Discriminator = nearest
                    });
                }
            }

            return chain;
        }

        // ---------------------------------------------------------------- emit

        private static string EmitRegistry(List<BaseTypeInfo> bases)
        {
            string converters = string.Join("\n",
                bases.Select(b => $"        public static readonly {b.ConverterName} {b.RegistryMemberName} = new {b.ConverterName}();"));
            return $$"""
                // <auto-generated/>
                #nullable enable

                namespace JsonSubTypes.Text.Json.Aot.Generated
                {
                    /// <summary>Generated by JsonSubTypes.Text.Json.Aot. Shared converter instances to add to JsonSerializerOptions.Converters.</summary>
                    [global::System.CodeDom.Compiler.GeneratedCode("JsonSubTypes.Text.Json.Aot", "1.0.0")]
                    public static class JsonSubTypesAotConverters
                    {
                {{converters}}
                    }
                }
                """;
        }

        private const string MemberOpenBrace = "        {\n";
        private const string MemberCloseBrace = "        }";

        // The shared skeleton: emitted once, exercised by every converter test.
        // Presence-mode converters inherit JsonSubTypesAotConverterBase<T> (its Write
        // is the simplified resolver path); value-mode converters inherit
        // JsonSubTypesAotValueConverterBase<T>, which adds the discriminator-injection
        // Write. Each generated converter only overrides the per-hierarchy members.
        private const string JsonSubTypesAotConverterBasesSource = """
                // <auto-generated/>
                #nullable enable
                using System;
                using System.Linq;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                namespace JsonSubTypes.Text.Json.Aot.Generated
                {
                    /// <summary>Generated by JsonSubTypes.Text.Json.Aot. Shared converter skeleton: Read, the dynamic-subtype machinery and the base-object helpers live here once so every converter test exercises them.</summary>
                    [global::System.CodeDom.Compiler.GeneratedCode("JsonSubTypes.Text.Json.Aot", "1.0.0")]
                    public abstract class JsonSubTypesAotConverterBase<T> : JsonConverter<T> where T : class
                    {
                        public sealed override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(T);

                        public sealed override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                        {
                            if (reader.TokenType == JsonTokenType.Null)
                            {
                                return null;
                            }
                            if (reader.TokenType != JsonTokenType.StartObject)
                            {
                                throw new JsonException("Unrecognized token: " + reader.TokenType);
                            }
                            using JsonDocument document = JsonDocument.ParseValue(ref reader);
                            JsonElement root = document.RootElement;
                            Type target = SelectType(root, options);
                            if (target == typeof(T))
                            {
                                return DeserializeBase(root, options);
                            }
                            return (T?)JsonSerializer.Deserialize(root, options.GetTypeInfo(target));
                        }

                        // Presence-mode write: the base object is written through WriteBaseObject,
                        // registered subtypes through their own resolver-provided converters, and
                        // unknown types through the type info resolver. Value-mode converters
                        // override Write to inject the discriminator.
                        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
                        {
                            if (value is null)
                            {
                                writer.WriteNullValue();
                                return;
                            }
                            Type runtimeType = value.GetType();
                            if (runtimeType == typeof(T))
                            {
                                WriteBaseObject(writer, value, options);
                                return;
                            }
                            JsonSerializer.Serialize(writer, value, options.GetTypeInfo(runtimeType));
                        }

                        protected abstract Type SelectType(JsonElement root, JsonSerializerOptions options);
                        protected abstract T DeserializeBase(JsonElement root, JsonSerializerOptions options);
                        protected abstract void WriteBaseObject(Utf8JsonWriter writer, T value, JsonSerializerOptions options);
                        protected abstract bool TryWriteNestedObject(Utf8JsonWriter writer, T value, Type runtimeType, JsonSerializerOptions options);

                        // Discriminator property name used by the dynamic write path; unused by
                        // presence-mode converters.
                        protected virtual string DiscriminatorPropertyName => "";

                        public System.Collections.Concurrent.ConcurrentDictionary<object, Type> DynamicSubtypes { get; } = new System.Collections.Concurrent.ConcurrentDictionary<object, Type>();
                        private readonly System.Collections.Concurrent.ConcurrentDictionary<Type, object> _dynamicReverse = new System.Collections.Concurrent.ConcurrentDictionary<Type, object>();

                        public void RegisterDynamicSubtype(object discriminator, Type type)
                        {
                            DynamicSubtypes[discriminator] = type;
                            _dynamicReverse[type] = discriminator; // last registration wins, like the builder
                        }

                        /// <summary>
                        /// Custom discriminator-to-type resolution hook, invoked after the static
                        /// registrations and DynamicSubtypes. Assign it to implement your own
                        /// name-based lookup (e.g. assembly scanning, a DI registry). The resolved
                        /// type must be resolvable by the TypeInfoResolver (in the source-gen context
                        /// for Native AOT).
                        /// </summary>
                        public Func<object?, Type?>? CustomTypeNameResolver { get; set; }

                        protected static object? GetDiscriminatorKey(JsonElement discriminator)
                        {
                            switch (discriminator.ValueKind)
                            {
                                case JsonValueKind.String:
                                    return discriminator.GetString();
                                case JsonValueKind.Number when int.TryParse(discriminator.GetRawText(), out int keyInt):
                                    return keyInt;
                                default:
                                    return discriminator.GetRawText();
                            }
                        }

                        protected bool TryGetDynamicType(JsonElement discriminator, out Type? dynamicType)
                        {
                            switch (discriminator.ValueKind)
                            {
                                case JsonValueKind.String:
                                    return DynamicSubtypes.TryGetValue(discriminator.GetString()!, out dynamicType);
                                case JsonValueKind.Number:
                                    if (int.TryParse(discriminator.GetRawText(), out int dynamicInt) && DynamicSubtypes.TryGetValue(dynamicInt, out dynamicType))
                                    {
                                        return true;
                                    }
                                    return DynamicSubtypes.TryGetValue(discriminator.GetRawText(), out dynamicType);
                                default:
                                    return DynamicSubtypes.TryGetValue(discriminator.GetRawText(), out dynamicType);
                            }
                        }

                        protected bool TryWriteDynamic(Utf8JsonWriter writer, T value, Type runtimeType, JsonSerializerOptions options)
                        {
                            if (_dynamicReverse.TryGetValue(runtimeType, out object? dynamicDiscriminator))
                            {
                                writer.WriteStartObject();
                                string dynamicDiscriminatorName = DiscriminatorPropertyName;
                                if (options.PropertyNamingPolicy != null)
                                {
                                    dynamicDiscriminatorName = options.PropertyNamingPolicy.ConvertName(dynamicDiscriminatorName);
                                }
                                writer.WritePropertyName(dynamicDiscriminatorName);
                                writer.WriteRawValue(JsonSerializer.Serialize(dynamicDiscriminator, options.GetTypeInfo(dynamicDiscriminator.GetType())));
                                string payload = JsonSerializer.Serialize(value, options.GetTypeInfo(runtimeType));
                                using JsonDocument payloadDocument = JsonDocument.Parse(payload);
                                foreach (JsonProperty property in payloadDocument.RootElement.EnumerateObject())
                                {
                                    property.WriteTo(writer);
                                }
                                writer.WriteEndObject();
                                return true;
                            }
                            return false;
                        }

                        protected string SerializeBasePayload(T value, JsonSerializerOptions options)
                        {
                            using System.IO.MemoryStream stream = new System.IO.MemoryStream();
                            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
                            {
                                WriteBaseObject(writer, value, options);
                            }
                            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
                        }

                        protected static bool TryGetProperty(JsonElement root, string name, JsonSerializerOptions options, out JsonElement value)
                        {
                            if (root.TryGetProperty(name, out value))
                            {
                                return true;
                            }
                            string? convertedName = options.PropertyNamingPolicy?.ConvertName(name);
                            if (convertedName != null && convertedName != name && root.TryGetProperty(convertedName, out value))
                            {
                                return true;
                            }
                            if (options.PropertyNameCaseInsensitive)
                            {
                                foreach (JsonProperty property in root.EnumerateObject().Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                                {
                                    value = property.Value;
                                    return true;
                                }
                            }
                            return false;
                        }

                        protected static bool TryGetValueInJson(JsonElement root, string propertyName, JsonSerializerOptions options, out JsonElement value)
                        {
                            if (TryGetProperty(root, propertyName, options, out value))
                            {
                                return true;
                            }
                            if (propertyName.Contains('.'))
                            {
                                string[] segments = propertyName.Split('.');
                                JsonElement current = root;
                                foreach (string segment in segments)
                                {
                                    if (!TryGetProperty(current, segment, options, out current))
                                    {
                                        value = default;
                                        return false;
                                    }
                                }
                                value = current;
                                return true;
                            }
                            return false;
                        }
                    }

                    /// <summary>Generated by JsonSubTypes.Text.Json.Aot. Value-mode converter skeleton: adds the discriminator-injection Write on top of JsonSubTypesAotConverterBase.</summary>
                    [global::System.CodeDom.Compiler.GeneratedCode("JsonSubTypes.Text.Json.Aot", "1.0.0")]
                    public abstract class JsonSubTypesAotValueConverterBase<T> : JsonSubTypesAotConverterBase<T> where T : class
                    {
                        public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
                        {
                            if (value is null)
                            {
                                writer.WriteNullValue();
                                return;
                            }

                            Type runtimeType = value.GetType();
                            if (!TryGetPayload(writer, value, runtimeType, options, out string payload))
                            {
                                return;
                            }

                            using JsonDocument payloadDocument = JsonDocument.Parse(payload);
                            string discriminatorName = options.PropertyNamingPolicy?.ConvertName(DiscriminatorPropertyName) ?? DiscriminatorPropertyName;
                            writer.WriteStartObject();
                            if (AddDiscriminatorFirst)
                            {
                                writer.WritePropertyName(discriminatorName);
                                WriteDiscriminatorValue(writer, runtimeType, options);
                            }
                            foreach (JsonProperty property in payloadDocument.RootElement.EnumerateObject().Where(p => !p.NameEquals(discriminatorName)))
                            {
                                property.WriteTo(writer);
                            }
                            if (!AddDiscriminatorFirst)
                            {
                                writer.WritePropertyName(discriminatorName);
                                WriteDiscriminatorValue(writer, runtimeType, options);
                            }
                            writer.WriteEndObject();
                        }

                        // Resolves the serialized payload of the runtime type. Returns false
                        // when the value was already written directly (base fallback, nested
                        // chain, dynamic subtype or unregistered runtime type).
                        private bool TryGetPayload(Utf8JsonWriter writer, T value, Type runtimeType, JsonSerializerOptions options, out string payload)
                        {
                            payload = "";
                            if (runtimeType == typeof(T))
                            {
                                if (IsRegistered(runtimeType))
                                {
                                    payload = SerializeBasePayload(value, options);
                                    return true;
                                }
                                WriteBaseObject(writer, value, options);
                                return false;
                            }
                            if (IsRegistered(runtimeType))
                            {
                                payload = JsonSerializer.Serialize(value, options.GetTypeInfo(runtimeType));
                                return true;
                            }
                            if (TryWriteNestedObject(writer, value, runtimeType, options))
                            {
                                return false;
                            }
                            if (TryWriteDynamic(writer, value, runtimeType, options))
                            {
                                return false;
                            }
                            JsonSerializer.Serialize(writer, value, options.GetTypeInfo(runtimeType));
                            return false;
                        }

                        protected virtual System.Collections.Generic.Dictionary<System.Type, System.Action<Utf8JsonWriter, JsonSerializerOptions>> DiscriminatorWriters { get; } = new System.Collections.Generic.Dictionary<System.Type, System.Action<Utf8JsonWriter, JsonSerializerOptions>>();

                        protected bool IsRegistered(Type runtimeType)
                        {
                            return DiscriminatorWriters.ContainsKey(runtimeType);
                        }

                        protected virtual bool AddDiscriminatorFirst => true;

                        private void WriteDiscriminatorValue(Utf8JsonWriter writer, Type runtimeType, JsonSerializerOptions options)
                        {
                            if (DiscriminatorWriters.TryGetValue(runtimeType, out System.Action<Utf8JsonWriter, JsonSerializerOptions>? write))
                            {
                                write(writer, options);
                                return;
                            }
                            throw new JsonException("Impossible to serialize type: " + runtimeType.FullName + " because there is no registered mapping for the discriminator property");
                        }
                    }
                }
                """;

        private static string EmitConverter(BaseTypeInfo info)
        {
            string usingLinq = info.IsValueMode ? "" : "using System.Linq;\n";
            string baseClass = info.IsValueMode
                ? $"JsonSubTypesAotValueConverterBase<{info.FullyQualifiedName}>"
                : $"JsonSubTypesAotConverterBase<{info.FullyQualifiedName}>";
            string valueModeMembers = info.IsValueMode ? EmitValueModeMembers(info) : "";

            return $$"""
                // <auto-generated/>
                #nullable enable
                using System;
                {{usingLinq}}using System.Text.Json;
                using System.Text.Json.Serialization;

                namespace JsonSubTypes.Text.Json.Aot.Generated
                {
                    /// <summary>Generated by JsonSubTypes.Text.Json.Aot. Compiled converter for {{info.FullyQualifiedName}}.</summary>
                    [global::System.CodeDom.Compiler.GeneratedCode("JsonSubTypes.Text.Json.Aot", "1.0.0")]
                    public sealed class {{info.ConverterName}} : {{baseClass}}
                    {
                {{valueModeMembers}}{{EmitSelectTypeMethod(info)}}

                {{EmitWriteBaseObject(info)}}
                {{EmitDeserializeBase(info)}}
                {{EmitTryWriteNestedObject(info)}}
                    }
                }
                """;
        }

        private static string EmitValueModeMembers(BaseTypeInfo info)
        {
            string discriminatorProperty =
                $"        private const string DiscriminatorPropertyNameValue = {SymbolDisplay.FormatLiteral(info.DiscriminatorPropertyName!, quote: true)};\n" +
                "        protected override string DiscriminatorPropertyName => DiscriminatorPropertyNameValue;\n\n";
            string addDiscriminatorFirst = info.AddDiscriminatorFirst ? "" : "        protected override bool AddDiscriminatorFirst => false;\n\n";
            string discriminatorWriters = EmitDiscriminatorWriters(info) + "\n\n";
            return discriminatorProperty + addDiscriminatorFirst + discriminatorWriters;
        }

        private static string EmitSelectTypeMethod(BaseTypeInfo info)
        {
            return info.IsValueMode ? EmitValueModeSelectType(info) : EmitPresenceModeSelectType(info);
        }

        private static string EmitValueModeSelectType(BaseTypeInfo info)
        {
            List<SubtypeRegistration> nulls = [.. info.Subtypes.Where(s => s.DiscriminatorKind == "null")];
            string nullReturns = string.Join("\n",
                nulls.Select(r => r.FullyQualifiedName).Distinct()
                    .Select(t => $"                    return typeof({t});"));

            List<SubtypeRegistration> strings = [.. info.Subtypes.Where(s => s.DiscriminatorKind == "string")];
            List<SubtypeRegistration> enums = [.. info.Subtypes.Where(s => s.DiscriminatorKind == "enum")];
            string stringCases = string.Join("\n",
                strings.Select(r => $"case {r.DiscriminatorLiteral}: return typeof({r.FullyQualifiedName});")
                    .Concat(enums.Select(r => $"case {SymbolDisplay.FormatLiteral(r.EnumMemberName!, quote: true)}: return typeof({r.FullyQualifiedName});")));

            List<SubtypeRegistration> ints = [.. info.Subtypes.Where(s => s.DiscriminatorKind == "int")];
            string numberCases = string.Join("\n",
                ints.Select(r => $"case {SymbolDisplay.FormatLiteral(r.DiscriminatorLiteral, quote: true)}: return typeof({r.FullyQualifiedName});")
                    .Concat(enums.Select(r => $"case {SymbolDisplay.FormatLiteral(r.EnumUnderlyingValue!, quote: true)}: return typeof({r.FullyQualifiedName});")));

            string switchBody = "";
            if (nulls.Count > 0)
            {
                switchBody += "                case JsonValueKind.Null:\n" + nullReturns + "\n";
            }
            if (strings.Count + enums.Count > 0)
            {
                switchBody += "                case JsonValueKind.String:\n" +
                              "                    switch (discriminator.GetString())\n" +
                              "                    {\n" +
                              "                        " + stringCases.Replace("\n", "\n                        ") + "\n" +
                              "                    }\n" +
                              "                    break;\n";
            }
            if (ints.Count + enums.Count > 0)
            {
                switchBody += "                case JsonValueKind.Number:\n" +
                              "                    switch (discriminator.GetRawText())\n" +
                              "                    {\n" +
                              "                        " + numberCases.Replace("\n", "\n                        ") + "\n" +
                              "                    }\n" +
                              "                    break;\n";
            }

            string selectType =
                "        protected override Type SelectType(JsonElement root, JsonSerializerOptions options)\n" +
                MemberOpenBrace +
                "            if (!TryGetValueInJson(root, DiscriminatorPropertyNameValue, options, out JsonElement discriminator))\n" +
                "            {\n" +
                "                return typeof(" + info.FallbackType + ");\n" +
                "            }\n" +
                "            Type? staticType = ResolveStaticType(discriminator);\n" +
                "            if (staticType != null)\n" +
                "            {\n" +
                "                return staticType;\n" +
                "            }\n" +
                "            if (TryGetDynamicType(discriminator, out Type? dynamicType))\n" +
                "            {\n" +
                "                return dynamicType!;\n" +
                "            }\n" +
                "            if (CustomTypeNameResolver is not null)\n" +
                "            {\n" +
                "                Type? customType = CustomTypeNameResolver(GetDiscriminatorKey(discriminator));\n" +
                "                if (customType != null)\n" +
                "                {\n" +
                "                    return customType;\n" +
                "                }\n" +
                "            }\n" +
                "            return typeof(" + info.FallbackType + ");\n" +
                MemberCloseBrace;

            string resolveStaticType =
                "\n\n" +
                "        private static Type? ResolveStaticType(JsonElement discriminator)\n" +
                MemberOpenBrace +
                "            switch (discriminator.ValueKind)\n" +
                "            {\n" +
                switchBody +
                "            }\n" +
                "            return null;\n" +
                MemberCloseBrace;

            return selectType + resolveStaticType;
        }

        private static string EmitPresenceModeSelectType(BaseTypeInfo info)
        {
            List<string> checks = [];
            foreach (PropertyPresenceRegistration reg in info.PropertyPresences)
            {
                checks.Add(reg.StopLookupOnMatch
                    ? "        if (root.TryGetProperty(" + SymbolDisplay.FormatLiteral(reg.PropertyName, quote: true) + ", out _))\n" +
                      MemberOpenBrace +
                      "            return typeof(" + reg.FullyQualifiedName + ");\n" +
                      MemberCloseBrace
                    : "        if (root.TryGetProperty(" + SymbolDisplay.FormatLiteral(reg.PropertyName, quote: true) + ", out _))\n" +
                      MemberOpenBrace +
                      "            matches.Add(typeof(" + reg.FullyQualifiedName + "));\n" +
                      MemberCloseBrace);
            }

            string presenceChecks = string.Join("\n", checks);
            return "        protected override Type SelectType(JsonElement root, JsonSerializerOptions options)\n" +
                   MemberOpenBrace +
                   "            System.Collections.Generic.HashSet<Type> matches = new System.Collections.Generic.HashSet<Type>();\n" +
                   presenceChecks + "\n" +
                   "            if (matches.Count == 1)\n" +
                   "            {\n" +
                   "                return matches.First();\n" +
                   "            }\n" +
                   "            if (matches.Count > 1)\n" +
                   "            {\n" +
                   "                throw new JsonException(\"Ambiguous type resolution, expected only one type but got: \" + string.Join(\", \", matches.Select(t => t.FullName)));\n" +
                   "            }\n" +
                   "            return typeof(" + info.FallbackType + ");\n" +
                   MemberCloseBrace;
        }

        private static string EmitDiscriminatorWriters(BaseTypeInfo info)
        {
            Dictionary<string, SubtypeRegistration> byType = new();
            foreach (SubtypeRegistration reg in info.Subtypes)
            {
                byType[reg.FullyQualifiedName] = reg; // last registration wins for writing
            }

            List<string> dictionaryEntries = [];
            foreach (SubtypeRegistration reg in byType.Values)
            {
                string value = reg.DiscriminatorKind switch
                {
                    "string" => $"writer.WriteStringValue({reg.DiscriminatorLiteral});",
                    "int" => $"writer.WriteNumberValue({reg.DiscriminatorLiteral});",
                    "enum" => $"writer.WriteRawValue(JsonSerializer.Serialize({reg.EnumReference}, options.GetTypeInfo(typeof({reg.EnumTypeName}))));",
                    _ => "writer.WriteNullValue();"
                };
                dictionaryEntries.Add($"            [typeof({reg.FullyQualifiedName})] = static (writer, options) => {value.TrimEnd(';')}");
            }

            return "        protected override System.Collections.Generic.Dictionary<System.Type, System.Action<Utf8JsonWriter, JsonSerializerOptions>> DiscriminatorWriters { get; } = new System.Collections.Generic.Dictionary<System.Type, System.Action<Utf8JsonWriter, JsonSerializerOptions>>\n" +
                   MemberOpenBrace +
                   string.Join(",\n", dictionaryEntries) + "\n" +
                   "        };";
        }

        private static string EmitNestedCases(BaseTypeInfo info)
        {
            if (info.NestedTypes.Count == 0)
            {
                return "";
            }

            List<string> blocks = [];
            foreach (NestedChain nested in info.NestedTypes)
            {
                List<string> discLines = [];
                foreach (ChainEntry entry in nested.Chain)
                {
                    string discriminatorName = entry.DiscriminatorName == info.DiscriminatorPropertyName
                    ? "DiscriminatorPropertyNameValue"
                    : SymbolDisplay.FormatLiteral(entry.DiscriminatorName, quote: true);
                discLines.Add($"                        writer.WritePropertyName({discriminatorName});");
                    discLines.Add($"                        {EmitDiscriminatorValueStatement(entry.Discriminator)}");
                }
                string payload = $$"""
                            string payload = JsonSerializer.Serialize(value, options.GetTypeInfo(runtimeType));
                            using JsonDocument payloadDocument = JsonDocument.Parse(payload);
                            foreach (JsonProperty property in payloadDocument.RootElement.EnumerateObject())
                            {
                                property.WriteTo(writer);
                            }
                            writer.WriteEndObject();
                            return true;
                """;
                blocks.Add($$"""
                        if (runtimeType == typeof({{nested.RuntimeTypeName}}))
                        {
                            writer.WriteStartObject();
                {{string.Join("\n", discLines)}}
                {{payload}}
                        }
                """);
            }

            return string.Join("\n", blocks);
        }

        private static string EmitDiscriminatorValueStatement(SubtypeRegistration reg)
        {
            return reg.DiscriminatorKind switch
            {
                "string" => $"writer.WriteStringValue({reg.DiscriminatorLiteral});",
                "int" => $"writer.WriteNumberValue({reg.DiscriminatorLiteral});",
                "enum" => $"writer.WriteRawValue(JsonSerializer.Serialize({reg.EnumReference}, options.GetTypeInfo(typeof({reg.EnumTypeName}))));",
                _ => "writer.WriteNullValue();"
            };
        }

        private static string Indent(string text, int spaces)
        {
            string padding = new string(' ', spaces);
            return padding + text.Replace("\n", "\n" + padding);
        }

        private static string EmitWriteBaseObject(BaseTypeInfo info)
        {
            List<string> writeProperties = [];
            foreach (BaseProperty prop in info.Properties)
            {
                if (!prop.HasGetter)
                {
                    continue;
                }

                string applyPolicy = prop.HasCustomName ? "false" : "true";
                string write =
                    "            string name" + prop.Name + " = " + SymbolDisplay.FormatLiteral(prop.JsonName, quote: true) + ";\n" +
                    "            if (options.PropertyNamingPolicy != null && " + applyPolicy + ")\n" +
                    "            {\n" +
                    "                name" + prop.Name + " = options.PropertyNamingPolicy.ConvertName(name" + prop.Name + ");\n" +
                    "            }\n" +
                    "            writer.WritePropertyName(name" + prop.Name + ");\n" +
                    "            JsonSerializer.Serialize(writer, value." + prop.Name + ", options.GetTypeInfo(typeof(" + prop.PropertyTypeName + ")));";

                // JsonIgnoreCondition.WhenWritingNull/WhenWritingDefault only skip the
                // write when the condition holds; the property is still read on
                // deserialization. WhenWritingNull never applies to value types.
                if (prop.IgnoreCondition == "WhenWritingNull" && prop.IsReferenceType)
                {
                    write = "            if (value." + prop.Name + " != null)\n" +
                            "            {\n" +
                            Indent(write, 4) + "\n" +
                            "            }";
                }
                else if (prop.IgnoreCondition == "WhenWritingDefault")
                {
                    write = "            if (!global::System.Collections.Generic.EqualityComparer<" + prop.PropertyTypeName + ">.Default.Equals(value." + prop.Name + ", default))\n" +
                            "            {\n" +
                            Indent(write, 4) + "\n" +
                            "            }";
                }

                writeProperties.Add(write);
            }

            string body = string.Join("\n", writeProperties);
            return "        protected override void WriteBaseObject(Utf8JsonWriter writer, " + info.FullyQualifiedName + " value, JsonSerializerOptions options)\n" +
                   MemberOpenBrace +
                   "            writer.WriteStartObject();\n" +
                   body + "\n" +
                   "            writer.WriteEndObject();\n" +
                   MemberCloseBrace;
        }

        private static string EmitDeserializeBase(BaseTypeInfo info)
        {
            if (info.BaseIsAbstractOrInterface)
            {
                return "        protected override " + info.FullyQualifiedName + " DeserializeBase(JsonElement root, JsonSerializerOptions options)\n" +
                       MemberOpenBrace +
                       "            throw new JsonException(\"Could not create an instance of type " + info.FullyQualifiedName + ". Type is an interface or abstract class and cannot be instantiated.\");\n" +
                       MemberCloseBrace;
            }

            if (!info.BaseHasParameterlessConstructor)
            {
                return "        protected override " + info.FullyQualifiedName + " DeserializeBase(JsonElement root, JsonSerializerOptions options)\n" +
                       MemberOpenBrace +
                       "            throw new JsonException(\"Could not create an instance of type " + info.FullyQualifiedName + ": a parameterless constructor is required to fall back to the base type.\");\n" +
                       MemberCloseBrace;
            }

            List<string> readProperties = [];
            foreach (BaseProperty prop in info.Properties)
            {
                if (!prop.HasSetter)
                {
                    continue;
                }

                readProperties.Add(
                    "            if (TryGetProperty(root, " + SymbolDisplay.FormatLiteral(prop.JsonName, quote: true) + ", options, out JsonElement " + prop.Name + "Value))\n" +
                    "            {\n" +
                    "                instance." + prop.Name + " = (" + prop.PropertyTypeName + ")JsonSerializer.Deserialize(" + prop.Name + "Value.GetRawText(), options.GetTypeInfo(typeof(" + prop.PropertyTypeName + ")))!;\n" +
                    "            }");
            }

            string body = string.Join("\n", readProperties);
            return "        protected override " + info.FullyQualifiedName + " DeserializeBase(JsonElement root, JsonSerializerOptions options)\n" +
                   MemberOpenBrace +
                   "            " + info.FullyQualifiedName + " instance = new " + info.FullyQualifiedName + "();\n" +
                   body + "\n" +
                   "            return instance;\n" +
                   MemberCloseBrace;
        }

        private static string EmitTryWriteNestedObject(BaseTypeInfo info)
        {
            string nestedCases = EmitNestedCases(info);
            return "        protected override bool TryWriteNestedObject(Utf8JsonWriter writer, " + info.FullyQualifiedName + " value, Type runtimeType, JsonSerializerOptions options)\n" +
                   MemberOpenBrace +
                   (nestedCases.Length == 0 ? "" : nestedCases + "\n") +
                   "            return false;\n" +
                   MemberCloseBrace;
        }
    }
}
