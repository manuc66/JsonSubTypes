# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### JsonSubTypes.Text.Json
#### Added
- New `JsonSubTypeConverterAttribute` convenience constructors that close the generic `JsonSubtypes<T>` converter over the annotated type, so the base type does not need to be repeated: `[JsonSubTypeConverter("Kind")]` instead of `[JsonSubTypeConverter(typeof(JsonSubtypes<Animal>), "Kind")]`.
#### Changed
- Replaced the global `JsonSubTypesTypeResolution.AddAssembly` registry with a declarative `[KnownSubTypeOtherAssembly("AssemblyName")]` attribute on the base type. Resolution is now per-type instead of process-wide, so it no longer leaks across serialization profiles. The attribute takes an assembly name, keeping the base type free of a compile-time reference to the plugin.
- Renamed `FallBackSubTypeAttribute` to `FallbackSubTypeAttribute` and `FallBackToNearestAncestor()` to `FallbackToNearestAncestor()` for consistent capitalization. The `FallBack*` names still work in `JsonSubTypes` (Newtonsoft), which keeps its historical API.
#### Fixed
- The attribute-based converter now writes the discriminator on serialization, as documented: the attribute's `CreateConverter` override was previously bypassed by `System.Text.Json` (the converter was built through its parameterless constructor), so the discriminator was only read, never written. The attribute now routes through `CreateConverter`, which also activates the discriminator-injection write path for registered subtypes.

## [2.2.0] - 2026-10-07

### JsonSubTypes
#### Added
- `JsonSubtypesConverter` exposes `protected static bool IsClosedGenericFormOf(Type objectType, Type genericType)`, so custom converters built on the public base can reuse the closed-generic base matching.

#### Fixed
- Deserialization with an open generic base type (e.g. `Base<>`) now closes the generic subtype correctly (e.g. `Nested1<int>` for `Base<int>`) instead of failing. #177
- Errors and exceptions raised while deserializing a subtype now carry the fully qualified JSON path (e.g. `Property2.Value` instead of `Value`), matching stock Newtonsoft.Json error handling. #182

#### Changed
- Faster deserialization and serialization, output unchanged: the attribute-derived subtype mapping and property-presence list are cached per type, single-level type resolution skips the multi-level converter walk, and string/int discriminators are converted directly instead of round-tripping through `JToken.FromObject`/`ToObject` reflection. A custom `JsonConverter` registered on the serializer for the discriminator type still takes the serializer-aware path on both read and write. Measured with BenchmarkDotNet on net10: single deserialize 2.54µs → 2.33µs, collection deserialize 10.21µs → 7.72µs, single serialize 1.54µs → 1.31µs, collection serialize 5.51µs → 4.86µs.
- Generic subtype matching is restricted to the base class hierarchy: `CanConvert` no longer claims types that merely implement an unrelated generic interface of the base type.
- The name-based resolution risk (declared only when no subtype mapping exists) is documented on the converters, so it shows up in the IDE, and in the README security section.

#### Security
- netstandard1.3: `System.Net.Http` and `System.Text.RegularExpressions` are pinned to the patched 4.3.4 and 4.3.1; `NETStandard.Library 1.6.1` otherwise resolves them to the vulnerable 4.3.0 versions.

## [1.0.0-rc.4] - 2026-10-07

### JsonSubTypes.Text.Json
#### Changed
- The converter caches the resolved `IJsonSubtypes` converter list per `JsonSerializerOptions` (frozen on first use) and resolves single-level hierarchies without per-object allocations.
- Deserialization reads the resolved subtype from the already-parsed `JsonElement` instead of re-reading the raw bytes through a second materialization.
- The discriminator write path streams the payload as UTF-8 (`ArrayBufferWriter` + `Utf8JsonWriter`) instead of round-tripping through a UTF-16 string.

#### Security
- The name-based resolution risk (declared only when no subtype mapping exists) is documented on the converter and in the README security section.

## [1.0.0-rc.2] - 2026-10-07

### JsonSubTypes.Text.Json.Aot
#### Changed
- **Breaking (pre-1.0)**: the package is renamed `JsonSubTypes.Aot` → `JsonSubTypes.Text.Json.Aot`: new NuGet package id, new project/namespace, and the generated-code namespace becomes `JsonSubTypes.Text.Json.Aot.Generated` instead of `JsonSubTypes.Aot.Generated`. Update the `PackageReference` and the `using` directives. The previously published `JsonSubTypes.Aot` 1.0.0-rc.1 is superseded and receives no further updates.
- Generated converters get unique names and are emitted without a `.g.cs` suffix, a shared converter base class carries the common skeleton once per compilation, and the value-mode `Write` is hardened (redundant `&& true` removed).
- Generator diagnostics now check the cancellation token before every report, so generation stops promptly.
- Bumped the build-time `Microsoft.CodeAnalysis.CSharp` dependency from 5.6.0 to 5.9.0 (not shipped in the package).

## [1.0.0-rc.3] - 2026-08-12

### JsonSubTypes.Text.Json
#### Added
- New `JsonSubTypesAotConverterAttribute` opting a base type into the `JsonSubTypes.Text.Json.Aot` source generator.

