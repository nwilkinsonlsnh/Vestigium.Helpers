# Vestigium.Helpers.Kql.AppMgmt

Field catalogs and sessions for AppMgmtIQ. The dialect stays in `Vestigium.Helpers.Kql`.

Two sessions. The package page binds `app.*`. The Windows Features page binds `feature.*`.

```csharp
using var packages = AppMgmtKql.Packages();
var compiled = KqlHelper.Compile("app.publisher LIKE '%Microsoft%' && app.size.mb GT 50", packages);
```

Requirements: `Vestigium.Documentation/Vestigium/Helpers/Kql.AppMgmt/001 -- Implementation Plan/PR01`.
