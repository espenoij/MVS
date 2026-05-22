# 03-retarget-and-packages — Progress Detail

## Project File Changes

### MVS/MVS.csproj
- **TargetFramework**: `net472` → `net10.0-windows`
- **Properties**: kept `<OutputType>WinExe</OutputType>`, `<UseWindowsForms>true</UseWindowsForms>`, `<UseWPF>true</UseWPF>`. Added `<RollForward>LatestMajor</RollForward>`. Removed legacy ClickOnce/bootstrapper/manifest properties (no longer needed for net10).
- **Removed reference assemblies** (now part of framework reference): `System.ComponentModel`, `System.ComponentModel.Composition`, `System.ComponentModel.DataAnnotations`, `System.Configuration`, `System.Configuration.Install`, `System.Data.OracleClient`, `System.Management`, `System.Net`, `System.Security`, `System.ServiceProcess`, `System.Transactions`, `System.Web.Extensions`, `Microsoft.CSharp`, `System.Data.DataSetExtensions`, `System.Web`.
- **Removed framework-included packages** (NU1510): `Microsoft.NETCore.Platforms`, `Microsoft.Win32.Primitives`, `NETStandard.Library`, all 32 `System.*` 4.3.x packages, `System.Collections.Immutable`, `System.Configuration.ConfigurationManager`, `System.Diagnostics.DiagnosticSource`, `System.IO.Pipelines`, `System.Reflection.Metadata`, `System.Runtime.CompilerServices.Unsafe`, `System.Text.Json`.
- **Replaced incompatible package**: `Microsoft.Xaml.Behaviors.Wpf` 1.1.135 → 1.1.39 (the version validated as compatible by the upgrade analyzer).
- **Telerik UI**: kept HintPath references to `..\lib\RCWPF\2026.1.415.462.NoXaml\*.dll` (2026.1 release supports .NET 10).
- **Resources**: consolidated 18 `Resource` items into a single `<ItemGroup>`.
- **Kept compatible packages**: CrcDotNET, DeviceId.Windows.Wmi, Google.Protobuf, K4os.Compression.LZ4.Streams, MathNet.Numerics, MIConvexHull, MySql.Data, MySqlConnector, NModbus.Serial, Portable.BouncyCastle, Telerik.Licensing, ZstdSharp.Port.

### MVSTests/MVSTests.csproj
- **TargetFramework**: `net472` → `net10.0-windows`
- **Removed reference assemblies**: `Microsoft.CSharp`, `System.Configuration`, `System.Net.Http`, `System.Runtime` (all framework-included on net10).
- **Removed deprecated package**: `Microsoft.ApplicationInsights` 2.22.0 — not used by the test infrastructure; the modern test stack provides telemetry separately.
- **Removed framework-included / transitive packages**: `Microsoft.Testing.Extensions.Telemetry`, `Microsoft.Testing.Extensions.TrxReport.Abstractions`, `Microsoft.Testing.Extensions.VSTestBridge`, `Microsoft.Testing.Platform`, `Microsoft.Testing.Platform.MSBuild`, `Microsoft.TestPlatform.ObjectModel`, `System.Buffers`, `System.Collections.Immutable`, `System.Diagnostics.DiagnosticSource`, `System.Memory`, `System.Numerics.Vectors`, `System.Reflection.Metadata`, `System.Runtime.CompilerServices.Unsafe`, `System.Threading.Tasks.Extensions`, `System.Xml.ReaderWriter`. These come transitively via `Microsoft.NET.Test.Sdk` + MSTest.
- **Updated test SDK**: `Microsoft.NET.Test.Sdk` 16.* → 18.5.1 (latest supported).
- **Kept**: `MSTest.Analyzers`, `MSTest.TestAdapter`, `MSTest.TestFramework` (3.7.3 — already modern).

## Validation
- `dotnet restore MVS.sln`: ✅ Clean restore, no NU1510/NU1605 warnings, no errors.
- Both projects target `net10.0-windows`.
- Build will be addressed in task 04 (compilation errors from API breaking changes are expected next).

## Files Modified
- `MVS/MVS.csproj`
- `MVSTests/MVSTests.csproj`
