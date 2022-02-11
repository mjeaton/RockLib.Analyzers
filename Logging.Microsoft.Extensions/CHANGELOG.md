# RockLib.Logging.Microsoft.Extensions.Analyzers Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased
	
#### Added
- Added `.editorconfig` and `Directory.Build.props` files to ensure consistency.

#### Changed
- Supported targets: net6.0, netcoreapp3.1, and net48 (.NET Standard 2.0 is still used for analyzers and code fixes)
- As the package now uses nullable reference types, some method parameters now specify if they can accept nullable values.

## 1.0.1 - 2021-08-12

#### Changed

- Changes "Quicken Loans" to "Rocket Mortgage".

## 1.0.0 - 2021-07-21

#### Added

- Adds analyzer and codefix for RockLib0002: Logger should be synchronous
- Adds analyzer for RockLib0003: RockLibLoggerProvider has missing logger
