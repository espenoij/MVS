# 05-config-migration — Progress Detail

## Findings
- `MVS\App.config` — Used heavily by `MVS\Config\Config.cs` via `ConfigurationManager.OpenMappedExeConfiguration` to read `appSettings` and custom `AppSettingsSection` sections (`SerialPortConfig`, `ModbusConfig`, `FileReaderConfig`, `ErrorMessages`). These patterns work unchanged on .NET 10 because `System.Configuration.ConfigurationManager` is provided by the `Microsoft.WindowsDesktop.App` framework reference.
- `MVSTests\app.config` — Contained only assembly binding redirects (`System.Diagnostics.DiagnosticSource`, `System.Memory`, `System.Collections.Immutable`, etc.) which are entirely obsolete on .NET 10 (no assembly unification needed).

## Changes Applied

### `MVS/App.config`
Removed the obsolete `<startup>` and `<runtime>/<assemblyBinding>` elements:
- `<startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2" /></startup>` — meaningless on .NET 10.
- Empty `<runtime>/<assemblyBinding>` block — no longer needed.

Kept all `<appSettings>` keys and custom section definitions — these are still used at runtime by `Config.cs`.

### `MVSTests/app.config`
**Deleted entirely.** All content was net472-specific binding redirects that no longer apply.

## What Was NOT Done
- No code changes to `Config.cs` — the `ConfigurationManager` API surface is identical on .NET 10. The custom `AppSettingsSection` registrations and `OpenMappedExeConfiguration` flows work as-is.
- No migration to `Microsoft.Extensions.Configuration` — the user's existing config code path is preserved (lower risk; can be done later as a separate modernization if desired).

## Validation
- `dotnet build`: ✅ Build successful, 0 errors, 0 warnings.
- Config file is still picked up by the build and copied to output (default behavior for `App.config` in SDK-style projects).

## Files Modified
- `MVS/App.config` (cleaned)
- `MVSTests/app.config` (deleted)
