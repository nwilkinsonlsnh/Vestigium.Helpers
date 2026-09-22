# Processes — Design 1.3 (as built)

**Status:** Current  
**Date:** 15 September 2026

- Snapshotter fills `ProcessInfo` + `FieldAvailability` (Ok / Denied / Unsupported / Gone).
- `ProcessHelper.Search(term)` is StartsWith / EndsWith / Contains. `Search(query)` is Kql.
- Watchers sample CPU / IO / memory on an interval. Campaigns write JSONL under the campaign root.
- Caps go through `HelperGuard.AtMost` / `InRange`.
