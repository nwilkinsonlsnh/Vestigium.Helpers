# Vestigium.Helpers.FileSystemPaths

Path meaning. Resolve a registry path, sanitize a name, expand tokens, and keep long paths safe.

```csharp
var clean = PathResolve.Clean(raw);
var found = AppPathResolve.From(name, pathValue, scope);
```
