## PR13a jobs

```csharp
var rows = PortScanCsv.Rows(scan);
var csv = PortScanCsv.Write(scan);
```

`Rows` is the grid door. One `PortScanOpenRow` per open port. Pure. No I/O.

`Write` formats those rows. Banner is quoted. No file write.

Nested host rows stay. They answer what is open on one box.

