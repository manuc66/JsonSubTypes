# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### JsonSubTypes.Text.Json
#### Changed
- Replaced the global `JsonSubTypesTypeResolution.AddAssembly` registry with a declarative `[JsonSubTypesTypeResolution("AssemblyName")]` attribute on the base type. Resolution is now per-type instead of process-wide, so it no longer leaks across serialization profiles. The attribute takes an assembly name, keeping the base type free of a compile-time reference to the plugin.
- Renamed `FallBackSubTypeAttribute` to `FallbackSubTypeAttribute` and `FallBackToNearestAncestor()` to `FallbackToNearestAncestor()` for consistent capitalization. The `FallBack*` names still work in `JsonSubTypes` (Newtonsoft), which keeps its historical API.

### JsonSubTypes
#### Fixed
- Deserialization with an open generic base type (e.g. `Base<>`) now closes the generic subtype correctly (e.g. `Nested1<int>` for `Base<int>`) instead of failing. #177
- Errors and exceptions raised while deserializing a subtype now carry the fully qualified JSON path (e.g. `Property2.Value` instead of `Value`), matching stock Newtonsoft.Json error handling. #182

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




