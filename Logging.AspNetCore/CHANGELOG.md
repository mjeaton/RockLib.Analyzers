# RockLib.Logging.AspNetCore.Analyzers Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 2.0.0 - 2022-02-18
	
#### Added
- Added `.editorconfig` and `Directory.Build.props` files to ensure consistency.

#### Changed
- Supported targets: net6.0, netcoreapp3.1, and net48.
- As the package now uses nullable reference types, some method parameters now specify if they can accept nullable values.

## 1.0.2 - 2021-08-12

#### Changed

- Changes "Quicken Loans" to "Rocket Mortgage".

## 1.0.1 - 2021-07-22

#### Fixed

For RockLib0004 - Add InfoLog attribute:

- Improves the search for controllers & action methods, reducing false positives.
- Don't report when InfoLog is added to controller filters.

## 1.0.0 - 2021-07-13

#### Added

- Adds analyzer for RockLib0004: Add InfoLog attribute
