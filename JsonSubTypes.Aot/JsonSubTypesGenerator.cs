using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private static readonly DiagnosticDescriptor UnsupportedDiscriminator =
            new DiagnosticDescriptor(
                DiagnosticId,
                "Discriminator value not supported by JsonSubTypes.Aot",
                "The discriminator value of type '{0}' on subtype '{1}' is not supported by JsonSubTypes.Aot; only string and int constants are. The subtype is not generated. Use the runtime converter for this hierarchy.",
                "JsonSubTypes.Aot",
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<INamedTypeSymbol> baseTypes = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    $"{AttributeNamespace}.{JsonSubTypesAotConverterAttributeName}",
                    predicate: static (node, _) => true,
                    transform: static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);

            context.RegisterSourceOutput(baseTypes.Collect(), static (spc, types) =>
            {
                if (types.Length == 0)
                {
                    return;
                }

                HashSet<string> visitedBases = new HashSet<string>();
                List<BaseTypeInfo> bases = new List<BaseTypeInfo>();
                foreach (INamedTypeSymbol baseType in types.Distinct(SymbolEqualityComparer.Default))
                {
                    if (visitedBases.Add(baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
                    {
                        bases.Add(BuildBaseTypeInfo(baseType, spc));
                    }
                }

            spc.AddSource("JsonSubTypesAotConverters.g.cs",
                    SourceText.From(EmitRegistry(bases), Encoding.UTF8));

            foreach (BaseTypeInfo baseInfo in bases)
            {
                if (baseInfo.Subtypes.Count == 0 && !baseInfo.HasPropertyPresence)
                {
                    continue;
                }

                spc.AddSource($"{baseInfo.TypeName}JsonSubTypesConverter.g.cs",
                    SourceText.From(EmitConverter(baseInfo), Encoding.UTF8));
            }
            });
        }

        private sealed class BaseTypeInfo
        {
            public string Namespace { get; set; } = "";
            public string TypeName { get; set; } = "";
            public string FullyQualifiedName { get; set; } = "";
            public string? DiscriminatorPropertyName { get; set; }
            public bool HasPropertyPresence { get; set; }
            public List<SubtypeRegistration> Subtypes { get; } = new List<SubtypeRegistration>();
            public List<PropertyPresenceRegistration> PropertyPresences { get; } = new List<PropertyPresenceRegistration>();
            public string? FallbackTypeName { get; set; }
            public string? FallbackFullyQualifiedName { get; set; }
            public List<ITypeSymbol> AllTypes { get; } = new List<ITypeSymbol>();
            public List<BaseProperty> Properties { get; } = new List<BaseProperty>();
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

        private sealed class SubtypeRegistration
        {
            public string TypeName { get; set; } = "";
            public string FullyQualifiedName { get; set; } = "";
            public string DiscriminatorKind { get; set; } = ""; // "string" | "int"
            public string DiscriminatorLiteral { get; set; } = "";
            public bool IsBaseType { get; set; }
        }

        private sealed class PropertyPresenceRegistration
        {
            public string TypeName { get; set; } = "";
            public string FullyQualifiedName { get; set; } = "";
            public string PropertyName { get; set; } = "";
            public bool StopLookupOnMatch { get; set; }
        }

        private static BaseTypeInfo BuildBaseTypeInfo(INamedTypeSymbol baseType, SourceProductionContext spc)
        {
            BaseTypeInfo info = new BaseTypeInfo();
            info.FullyQualifiedName = baseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            info.TypeName = baseType.Name;
            info.Namespace = baseType.ContainingNamespace.IsGlobalNamespace
                ? ""
                : baseType.ContainingNamespace.ToDisplayString();
            info.AllTypes.Add(baseType);

            string? discriminator = null;
            foreach (AttributeData attr in baseType.GetAttributes())
            {
                if (attr.AttributeClass?.Name != JsonSubTypesAotConverterAttributeName)
                {
                    continue;
                }

                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string discName)
                {
                    discriminator = discName;
                }
            }

            info.DiscriminatorPropertyName = discriminator;

            foreach (AttributeData attr in baseType.GetAttributes())
            {
                switch (attr.AttributeClass?.Name)
                {
                    case KnownSubTypeAttributeName:
                    {
                        if (attr.ConstructorArguments.Length != 2)
                        {
                            break;
                        }

                        ITypeSymbol? subtype = attr.ConstructorArguments[0].Value as ITypeSymbol;
                        object? value = attr.ConstructorArguments[1].Value;
                        if (subtype == null)
                        {
                            break;
                        }

                        info.AllTypes.Add(subtype);

                        if (discriminator == null)
                        {
                            continue; // presence mode ignores value registrations
                        }

                        SubtypeRegistration registration = new SubtypeRegistration
                        {
                            TypeName = subtype.Name,
                            FullyQualifiedName = subtype.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            IsBaseType = SymbolEqualityComparer.Default.Equals(subtype, baseType)
                        };
                        string discKind;
                        string discLiteral;
                        if (TryGetConstant(value, out discKind, out discLiteral))
                        {
                            registration.DiscriminatorKind = discKind;
                            registration.DiscriminatorLiteral = discLiteral;
                            info.Subtypes.Add(registration);
                        }
                        else
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(UnsupportedDiscriminator,
                                attr.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                                value?.GetType().Name ?? "null",
                                subtype.Name));
                        }

                        break;
                    }
                    case KnownSubTypeWithPropertyAttributeName:
                    {
                        if (attr.ConstructorArguments.Length != 2)
                        {
                            break;
                        }

                        ITypeSymbol? subtype = attr.ConstructorArguments[0].Value as ITypeSymbol;
                        string? propertyName = attr.ConstructorArguments[1].Value as string;
                        if (subtype == null || propertyName == null)
                        {
                            break;
                        }

                        bool stopLookup = false;
                        foreach (KeyValuePair<string, TypedConstant> namedArg in attr.NamedArguments)
                        {
                            if (namedArg.Key == "StopLookupOnMatch" && namedArg.Value.Value is bool b)
                            {
                                stopLookup = b;
                            }
                        }

                        info.AllTypes.Add(subtype);
                        info.HasPropertyPresence = true;
                        info.PropertyPresences.Add(new PropertyPresenceRegistration
                        {
                            TypeName = subtype.Name,
                            FullyQualifiedName = subtype.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            PropertyName = propertyName,
                            StopLookupOnMatch = stopLookup
                        });
                        break;
                    }
                    case FallBackSubTypeAttributeName:
                    {
                        if (attr.ConstructorArguments.Length != 1)
                        {
                            break;
                        }

                        ITypeSymbol? fallback = attr.ConstructorArguments[0].Value as ITypeSymbol;
                        if (fallback != null)
                        {
                            info.AllTypes.Add(fallback);
                            info.FallbackTypeName = fallback.Name;
                            info.FallbackFullyQualifiedName = fallback.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        }

                        break;
                    }
                }
            }

            foreach (ISymbol member in baseType.GetMembers())
            {
                if (member is not IPropertySymbol property ||
                    property.IsStatic ||
                    property.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (property.GetMethod == null || property.SetMethod == null)
                {
                    continue;
                }

                bool ignored = false;
                string? jsonName = null;
                foreach (AttributeData attr in property.GetAttributes())
                {
                    if (attr.AttributeClass?.Name == "JsonIgnoreAttribute")
                    {
                        ignored = true;
                    }
                    else if (attr.AttributeClass?.Name == "JsonPropertyNameAttribute" &&
                             attr.ConstructorArguments.Length > 0 &&
                             attr.ConstructorArguments[0].Value is string name)
                    {
                        jsonName = name;
                    }
                }

                if (ignored)
                {
                    continue;
                }

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

            return info;
        }

        private static bool TryGetConstant(object? value, out string kind, out string literal)
        {
            kind = "";
            literal = "";
            switch (value)
            {
                case string s:
                    kind = "string";
                    literal = SymbolDisplay.FormatLiteral(s, quote: true);
                    return true;
                case int i:
                    kind = "int";
                    literal = i.ToString();
                    return true;
                default:
                    return false;
            }
        }

        private static string EmitRegistry(List<BaseTypeInfo> bases)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("namespace JsonSubTypes.Aot.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>Generated by JsonSubTypes.Aot. Shared converter instances to add to JsonSerializerOptions.Converters.</summary>");
            sb.AppendLine("    [global::System.CodeDom.Compiler.GeneratedCode(\"JsonSubTypes.Aot\", \"1.0.0\")]");
            sb.AppendLine("    public static class JsonSubTypesAotConverters");
            sb.AppendLine("    {");
            foreach (BaseTypeInfo baseInfo in bases)
            {
                if (baseInfo.Subtypes.Count == 0 && !baseInfo.HasPropertyPresence)
                {
                    continue;
                }

                sb.AppendLine($"        public static readonly {baseInfo.TypeName}JsonSubTypesConverter {baseInfo.TypeName} = new {baseInfo.TypeName}JsonSubTypesConverter();");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string EmitConverter(BaseTypeInfo info)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using System.Text.Json;");
            sb.AppendLine("using System.Text.Json.Serialization;");
            sb.AppendLine();
            sb.AppendLine("namespace JsonSubTypes.Aot.Generated");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>Generated by JsonSubTypes.Aot. Compiled converter for {info.FullyQualifiedName}.</summary>");
            sb.AppendLine("    [global::System.CodeDom.Compiler.GeneratedCode(\"JsonSubTypes.Aot\", \"1.0.0\")]");
            sb.AppendLine($"    public sealed class {info.TypeName}JsonSubTypesConverter : JsonConverter<{info.FullyQualifiedName}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof({info.FullyQualifiedName});");
            sb.AppendLine();
            sb.AppendLine("        public override void Write(Utf8JsonWriter writer, " + info.FullyQualifiedName + " value, JsonSerializerOptions options)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (value is null)");
            sb.AppendLine("            {");
            sb.AppendLine("                writer.WriteNullValue();");
            sb.AppendLine("                return;");
            sb.AppendLine("            }");
            sb.AppendLine("            Type runtimeType = value.GetType();");
            if (info.DiscriminatorPropertyName != null)
            {
                EmitValueModeWrite(sb, info);
            }
            else
            {
                sb.AppendLine($"            if (runtimeType == typeof({info.FullyQualifiedName}))");
                sb.AppendLine("            {");
                sb.AppendLine("                WriteBaseObject(writer, (" + info.FullyQualifiedName + ")value, options);");
                sb.AppendLine("                return;");
                sb.AppendLine("            }");
                sb.AppendLine("            JsonSerializer.Serialize(writer, value, options.TypeInfoResolver!.GetTypeInfo(runtimeType, options)!);");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override " + info.FullyQualifiedName + "? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)");
            sb.AppendLine("        {");
            sb.AppendLine("            using JsonDocument document = JsonDocument.ParseValue(ref reader);");
            sb.AppendLine("            JsonElement root = document.RootElement;");
            sb.AppendLine("            Type target = SelectType(root);");
            sb.AppendLine($"            if (target == typeof({info.FullyQualifiedName}))");
            sb.AppendLine("            {");
            sb.AppendLine("                return DeserializeBase(root, options);");
            sb.AppendLine("            }");
            sb.AppendLine("            return (" + info.FullyQualifiedName + "?)JsonSerializer.Deserialize(root.GetRawText(), options.TypeInfoResolver!.GetTypeInfo(target, options)!);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static Type SelectType(JsonElement root)");
            sb.AppendLine("        {");
            if (info.DiscriminatorPropertyName != null)
            {
                EmitValueModeSelectType(sb, info);
            }
            else
            {
                EmitPresenceModeSelectType(sb, info);
            }
            sb.AppendLine("        }");
            EmitBaseHelpers(sb, info);
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void EmitBaseHelpers(StringBuilder sb, BaseTypeInfo info)
        {
            sb.AppendLine($"        private static string SerializeBasePayload({info.FullyQualifiedName} value, JsonSerializerOptions options)");
            sb.AppendLine("        {");
            sb.AppendLine("            using System.IO.MemoryStream stream = new System.IO.MemoryStream();");
            sb.AppendLine("            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))");
            sb.AppendLine("            {");
            sb.AppendLine("                WriteBaseObject(writer, value, options);");
            sb.AppendLine("            }");
            sb.AppendLine("            return System.Text.Encoding.UTF8.GetString(stream.ToArray());");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        private static void WriteBaseObject(Utf8JsonWriter writer, {info.FullyQualifiedName} value, JsonSerializerOptions options)");
            sb.AppendLine("        {");
            sb.AppendLine("            writer.WriteStartObject();");
            foreach (BaseProperty prop in info.Properties)
            {
                if (!prop.HasGetter)
                {
                    continue;
                }

                string applyPolicy = prop.HasCustomName ? "false" : "true";
                sb.AppendLine($"            string name{prop.Name} = {SymbolDisplay.FormatLiteral(prop.JsonName, quote: true)};");
                sb.AppendLine($"            if (options.PropertyNamingPolicy != null && {applyPolicy})");
                sb.AppendLine("            {");
                sb.AppendLine($"                name{prop.Name} = options.PropertyNamingPolicy.ConvertName(name{prop.Name});");
                sb.AppendLine("            }");
                sb.AppendLine($"            writer.WritePropertyName(name{prop.Name});");
                sb.AppendLine($"            JsonSerializer.Serialize(writer, value.{prop.Name}, options.TypeInfoResolver!.GetTypeInfo(typeof({prop.PropertyTypeName}), options)!);");
            }
            sb.AppendLine("            writer.WriteEndObject();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        private static {info.FullyQualifiedName} DeserializeBase(JsonElement root, JsonSerializerOptions options)");
            sb.AppendLine("        {");
            sb.AppendLine($"            {info.FullyQualifiedName} instance = new {info.FullyQualifiedName}();");
            foreach (BaseProperty prop in info.Properties)
            {
                if (!prop.HasSetter)
                {
                    continue;
                }

                string jsonNameLiteral = SymbolDisplay.FormatLiteral(prop.JsonName, quote: true);
                sb.AppendLine($"            if (TryGetProperty(root, {jsonNameLiteral}, options, out JsonElement {prop.Name}Value))");
                sb.AppendLine("            {");
                sb.AppendLine($"                instance.{prop.Name} = ({prop.PropertyTypeName})JsonSerializer.Deserialize({prop.Name}Value.GetRawText(), options.TypeInfoResolver!.GetTypeInfo(typeof({prop.PropertyTypeName}), options)!)!;");
                sb.AppendLine("            }");
            }
            sb.AppendLine("            return instance;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static bool TryGetProperty(JsonElement root, string name, JsonSerializerOptions options, out JsonElement value)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (root.TryGetProperty(name, out value))");
            sb.AppendLine("            {");
            sb.AppendLine("                return true;");
            sb.AppendLine("            }");
            sb.AppendLine("            string? convertedName = options.PropertyNamingPolicy?.ConvertName(name);");
            sb.AppendLine("            if (convertedName != null && convertedName != name && root.TryGetProperty(convertedName, out value))");
            sb.AppendLine("            {");
            sb.AppendLine("                return true;");
            sb.AppendLine("            }");
            sb.AppendLine("            if (options.PropertyNameCaseInsensitive)");
            sb.AppendLine("            {");
            sb.AppendLine("                foreach (JsonProperty property in root.EnumerateObject())");
            sb.AppendLine("                {");
            sb.AppendLine("                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))");
            sb.AppendLine("                    {");
            sb.AppendLine("                        value = property.Value;");
            sb.AppendLine("                        return true;");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
        }

        private static void EmitValueModeWrite(StringBuilder sb, BaseTypeInfo info)
        {
            string discName = info.DiscriminatorPropertyName!;
            sb.AppendLine($"            string payload = runtimeType == typeof({info.FullyQualifiedName})");
            sb.AppendLine($"                ? SerializeBasePayload(({info.FullyQualifiedName})value, options)");
            sb.AppendLine("                : JsonSerializer.Serialize(value, options.TypeInfoResolver!.GetTypeInfo(runtimeType, options)!);");
            sb.AppendLine("            using JsonDocument payloadDocument = JsonDocument.Parse(payload);");
            sb.AppendLine("            writer.WriteStartObject();");
            sb.AppendLine($"            writer.WritePropertyName({SymbolDisplay.FormatLiteral(discName, quote: true)});");
            sb.AppendLine("            switch (runtimeType)");
            sb.AppendLine("            {");
            foreach (SubtypeRegistration reg in info.Subtypes)
            {
                sb.AppendLine($"                case var _ when runtimeType == typeof({reg.FullyQualifiedName}):");
                if (reg.DiscriminatorKind == "string")
                {
                    sb.AppendLine($"                    writer.WriteStringValue({reg.DiscriminatorLiteral});");
                }
                else
                {
                    sb.AppendLine($"                    writer.WriteNumberValue({reg.DiscriminatorLiteral});");
                }
                sb.AppendLine("                    break;");
            }
            sb.AppendLine("                default:");
            sb.AppendLine("                    throw new JsonException(\"Impossible to serialize type: \" + runtimeType.FullName + \" because there is no registered mapping for the discriminator property\");");
            sb.AppendLine("            }");
            sb.AppendLine("            foreach (JsonProperty property in payloadDocument.RootElement.EnumerateObject())");
            sb.AppendLine("            {");
            sb.AppendLine($"                if (!property.NameEquals({SymbolDisplay.FormatLiteral(discName, quote: true)}))");
            sb.AppendLine("                {");
            sb.AppendLine("                    property.WriteTo(writer);");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("            writer.WriteEndObject();");
        }

        private static void EmitValueModeSelectType(StringBuilder sb, BaseTypeInfo info)
        {
            string discName = info.DiscriminatorPropertyName!;
            string fallback = info.FallbackFullyQualifiedName ?? info.FullyQualifiedName;
            sb.AppendLine($"            if (root.TryGetProperty({SymbolDisplay.FormatLiteral(discName, quote: true)}, out JsonElement discriminator))");
            sb.AppendLine("            {");
            var strings = info.Subtypes.Where(s => s.DiscriminatorKind == "string").ToList();
            var ints = info.Subtypes.Where(s => s.DiscriminatorKind == "int").ToList();
            if (strings.Count > 0)
            {
                sb.AppendLine("                if (discriminator.ValueKind == JsonValueKind.String)");
                sb.AppendLine("                {");
                sb.AppendLine("                    switch (discriminator.GetString())");
                sb.AppendLine("                    {");
                foreach (SubtypeRegistration reg in strings)
                {
                    sb.AppendLine($"                        case {reg.DiscriminatorLiteral}: return typeof({reg.FullyQualifiedName});");
                }
                sb.AppendLine("                    }");
                sb.AppendLine("                }");
            }
            if (ints.Count > 0)
            {
                sb.AppendLine("                if (discriminator.ValueKind == JsonValueKind.Number)");
                sb.AppendLine("                {");
                sb.AppendLine("                    switch (discriminator.GetRawText())");
                sb.AppendLine("                    {");
                foreach (SubtypeRegistration reg in ints)
                {
                    sb.AppendLine($"                        case {SymbolDisplay.FormatLiteral(reg.DiscriminatorLiteral, quote: true)}: return typeof({reg.FullyQualifiedName});");
                }
                sb.AppendLine("                    }");
                sb.AppendLine("                }");
            }
            sb.AppendLine($"                return typeof({fallback});");
            sb.AppendLine("            }");
            sb.AppendLine($"            return typeof({fallback});");
        }

        private static void EmitPresenceModeSelectType(StringBuilder sb, BaseTypeInfo info)
        {
            string fallback = info.FallbackFullyQualifiedName ?? info.FullyQualifiedName;
            sb.AppendLine("            System.Collections.Generic.List<Type> matches = new System.Collections.Generic.List<Type>();");
            foreach (PropertyPresenceRegistration reg in info.PropertyPresences)
            {
                sb.AppendLine($"            if (root.TryGetProperty({SymbolDisplay.FormatLiteral(reg.PropertyName, quote: true)}, out _))");
                sb.AppendLine("            {");
                if (reg.StopLookupOnMatch)
                {
                    sb.AppendLine($"                return typeof({reg.FullyQualifiedName});");
                }
                else
                {
                    sb.AppendLine($"                matches.Add(typeof({reg.FullyQualifiedName}));");
                }
                sb.AppendLine("            }");
            }
            sb.AppendLine("            if (matches.Count == 1)");
            sb.AppendLine("            {");
            sb.AppendLine("                return matches[0];");
            sb.AppendLine("            }");
            sb.AppendLine("            if (matches.Count > 1)");
            sb.AppendLine("            {");
            sb.AppendLine("                throw new JsonException(\"Ambiguous type resolution, expected only one type but got: \" + string.Join(\", \", matches.Select(t => t.FullName)));");
            sb.AppendLine("            }");
            sb.AppendLine($"            return typeof({fallback});");
        }
    }
}
