# HMS
Helideck Monitoring System

## Telerik migration status

The first collection-decoupling increment uses .NET collection interfaces in
`DisplayList` and `CaptureReadinessChecker`, with their algorithms unchanged.
All runtime collection instances, including display lists, retain their original
Telerik types until Windows binding and live-update parity can be demonstrated.
Display transfers still update the first matching item in place, append copies
of new items, and retain items absent from the source. Direct transfers still
clear and repopulate the display.

The migration must not change any app functionality or observable behavior.
Keep existing workflows, validation, device/data behavior, dialogs, grid
interactions, report content/output, charts and error paths unchanged. Do not
replace a Telerik-dependent area if its parity cannot be demonstrated; record
the evidence and blocker and leave that area unchanged.

Controls, acquisition collections, chart buffers, PDF generation/preview, packages,
licensing and installer dependencies remain unchanged. Chart notification batching
and document/viewer replacements require separate parity and deployment validation.
The existing Telerik-control guidance still applies to controls; replacing it
requires resolving the applicable instruction conflict before UI migration.

Validate the focused tests on Windows with .NET 10 and the configured Telerik feed:

```powershell
dotnet test MVSTests\MVSTests.csproj --filter "FullyQualifiedName~DisplayListTests|FullyQualifiedName~CaptureReadinessCheckerTests|FullyQualifiedName~ReadinessReportTests"
```

The display tests exercise both Telerik and .NET collection destinations against
the same expectations, and readiness tests compare both collection sources.
These checks do not prove WPF rendering or binding parity. Before replacing
runtime collections, compare status, sensor, serial, file, fixed-value and output
grids with the original app during start/stop and live updates, including
selection, sorting, scrolling, updates, notifications and errors. Linux cannot
run the WPF application or its WindowsDesktop-dependent test assembly.
