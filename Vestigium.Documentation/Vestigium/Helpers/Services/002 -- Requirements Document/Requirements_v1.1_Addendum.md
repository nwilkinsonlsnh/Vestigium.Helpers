# Services — Requirements addendum 1.1

**Parent:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Status:** Accepted — shipped. Tests passing 15 September 2026.

| Topic | Rule |
|---|---|
| Tree cap | Unique service names visited. `ServiceHelper.MaxTreeNodes` = `ServiceTreeWalker.MaxNodes` = 256. DependsOn, DependedBy, and Both share one `seen` set. Flatten count ≤ cap, names unique. |
| Hidden | `ListHidden` disjoint from Visible. `List(..., All)` is the union. |
| Control | Start / Stop / Restart / Pause / Resume when the service allows it. Confirm on destructive calls. |
