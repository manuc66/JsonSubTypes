using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace JsonSubTypes.Aot
{
    [Generator]
    public sealed class JsonSubTypesGenerator : IIncrementalGenerator
    {
        private const string AttributeNamespace = "JsonSubTypes.Text.Json";
        private const string JsonSubTypesAotConverterAttributeName = "JsonSubTypesAotConverterAttribute";
        private const string KnownSubTypeAttributeName = "KnownSubTypeAttribute";
        private const string KnownSubTypeWithPropertyAttributeName = "KnownSubTypeWithPropertyAttribute";
        private const string FallBackSubTypeAttributeName = "FallBackSubTypeAttribute";
        private const string DiagnosticId = "JSTAOT001";
        private const string DuplicateDiscriminatorDiagnosticId = "JSTAOT002";

        private static readonly DiagnosticDescriptor UnsupportedDiscriminator =
            new(
                DiagnosticId,
                "Discriminator value not supported by JsonSubTypes.Aot",
                "The discriminator value of type '{0}' on subtype '{1}' is not supported by JsonSubTypes.Aot. The subtype is not generated. Use the runtime converter for this hierarchy.",
                "JsonSubTypes.Aot",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor DuplicateDiscriminators =
            new(
                DuplicateDiscriminatorDiagnosticId,
                "Multiple discriminators on one type are not supported by JsonSubTypes.Aot",
                "Type '{0}' is registered with several discriminator values; only the last one is used for writing. The runtime converter's Build() rejects this configuration.",
                "JsonSubTypes.Aot",
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

                spc.AddSource("JsonSubTypesAotConverters.g.cs", SourceText.From(EmitRegistry(generated), System.Text.Encoding.UTF8));
                foreach (BaseTypeInfo baseInfo in generated)
                {
                    spc.AddSource($"{baseInfo.TypeName}JsonSubTypesConverter.g.cs", SourceText.From(EmitConverter(baseInfo), System.Text.Encoding.UTF8));
                }
            });
        }

        private sealed class BaseTypeInfo
        {
            public string FullyQualifiedName { get; set; } = "";
            public string TypeName { get; set; } = "";
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
                if (attr.AttributeClass?.Name != JsonSubTypesAotConverterAttributeName)
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
                return; // presence mode ignores value registrations
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
                attr.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
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
                    property.GetMethod == null ||
                    property.SetMethod == null)
                {
                    continue;
                }

                bool ignored = false;
                string? jsonName = null;
                foreach (AttributeData attr in property.GetAttributes())
                {
                    switch (attr.AttributeClass?.Name)
                    {
                        case "JsonIgnoreAttribute":
                            ignored = true;
                            break;
                        case "JsonPropertyNameAttribute" when
                            attr.ConstructorArguments.Length > 0 &&
                            attr.ConstructorArguments[0].Value is string name:
                            jsonName = name;
                            break;
                    }
                }

                if (!ignored)
                {
                    info.Properties.Add(new BaseProperty
                    {
                        Name = property.Name,
                        JsonName = jsonName ?? property.Name,
                        PropertyTypeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        HasCustomName = jsonName != null,
                        HasGetter = property.GetMethod.DeclaredAccessibility == Accessibility.Public,
                        HasSetter = property.SetMethod.DeclaredAccessibility == Accessibility.Public
                    });
                }
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
                bases.Select(b => $"        public static readonly {b.TypeName}JsonSubTypesConverter {b.TypeName} = new {b.TypeName}JsonSubTypesConverter();"));
            return $$"""
                // <auto-generated/>
                #nullable enable

                namespace JsonSubTypes.Aot.Generated
                {
                    /// <summary>Generated by JsonSubTypes.Aot. Shared converter instances to add to JsonSerializerOptions.Converters.</summary>
                    [global::System.CodeDom.Compiler.GeneratedCode("JsonSubTypes.Aot", "1.0.0")]
                    public static class JsonSubTypesAotConverters
                    {
                {{converters}}
                    }
                }
                """;
        }

        private static string EmitConverter(BaseTypeInfo info)
        {
            string write = EmitWriteMethod(info);
            string read = EmitReadMethod(info);
            string selectType = EmitSelectTypeMethod(info);
            string discriminatorWriter = info.IsValueMode ? EmitDiscriminatorWriter(info) : "";
            string baseHelpers = EmitBaseHelpers(info);
            return $$"""
                // <auto-generated/>
                #nullable enable
                using System;
                using System.Linq;
                using System.Text.Json;
                using System.Text.Json.Serialization;

                namespace JsonSubTypes.Aot.Generated
                {
                    /// <summary>Generated by JsonSubTypes.Aot. Compiled converter for {{info.FullyQualifiedName}}.</summary>
                    [global::System.CodeDom.Compiler.GeneratedCode("JsonSubTypes.Aot", "1.0.0")]
                    public sealed class {{info.TypeName}}JsonSubTypesConverter : JsonConverter<{{info.FullyQualifiedName}}>
                    {
                        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof({{info.FullyQualifiedName}});

                        public override void Write(Utf8JsonWriter writer, {{info.FullyQualifiedName}} value, JsonSerializerOptions options)
                        {
                {{write}}
                        }

                        public override {{info.FullyQualifiedName}}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                        {
                {{read}}
                        }

                        private Type SelectType(JsonElement root, JsonSerializerOptions options)
                        {
                {{selectType}}
                        }

                {{discriminatorWriter}}
                {{baseHelpers}}
                    }
                }
                """;
        }

        private static string EmitWriteMethod(BaseTypeInfo info)
        {
            if (!info.IsValueMode)
            {
                // presence mode is read-only: subtypes serialize through the resolver, base via WriteBaseObject
                return $$"""
                            if (value is null)
                            {
                                writer.WriteNullValue();
                                return;
                            }
                            Type runtimeType = value.GetType();
                            if (runtimeType == typeof({{info.FullyQualifiedName}}))
                            {
                                WriteBaseObject(writer, ({{info.FullyQualifiedName}})value, options);
                                return;
                            }
                            JsonSerializer.Serialize(writer, value, options.GetTypeInfo(runtimeType));
                """;
            }

            string payloadSelection = $$"""
                            Type runtimeType = value.GetType();
                            string payload;
                            if (runtimeType == typeof({{info.FullyQualifiedName}}))
                            {
                                if (IsRegistered(runtimeType))
                                {
                                    payload = SerializeBasePayload(({{info.FullyQualifiedName}})value, options);
                                }
                                else
                                {
                                    WriteBaseObject(writer, ({{info.FullyQualifiedName}})value, options);
                                    return;
                                }
                            }
                            else if (IsRegistered(runtimeType))
                            {
                                payload = JsonSerializer.Serialize(value, options.GetTypeInfo(runtimeType));
                            }
                            else if (TryWriteNestedObject(writer, value, runtimeType, options))
                            {
                                return;
                            }
                            else if (TryWriteDynamic(writer, value, runtimeType, options))
                            {
                                return;
                            }
                            else
                            {
                                JsonSerializer.Serialize(writer, value, options.GetTypeInfo(runtimeType));
                                return;
                            }
                """;
            string order = info.AddDiscriminatorFirst
                ? $$"""
                            writer.WritePropertyName(discriminatorName);
                            WriteDiscriminatorValue(writer, runtimeType, options);
                            foreach (JsonProperty property in payloadDocument.RootElement.EnumerateObject())
                            {
                                if (!property.NameEquals(discriminatorName))
                                {
                                    property.WriteTo(writer);
                                }
                            }
                """
                : $$"""
                            foreach (JsonProperty property in payloadDocument.RootElement.EnumerateObject())
                            {
                                if (!property.NameEquals(discriminatorName))
                                {
                                    property.WriteTo(writer);
                                }
                            }
                            writer.WritePropertyName(discriminatorName);
                            WriteDiscriminatorValue(writer, runtimeType, options);
                """;
            return $$"""
                            if (value is null)
                            {
                                writer.WriteNullValue();
                                return;
                            }
                {{payloadSelection}}
                            using JsonDocument payloadDocument = JsonDocument.Parse(payload);
                            string discriminatorName = {{SymbolDisplay.FormatLiteral(info.DiscriminatorPropertyName!, quote: true)}};
                            if (options.PropertyNamingPolicy != null)
                            {
                                discriminatorName = options.PropertyNamingPolicy.ConvertName(discriminatorName);
                            }
                            writer.WriteStartObject();
                {{order}}
                            writer.WriteEndObject();
                """;
        }

        private static string EmitReadMethod(BaseTypeInfo info)
        {
            return $$"""
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
                            if (target == typeof({{info.FullyQualifiedName}}))
                            {
                                return DeserializeBase(root, options);
                            }
                            return ({{info.FullyQualifiedName}}?)JsonSerializer.Deserialize(root.GetRawText(), options.GetTypeInfo(target));
                """;
        }

        private static string EmitSelectTypeMethod(BaseTypeInfo info)
        {
            return info.IsValueMode ? EmitValueModeSelectType(info) : EmitPresenceModeSelectType(info);
        }

        private static string EmitValueModeSelectType(BaseTypeInfo info)
        {
            string? nullCases = null;
            List<SubtypeRegistration> nulls = [.. info.Subtypes.Where(s => s.DiscriminatorKind == "null")];
            if (nulls.Count > 0)
            {
                // dedupe so consecutive null registrations do not emit unreachable returns
                nullCases = "                    " + string.Join("\n" + "                    ",
                    nulls.Select(r => r.FullyQualifiedName).Distinct()
                        .Select(t => $"return typeof({t});"));
            }

            List<SubtypeRegistration> strings = [.. info.Subtypes.Where(s => s.DiscriminatorKind == "string")];
            List<SubtypeRegistration> enums = [.. info.Subtypes.Where(s => s.DiscriminatorKind == "enum")];
            string? stringCases = null;
            if (strings.Count + enums.Count > 0)
            {
                IEnumerable<string> cases = strings.Select(r => $"case {r.DiscriminatorLiteral}: return typeof({r.FullyQualifiedName});")
                    .Concat(enums.Select(r => $"case {SymbolDisplay.FormatLiteral(r.EnumMemberName!, quote: true)}: return typeof({r.FullyQualifiedName});"));
                stringCases = "                        " + string.Join("\n" + "                        ", cases);
            }

            List<SubtypeRegistration> ints = [.. info.Subtypes.Where(s => s.DiscriminatorKind == "int")];
            string? numberCases = null;
            if (ints.Count + enums.Count > 0)
            {
                IEnumerable<string> cases = ints.Select(r => $"case {SymbolDisplay.FormatLiteral(r.DiscriminatorLiteral, quote: true)}: return typeof({r.FullyQualifiedName});")
                    .Concat(enums.Select(r => $"case {SymbolDisplay.FormatLiteral(r.EnumUnderlyingValue!, quote: true)}: return typeof({r.FullyQualifiedName});"));
                numberCases = "                        " + string.Join("\n" + "                        ", cases);
            }

            string nullBlock = nullCases == null ? "" : $$"""
                                                              if (discriminator.ValueKind == JsonValueKind.Null)
                                                              {
                                                          {{nullCases}}
                                                              }
                                                          """;
            string stringBlock = stringCases == null ? "" : $$"""
                                                                  if (discriminator.ValueKind == JsonValueKind.String)
                                                                  {
                                                                      switch (discriminator.GetString())
                                                                      {
                                                              {{stringCases}}
                                                                      }
                                                                  }
                                                              """;
            string numberBlock = numberCases == null ? "" : $$"""
                                                                  if (discriminator.ValueKind == JsonValueKind.Number)
                                                                  {
                                                                      switch (discriminator.GetRawText())
                                                                      {
                                                              {{numberCases}}
                                                                      }
                                                                  }
                                                              """;

            return $$"""
                    if (TryGetValueInJson(root, {{SymbolDisplay.FormatLiteral(info.DiscriminatorPropertyName!, quote: true)}}, options, out JsonElement discriminator))
                    {
                {{nullBlock}}
                {{stringBlock}}
                {{numberBlock}}
                        if (TryGetDynamicType(discriminator, out Type? dynamicType))
                        {
                            return dynamicType!;
                        }
                        if (CustomTypeNameResolver is not null)
                        {
                            Type? customType = CustomTypeNameResolver(GetDiscriminatorKey(discriminator));
                            if (customType != null)
                            {
                                return customType;
                            }
                        }
                        return typeof({{info.FallbackType}});
                    }
                    return typeof({{info.FallbackType}});
                """;
        }

        private static string EmitPresenceModeSelectType(BaseTypeInfo info)
        {
            List<string> checks = [];
            foreach (PropertyPresenceRegistration reg in info.PropertyPresences)
            {
                checks.Add(reg.StopLookupOnMatch
                    ? $$"""
                        if (root.TryGetProperty({{SymbolDisplay.FormatLiteral(reg.PropertyName, quote: true)}}, out _))
                        {
                            return typeof({{reg.FullyQualifiedName}});
                        }
                        """
                    : $$"""
                        if (root.TryGetProperty({{SymbolDisplay.FormatLiteral(reg.PropertyName, quote: true)}}, out _))
                        {
                            matches.Add(typeof({{reg.FullyQualifiedName}}));
                        }
                        """);
            }

            string presenceChecks = string.Join("\n", checks);
            return $$"""
                    System.Collections.Generic.HashSet<Type> matches = new System.Collections.Generic.HashSet<Type>();
                {{presenceChecks}}
                    if (matches.Count == 1)
                    {
                        return matches.First();
                    }
                    if (matches.Count > 1)
                    {
                        throw new JsonException("Ambiguous type resolution, expected only one type but got: " + string.Join(", ", matches.Select(t => t.FullName)));
                    }
                    return typeof({{info.FallbackType}});
                """;
        }

        private static string EmitDiscriminatorWriter(BaseTypeInfo info)
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
                dictionaryEntries.Add($$"""
                            [typeof({{reg.FullyQualifiedName}})] = static (writer, options) => {{value.TrimEnd(';')}}
                """);
            }

            return $$"""
                    private static readonly System.Collections.Generic.Dictionary<System.Type, System.Action<Utf8JsonWriter, JsonSerializerOptions>> DiscriminatorWriters = new System.Collections.Generic.Dictionary<System.Type, System.Action<Utf8JsonWriter, JsonSerializerOptions>>
                    {
                {{string.Join(",\n", dictionaryEntries)}}
                    };

                    private static bool IsRegistered(Type runtimeType)
                    {
                        return DiscriminatorWriters.ContainsKey(runtimeType);
                    }

                    private static void WriteDiscriminatorValue(Utf8JsonWriter writer, Type runtimeType, JsonSerializerOptions options)
                    {
                        if (DiscriminatorWriters.TryGetValue(runtimeType, out System.Action<Utf8JsonWriter, JsonSerializerOptions>? write))
                        {
                            write(writer, options);
                            return;
                        }
                        throw new JsonException("Impossible to serialize type: " + runtimeType.FullName + " because there is no registered mapping for the discriminator property");
                    }

                    public readonly System.Collections.Concurrent.ConcurrentDictionary<object, Type> DynamicSubtypes = new System.Collections.Concurrent.ConcurrentDictionary<object, Type>();
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

                    private static object? GetDiscriminatorKey(JsonElement discriminator)
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

                    private bool TryGetDynamicType(JsonElement discriminator, out Type? dynamicType)
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

                    private static bool TryWriteNestedObject(Utf8JsonWriter writer, {{info.FullyQualifiedName}} value, Type runtimeType, JsonSerializerOptions options)
                    {
                {{EmitNestedCases(info)}}
                        return false;
                    }

                    private bool TryWriteDynamic(Utf8JsonWriter writer, {{info.FullyQualifiedName}} value, Type runtimeType, JsonSerializerOptions options)
                    {
                        if (_dynamicReverse.TryGetValue(runtimeType, out object? dynamicDiscriminator))
                        {
                            writer.WriteStartObject();
                            string dynamicDiscriminatorName = {{SymbolDisplay.FormatLiteral(info.DiscriminatorPropertyName!, quote: true)}};
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
                """;
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
                    discLines.Add($"                        writer.WritePropertyName({SymbolDisplay.FormatLiteral(entry.DiscriminatorName, quote: true)});");
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

        private static string EmitBaseHelpers(BaseTypeInfo info)
        {
            List<string> writeProperties = [];
            foreach (BaseProperty prop in info.Properties)
            {
                if (!prop.HasGetter)
                {
                    continue;
                }

                string applyPolicy = prop.HasCustomName ? "false" : "true";
                writeProperties.Add($$"""
                        string name{{prop.Name}} = {{SymbolDisplay.FormatLiteral(prop.JsonName, quote: true)}};
                        if (options.PropertyNamingPolicy != null && {{applyPolicy}})
                        {
                            name{{prop.Name}} = options.PropertyNamingPolicy.ConvertName(name{{prop.Name}});
                        }
                        writer.WritePropertyName(name{{prop.Name}});
                        JsonSerializer.Serialize(writer, value.{{prop.Name}}, options.GetTypeInfo(typeof({{prop.PropertyTypeName}})));
                    """);
            }

            List<string> readProperties = [];
            foreach (BaseProperty prop in info.Properties)
            {
                if (!prop.HasSetter)
                {
                    continue;
                }

                readProperties.Add($$"""
                        if (TryGetProperty(root, {{SymbolDisplay.FormatLiteral(prop.JsonName, quote: true)}}, options, out JsonElement {{prop.Name}}Value))
                        {
                            instance.{{prop.Name}} = ({{prop.PropertyTypeName}})JsonSerializer.Deserialize({{prop.Name}}Value.GetRawText(), options.GetTypeInfo(typeof({{prop.PropertyTypeName}})))!;
                        }
                    """);
            }

            string deserializeBase;
            if (info.BaseIsAbstractOrInterface)
            {
                deserializeBase = $$"""
                    private static {{info.FullyQualifiedName}} DeserializeBase(JsonElement root, JsonSerializerOptions options)
                    {
                        throw new JsonException("Could not create an instance of type {{info.FullyQualifiedName}}. Type is an interface or abstract class and cannot be instantiated.");
                    }
                """;
            }
            else if (!info.BaseHasParameterlessConstructor)
            {
                deserializeBase = $$"""
                    private static {{info.FullyQualifiedName}} DeserializeBase(JsonElement root, JsonSerializerOptions options)
                    {
                        throw new JsonException("Could not create an instance of type {{info.FullyQualifiedName}}: a parameterless constructor is required to fall back to the base type.");
                    }
                """;
            }
            else
            {
                deserializeBase = $$"""
                    private static {{info.FullyQualifiedName}} DeserializeBase(JsonElement root, JsonSerializerOptions options)
                    {
                        {{info.FullyQualifiedName}} instance = new {{info.FullyQualifiedName}}();
                {{string.Join("\n", readProperties)}}
                        return instance;
                    }
                """;
            }

            return $$"""
                    private static string SerializeBasePayload({{info.FullyQualifiedName}} value, JsonSerializerOptions options)
                    {
                        using System.IO.MemoryStream stream = new System.IO.MemoryStream();
                        using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
                        {
                            WriteBaseObject(writer, value, options);
                        }
                        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
                    }

                    private static void WriteBaseObject(Utf8JsonWriter writer, {{info.FullyQualifiedName}} value, JsonSerializerOptions options)
                    {
                        writer.WriteStartObject();
                {{string.Join("\n", writeProperties)}}
                        writer.WriteEndObject();
                    }

                {{deserializeBase}}

                    private static bool TryGetProperty(JsonElement root, string name, JsonSerializerOptions options, out JsonElement value)
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
                            foreach (JsonProperty property in root.EnumerateObject())
                            {
                                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                                {
                                    value = property.Value;
                                    return true;
                                }
                            }
                        }
                        return false;
                    }

                    private static bool TryGetValueInJson(JsonElement root, string propertyName, JsonSerializerOptions options, out JsonElement value)
                    {
                        if (TryGetProperty(root, propertyName, options, out value))
                        {
                            return true;
                        }
                        if (propertyName.IndexOf('.') >= 0)
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
                """;
        }
    }
}
