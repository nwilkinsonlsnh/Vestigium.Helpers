# Vestigium.Helpers.Kql.ActiveSetup

Field catalog and session for Active Setup. The dialect stays in `Vestigium.Helpers.Kql`.

One session. The Active Setup page binds `setup.*`.

```csharp
using var setup = ActiveSetupKql.Components();
var compiled = KqlHelper.Compile("setup.pending(true) && setup.stubpath.contains(powershell)", setup);
```
