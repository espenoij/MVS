# 04-fix-build-errors: Fix all compilation errors from API breaking changes

Build the solution and resolve all compilation errors introduced by the framework retarget in a single bounded pass. Errors will cluster around WPF (largest impact — 3,962 flagged items), Legacy Configuration (732), GDI+ / `System.Drawing` (28), and WinForms (16). Apply the standard breaking-change fixes for binary-incompatible APIs (`Api.0001`, ~5,449 items) and source-incompatible APIs (`Api.0002`, ~1,124 items). Continue using Telerik-styled controls — do not replace them with stock WPF controls.

**Done when**: `dotnet build` produces 0 errors across the entire solution.
