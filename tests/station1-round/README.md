# Station 1 Round Regression Checks

Run with Node.js and the .NET 8 SDK:

```sh
node tests/station1-round/run.mjs
```

Set `DOTNET` to the SDK executable path when it is not on `PATH`.

The runner extracts the current reset, navigation, scale-confirmation and save
methods from MainWindow.xaml.cs, together with both table-loading handlers, and
compiles their bodies with headless control and persistence doubles. Async
handlers return Task only in the temporary test copy so checks can await them.
No database or scale is used.

Checks cover two complete Enter-driven rounds, retained supervisor/line/bill,
fresh member and weighing details, editing retained fields, delayed bill lookups,
retained saved records, post-confirm selected-line reload and grey totals, duplicate submission, Clear during a pending save, save failure,
keyboard auto-repeat and reset request-version invalidation. Table checks use
scrambled transaction IDs and interleaved members to verify newest-first order,
chronological round labels, unchanged totals and blank member identifiers.
These checks do not replace a Windows WPF focus and physical-scale acceptance
test. Confirm and manual Clear retain supervisor, line and bill; the default
fresh-login reset still clears them. Clear checks cover repeated clicks before
the first save, edited values, delayed bill lookups, no database writes and
continuing the next Enter-driven round with the retained fields.

Shortcut checks cover `M` from each round step, selecting the bill number,
returning to the member number with Enter, preserving round data, preventing a
save from that Enter, modifier keys, auto-repeat, busy state and Station 2 isolation.
The previous `N` key must no longer activate the bill-number shortcut.

Release checks also verify that unregistered members can still save both records
and that successful and failed saves retain the production diagnostic calls.
