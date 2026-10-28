# Vestigium.Helpers.Pdf — PR01 Implementation Plan

**Document ID:** VEST-HLP-PDF-PR01
**Version:** 1.0
**Status:** Draft
**Date:** 25 October 2026
**Requirements:** `Requirements_v1.0.md`

## 0. Steps

| Step | Delivers |
|---|---|
| PR01.001 | Project `src/Vestigium.Helpers.Pdf`, MIT, README, packs. |
| PR01.002 | Identity block. User, SID, machine, domain, UTC, adapters. |
| PR01.003 | Report body plus trailer page. QuestPDF. |
| PR01.004 | Body hash and HMAC seal. Key held with DPAPI. |
| PR01.005 | Public `PdfEvidence.Verify`. Valid, Tampered, NoTrailer. |
| PR01.006 | Pack and publish `Vestigium.Helpers.Pdf` to NuGet. MIT, README. |
| PR01.007 | FXplorerIQ takes the package. Verify writes the PDF. The button opens it. |

## 1. Library

QuestPDF, MIT, for the pages. `Vestigium.Helpers.Hashing` for the body hash and the seal. No iText. No certificate signing in this PR. Pdf does not implement a hash.

`PdfReport` takes the rows and the identity. `PdfEvidence.Build` returns the bytes. `PdfEvidence.Verify` reads them back.

## 2. Identity

```csharp
public sealed class PdfIdentity
{
    public string User { get; init; }
    public string? Sid { get; init; }
    public string Machine { get; init; }
    public string? Domain { get; init; }
    public DateTimeOffset Utc { get; init; }
    public string Version { get; init; }
    public IReadOnlyList<PdfAdapter> Adapters { get; init; }
}

public sealed class PdfAdapter
{
    public string Name { get; init; }
    public string Address { get; init; }
    public string Mac { get; init; }
}
```

`PdfIdentity.Capture()` reads the current process. It lists every adapter that is up and has a unicast address. It does not pick a favourite.

## 3. Seal

Body hash is SHA-256 of the PDF bytes before the trailer page is appended, through `HashingHelper` and `HashingAlgorithm.Sha256`.

The seal is HMAC-SHA256 of the canonical identity text plus the body hash, through `HashingHelper.HmacString` and an `HmacKey` of 32 bytes. The key is generated once per machine, wrapped with DPAPI, and stored beside the app data. It is not written into the PDF and it is not compiled into the library.

Both values are printed on the trailer page and stored in metadata. Verify rebuilds the canonical text from the trailer fields, recomputes the body hash, and checks the seal when the machine key is present.

## 4. Public check

```csharp
public static PdfVerdict Verify(string path);
public static PdfVerdict Verify(ReadOnlySpan<byte> pdf);

public sealed class PdfVerdict
{
    public PdfVerifyResult Result { get; init; }
    public bool BodyMatches { get; init; }
    public bool SealChecked { get; init; }
    public bool SealMatches { get; init; }
    public string? BodyHash { get; init; }
}
```

`Verify` reads the trailer, rebuilds the canonical identity, recomputes the body hash through `HashingHelper`, and checks the HMAC seal through `HashingHelper.HmacString` when the machine key is present. It is the only door a host needs. It does not throw for a bad file.

## 5. Host

Copy (Verify), Move (Verify), and Mirror (Verify) call `PdfEvidence.Build` when the pass finishes. The button opens the PDF. The jsonl remains for now.

## 7. Honest claim

The README and the trailer page say: this detects an edit. The seal proves the file was sealed on this machine. It does not prove who made it, and a PDF taken to another machine can have its body hash checked but not its seal. A later PR can add a certificate signature if that claim is needed.
