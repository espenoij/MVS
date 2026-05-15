# Scenario Instructions: .NET Version Upgrade (MVS)

## Parameters
- **Scenario**: dotnet-version-upgrade
- **Solution**: `C:\Users\espen.oijordsbakken\source\repos\MVS\MVS.sln`
- **Source framework**: `net472`
- **Target framework**: `net10.0-windows` (LTS, WinForms + WPF capable)
- **Source branch**: `main`
- **Working branch**: `upgrade-to-NET10`

## Preferences

### Flow Mode
**Automatic** — Run end-to-end; only pause when blocked, ambiguous, or needing user input.

### Commit Strategy
**Single Commit at End** — All-At-Once upgrade is one atomic operation; commit once after full validation succeeds.

### Technical Preferences
- **UI controls**: Always use Telerik-styled controls (RadComboBox, RadToggleSwitchButton, RadButton, etc.) instead of standard WPF controls. Source: `.github/copilot-instructions.md`.

## Strategy
**Selected**: All-At-Once
**Rationale**: Only 2 projects, both on uniform legacy framework (`net472`), simple dependency graph (test → app), most NuGet packages already have compatible modern versions. Phasing adds no value.

### Execution Constraints
- Single atomic upgrade — both projects updated together (project files, TFMs, packages, code fixes).
- Operation sequence: SDK-style conversion → retarget + package update → restore → build & fix all compilation errors in one bounded pass → run tests.
- Validate full solution build (0 errors) before running tests.
- Do not introduce tier ordering or per-project sequencing.
- Preserve Telerik UI usage; verify Telerik WPF packages support `net10.0-windows` during package update step.

## Key Decisions Log
- **2025**: Target framework set to **.NET 10.0 (LTS)** — user confirmed default suggestion.
- **2025**: Flow mode **Automatic** — user confirmed default.
- **2025**: Strategy **All-At-Once** auto-selected — 2 projects, uniform source framework, low structural complexity.
