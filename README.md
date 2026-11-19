# HMS
Helideck Monitoring System

## Telerik migration status

The collection-decoupling work uses .NET collection interfaces in `DisplayList`
and `CaptureReadinessChecker`. Some display, error-message, chart and loaded
project-sample and fixed-value output collections now use native collections;
Telerik collections remain in acquisition, project-list, setup and other display
paths. Display transfers still update the first matching item in place, append
copies of new items, and retain items absent from the source. Direct transfers
still clear and repopulate the display.

The main, setup, About, and PDF-preview window shells, data-analysis and report
progress windows, handler warning, recording readiness/status, screen-capture
confirmation, and small contextual popups now use native WPF windows. The
LiDAR messages expander also uses the native WPF control. Telerik alert and
confirmation call sites now use native WPF dialogs. Modal/modeless use, close
behavior, existing event flows, and progress updates are retained.

The migration must not change any app functionality or observable behavior.
Keep existing workflows, validation, device/data behavior, dialogs, grid
interactions, report content/output, charts and error paths unchanged. Do not
replace a Telerik-dependent area if its parity cannot be demonstrated; record
the evidence and blocker and leave that area unchanged.

Remaining runtime dependencies include:

- **Controls and themes:** `App.xaml` loads Telerik Material dictionaries.
  Telerik grids, tabs, step progress,
  numeric inputs, watermark inputs and dropdowns remain in views, and SES
  styles still target Telerik controls. The native About window dynamically
  creates a Telerik QR barcode; the native PDF-preview window still hosts
  `RadPdfViewer`.
- **Collections and charts:** `RadObservableCollection` remains in acquisition,
  database, project, setup and display paths. `CaptureCharts.xaml` and
  `DataAnalysis.xaml` still use Telerik charts, axes and series. Native chart
  collection tests do not establish parity for replacing the chart controls.
- **Documents:** `VerificationPdfReportExporter` uses Telerik document
  processing, and `DialogReportPreview` uses `RadPdfViewer`. Export, rendering,
  preview and report content must all be compared before replacing them.
- **Packages and licensing:** `MVS.csproj` references Telerik UI for WPF and
  Telerik licensing, includes a license file in output, and references
  `System.IO.Hashing` for the Telerik PDF exporter. `App.xaml.cs` registers
  Telerik licensing. `NuGet.config` maps the UI package to the local Telerik
  installer feed and licensing to nuget.org.
- **Assets and deployment:** tracked Telerik design-time assemblies remain
  under `lib/RCWPF`; license files exist at the repository root and under
  `MVS`. `MVS_Setup/HMSServerSetup.vdproj` lists Telerik assemblies with
  older version metadata. The actual installer output and deployment must be
  verified before removing those entries or assets.

Do not remove the remaining packages, licenses, themes, assets or installer
references until their consumers are replaced and build and deployment parity
are verified. Any chart or document replacement also needs feature, licensing
and redistribution checks.

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

`FixedValueDisplayTests` compares the fixed-value display notifications with
Telerik and exercises its timer, selection, start, stop and clear behavior.
`ProjectDataCollectionTests` compares loaded-sample notifications with Telerik
and exercises analysis, chart data and progress. The optional
`ProjectDataDatabaseIntegrationTests` additionally compares successful and
failed database loads. Set `MVS_TEST_LOCAL_MARIADB=1` only when a disposable,
isolated MariaDB instance is listening on loopback port 45173 with passwordless
root access. The test creates and removes a uniquely named synthetic database;
never point this fixture at a production instance.
