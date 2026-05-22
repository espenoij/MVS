# 05-config-migration: Migrate legacy app.config patterns

Address the 732 Legacy Configuration System issues by migrating `app.config`-based settings/patterns to forms supported by modern .NET configuration. Preserve user-facing settings and connection strings. `System.Configuration.ConfigurationManager` is available via NuGet for legacy `appSettings` reads where needed.

**Done when**: Configuration loads at runtime without exceptions and existing settings are accessible via the same code paths or updated equivalents.
