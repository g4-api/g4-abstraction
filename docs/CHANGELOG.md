# Changelog

All notable changes to the G4.Abstraction packages are listed here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Each version heading matches a GitHub release tag without
its `v` prefix and the NuGet package version.

## 2026.11.25.27

### Removed

- `G4.Abstraction.WebDriver` moved to g4-engine, with the same package ID and namespace. The engine publishes it from
  now on, so the engine and this repository no longer depend on each other. Its last version from this repository is
  2026.11.25.26. The driver table, the driver-plugin guide and the package readme moved with it.

### Changed

- The `ClientApp` sample references the published `G4.Abstraction.WebDriver` package instead of the project.

## 2026.11.25.26

### Changed

- `G4.Abstraction.WebDriver` uses `G4.WebDriver` and every `G4.WebDriver.Remote.*` and `G4.WebDriver.Simulator`
  package at 2026.11.25.20, so the engine resolves one client version. They were at 2026.11.1.14.
- `G4.Abstraction.WebDriver` uses `G4.Cache` 2026.11.24.109. It was at 2026.10.30.89.
- The README describes the build instead of linking to a guide outside the repository.

## 2026.11.08.25

### Changed

- Updated the documentation for the current APIs and WebDrivers.

## 2026.11.03.24

### Added

- SAP WebDriver plugin support.

### Changed

- Updated the logging, cache and WebDriver packages.