### JsonSubTypes.Text.Json.Aot
#### Added
- New package `JsonSubTypes.Text.Json.Aot` (1.0.0-rc.1): a Roslyn source generator emitting compiled subtype converters. Routing (property presence, fallback, enums, nested hierarchies, dynamic registration) is compiled, so it works in Native AOT / trimmed binaries without reflection.

## [1.0.0-rc.2] - 2026-08-10
### Changed
- Rebuilt with Source Link, deterministic builds and `.snupkg` symbol packages so symbols validate against the published package.

## [1.0.0-rc.1] - 2026-08-10
### Added
- New package `JsonSubTypes.Text.Json` bringing polymorphic subtype serialization to `System.Text.Json` (.NET 8+).
  - Attribute-based and builder-based subtype registration.
  - Discriminator mapping by property presence (`KnownSubTypeWithProperty`).
  - Fallback subtype support (`FallBackSubType`).
  - Opt-in cross-assembly subtype resolution (`JsonSubTypesTypeResolution.AddAssembly`).
  - Comprehensive unit test suite with 129 test cases.

## [2.1.0] - 2026-08-10

### Added
- Make `JsonSubtypesConverter` and `JsonSubtypesByDiscriminatorValueConverter` public and subclassable: custom subtype converters can now be built with a custom discriminator mapping and control over discriminator serialization, instead of being limited to the fluent builder. #138

### Fixed
- Deserializing dates now matches stock Newtonsoft deserialization #166 #167

### Changed
- Bump Newtonsoft.Json dependency to 13.0.4
- Deterministic builds, Source Link and .snupkg symbol packages

## [2.0.1] - 2022-05-09

### Fixed
- Add package description with an included README.md

## [2.0.0] - 2022-05-09

### Changed
- Discriminator property is placed first by default now #46 #149
- Depends on the latest Newtonsoft.Json #131 #148
- Signature of SetFallbackSubtype has been changed to fix a design bug #152 #147

### Added
- Allow to stop searching when a match is found #128 #151

### Fixed
- Fix a DateTime issue introduced in release 1.8.0 #120 #128

## [1.9.0] - 2022-05-09

### Added
- Add version of builder methods with generic types for cleaner syntax. #110
- Support (serializing) sub types with generic type parameters when using JsonSubtypesConverterBuilder #135
- Add cache of type's attributes #119

### Fixed
- Newtonsoft.Json dependency version should be lowest supported, not latest available #101
- Multiple type discriminators in JSON silently passes. #100
- Incorrect handling of datetime field in a sub-type #114
- Too many target framework inside the nuget package #48
- Copy MaxDepth when creating internal JObjectReader #137
- Fix deserialization of hierarchy with multiple levels #118

## [1.8.0] - 2020-09-24

### Added
- Add version of builder methods with generic types for cleaner syntax. #115

### Fixed
- Newtonsoft.Json dependency version should be lowest supported, not latest available #101
- Multiple type discriminators in JSON silently passes. #100
- Incorrect handling of datetime field in a sub-type #114

## [1.7.0] - 2020-03-28

### Added
- Fallback to JSONPath to allow nested field as a deserialization property. #89
- Bump Newtonsoft.Json from 11.0.2 to 12.0.3 #88
- Implements dynamic registration for subtype detection by property presence. #50

### Fixed
- JsonSubtypes does not respect naming strategy for discriminator property value #80
- Fix infinite loop when specifying name of abstract base class as discriminator #83
- Serializing base class with discriminator property results in KeyNotFoundException #79

## [1.6.0] - 2019-06-25
### Added
- Support for multiple discriminators on single type #66
- Support for per inheritance level discriminators #60
- Support specifying a falback sub type if none matched #63
- Provide NuGet package with strong name #75
- Changelog history and documentation arround versionning

## [1.5.2] - 2019-01-19
### Security
- Arbitrary constructor invocation #56

## [1.5.1] - 2018-10-15
### Fixed
- Read.me was imported by the nuget install #51

## [1.5.0] - 2018-08-27
### Added
- Ability to set the discriminator property order to first (see #46)
- Compatibility with JSON.NET 11.0.2 (see #47)

## [1.4.0] - 2018-04-18
### Added
- Support for both camel case and non camel case parameters #31
- Explicit support for netstandard2.0 #34

### Fixed
- Code refactoring to reduce the number of conditional compilation statements #36

## [1.3.1] - 2014-04-12
### Fixed
- fixed exception that was returned instead of thrown #32 

## [1.3.0] - 2018-29-01
### Added
- De-/Serialization for sub-types without "type" property #13
- Option for avoiding mapping on the Parent #26

### Fixed
- Sonar (Coverage) analysis is broken #23

## [1.1.3] - 2017-11-15
### Fixed
- fixed support of framework net40 #21

## [1.1.2] - 2017-11-20
### Fixed
- fix #18 : Deserialisation is not thread safe

## [1.1.1] - 2017-09-22
### Fixed
- fix #11 Nuget packages doesn't work for .Net Framework projects

## [1.1.0] - 2017-09-19
### Added
- Parse string enum values #9.

## [1.0.0] - 2017-07-23
Initial release !




