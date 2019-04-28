# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Support for multiple discriminators on single type #66
- Support for per inheritance level discriminators #60

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




