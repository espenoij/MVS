# 01-prerequisites: Verify .NET 10 SDK & environment

Confirm the .NET 10 SDK is installed on the machine and that no `global.json` pins a lower SDK version that would block the upgrade. Establish a clean baseline (current solution builds on `net472`) so post-upgrade results are comparable.

**Done when**: .NET 10 SDK is available, no SDK pin blocks it, and the existing solution builds cleanly on its current framework.
