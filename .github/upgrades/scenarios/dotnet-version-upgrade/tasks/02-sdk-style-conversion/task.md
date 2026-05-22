# 02-sdk-style-conversion: Convert both project files to SDK-style

Convert `MVS/MVS.csproj` and `MVSTests/MVSTests.csproj` from legacy non-SDK format to modern SDK-style. Preserve all references, build items, resources, embedded files, designer associations, app.config behavior, and Telerik UI integration. Keep the current `net472` target during this step — only the project file format changes.

**Done when**: Both `.csproj` files use SDK-style format, projects load in Visual Studio, and the solution still builds successfully on `net472`.
