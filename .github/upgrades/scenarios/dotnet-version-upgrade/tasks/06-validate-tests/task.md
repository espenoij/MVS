# 06-validate-tests: Run MSTest test suite and fix failures

Execute the MSTest test suite (already on MSTest 3.7.3 + `Microsoft.Testing.Platform` — no test framework migration required). Investigate and fix any failures introduced by `Api.0003` behavioral changes (40 flagged items) or by configuration/runtime differences between `net472` and `net10.0-windows`.

**Done when**: All tests in `MVSTests` pass.
