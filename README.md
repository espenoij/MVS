# HMS
Helideck Monitoring System

The application targets Windows and .NET 10. PDF reports use PDFsharp, and the
PDF preview requires the Microsoft Edge WebView2 Evergreen Runtime.

Run the tests on Windows:

```powershell
dotnet test MVSTests\MVSTests.csproj
```

The optional `ProjectDataDatabaseIntegrationTests` require a disposable,
isolated MariaDB instance listening on loopback port 45173 with passwordless
root access. Set `MVS_TEST_LOCAL_MARIADB=1` to enable them. The fixture creates
and removes a uniquely named synthetic database; never point it at production.
