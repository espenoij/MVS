# HMS
Helideck Monitoring System

## Telerik migration constraint

Replace Telerik controls and update visual design only. Preserve every existing
application capability and observable behavior; defer any area whose replacement
behavior cannot be verified.

Run the test suite on Windows when validating migration changes:

```powershell
dotnet test MVSTests\MVSTests.csproj
```
