# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade the MVS solution from **.NET Framework 4.7.2** to **.NET 10.0 (LTS)** (`net10.0-windows` to preserve WPF + WinForms support).
**Scope**: 2 projects — `MVS` (WinForms host with heavy Telerik WPF UI) and `MVSTests` (MSTest test project). 96 affected files, 76 NuGet packages, ~6.7k flagged compatibility items dominated by WPF (3,962) and Legacy Configuration (732).

### Selected Strategy
**All-At-Once** — Both projects upgraded together in a single coordinated operation.
**Rationale**: Only 2 projects, both on uniform `net472`, simple test → app dependency, and most NuGet packages already have compatible modern versions. Phasing adds no value.

## Tasks

### 01-prerequisites: Verify .NET 10 SDK & environment

Confirm the .NET 10 SDK is installed on the machine and that no `global.json` pins a lower SDK version that would block the upgrade. Establish a clean baseline (current solution builds on `net472`) so post-upgrade results are comparable.

**Done when**: .NET 10 SDK is available, no SDK pin blocks it, and the existing solution builds cleanly on its current framework.

---

### 02-sdk-style-conversion: Convert both project files to SDK-style

Convert `MVS/MVS.csproj` and `MVSTests/MVSTests.csproj` from legacy non-SDK format to modern SDK-style. Preserve all references, build items, resources, embedded files, designer associations, app.config behavior, and Telerik UI integration. Keep the current `net472` target during this step — only the project file format changes.

**Done when**: Both `.csproj` files use SDK-style format, projects load in Visual Studio, and the solution still builds successfully on `net472`.

---

### 03-retarget-and-packages: Retarget to net10.0-windows and update packages

Set ``<TargetFramework>net10.0-windows</TargetFramework>``, enable ``<UseWPF>true</UseWPF>`` and ``<UseWindowsForms>true</UseWindowsForms>`` on both projects. Remove the 46 NuGet packages now provided by the framework reference (mostly `System.*` 4.3.x and `NETStandard.Library`). Replace the incompatible `Microsoft.Xaml.Behaviors.Wpf` with a version compatible with .NET 10. Replace deprecated `Microsoft.ApplicationInsights` 2.22.0 (or remove if telemetry is not used). Bump recommended packages (`System.Collections.Immutable`, `System.Configuration.ConfigurationManager`, `System.Diagnostics.DiagnosticSource`, `System.IO.Pipelines`, `System.Reflection.Metadata`, `System.Runtime.CompilerServices.Unsafe`, `System.Text.Json`) to versions aligned with .NET 10. Verify Telerik UI for WPF packages support `net10.0-windows`.

**Done when**: Both projects target `net10.0-windows`, package list is clean (no framework-redundant packages, no deprecated/incompatible packages), and `dotnet restore` succeeds for the solution.

---

### 04-fix-build-errors: Fix all compilation errors from API breaking changes

Build the solution and resolve all compilation errors introduced by the framework retarget in a single bounded pass. Errors will cluster around WPF (largest impact — 3,962 flagged items), Legacy Configuration (732), GDI+ / `System.Drawing` (28), and WinForms (16). Apply the standard breaking-change fixes for binary-incompatible APIs (`Api.0001`, ~5,449 items) and source-incompatible APIs (`Api.0002`, ~1,124 items). Continue using Telerik-styled controls — do not replace them with stock WPF controls.

**Done when**: `dotnet build` produces 0 errors across the entire solution.

---

### 05-config-migration: Migrate legacy app.config patterns

Address the 732 Legacy Configuration System issues by migrating `app.config`-based settings/patterns to forms supported by modern .NET configuration. Preserve user-facing settings and connection strings. `System.Configuration.ConfigurationManager` is available via NuGet for legacy `appSettings` reads where needed.

**Done when**: Configuration loads at runtime without exceptions and existing settings are accessible via the same code paths or updated equivalents.

---

### 06-validate-tests: Run MSTest test suite and fix failures

Execute the MSTest test suite (already on MSTest 3.7.3 + `Microsoft.Testing.Platform` — no test framework migration required). Investigate and fix any failures introduced by `Api.0003` behavioral changes (40 flagged items) or by configuration/runtime differences between `net472` and `net10.0-windows`.

**Done when**: All tests in `MVSTests` pass.

---

### 07-final-validation: Full clean build and final test run

Perform a clean build of the entire solution and run the full test suite end-to-end against the upgraded projects. Confirm there are no remaining incompatible packages, deprecated packages, or build/test regressions.

**Done when**: Clean solution build succeeds with 0 errors, all tests pass, and the upgrade is ready to commit.
