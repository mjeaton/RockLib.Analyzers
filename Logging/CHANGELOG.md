# RockLib.Logging.Analyzers Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased
	
#### Added
- Added `.editorconfig` and `Directory.Build.props` files to ensure consistency.

#### Changed
- Supported targets: net6.0, netcoreapp3.1, and net48.
- As the package now uses nullable reference types, some method parameters now specify if they can accept nullable values.

## 1.0.3 - 2021-08-12

#### Changed

- Changes "Quicken Loans" to "Rocket Mortgage".

## 1.0.2 - 2021-07-21

#### Changed

- RockLib0000: Include base classes when identifying public properties.
- RockLib0000: Look for calls to `SafeToLogAttribute.Decorate` and `NotSafeToLogAttribute.Decorate` when determining if a type or property has been marked as safe to log.

#### Added

- Analyzer for RockLib0006: Caught exception should be logged.
- Analyzer for RockLib0007: Unexpected extended properties object.

## 1.0.1 - 2021-07-13

#### Added

- Adds analyzer and codefix for RockLib0005: No log level specified

#### Fixed

- Be able to analyze extended properties when defined as a parameter, not just as a local variable

## 1.0.0 - 2021-06-09

#### Added

- Adds analyzer for RockLib0000: Extended property not marked as safe to log
- Adds analyzer and codefix for RockLib0001: Use sanitizing logging method
