# Vestigium.Helpers.Pdf

Evidence PDFs. A report body and a trailer page. The trailer names who ran it, where, and when, and carries a hash so a later check can tell if the file was edited.

This detects an edit. It is not a certificate signature and it does not prove who made the file. The seal key is per machine, held with DPAPI, and it is not in the PDF.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Pdf` 1.0.0 |
| TFM | `net10.0` |
| Depends on | `Vestigium.Helpers.Hashing` 1.4.1 |
| License | MIT |
| Contract | [002 -- Requirements Document](https://github.com/nwilkinsonlsnh/Vestigium.Helpers/tree/main/Vestigium.Documentation/Vestigium/Helpers/Pdf/002%20--%20Requirements%20Document) |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Pdf" Version="1.0.0" />
```

PR01.001 is the project. The report, the trailer, and `PdfEvidence.Verify` land in the steps after this one.
