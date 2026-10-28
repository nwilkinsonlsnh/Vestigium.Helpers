# Vestigium.Helpers.Pdf — Requirements

**Document ID:** VEST-HLP-PDF-SRS-001
**Version:** 1.0
**Status:** Draft
**Date:** 25 October 2026
**Target:** .NET 10 / `net10.0`
**Project:** `src/Vestigium.Helpers.Pdf/`
**PR:** PR01

## 0. Purpose

Build a PDF that can be handed to someone as evidence. The body is the report. The last page is a trailer that says who ran it, where, and when, and carries a hash so a later check can tell if the file was edited.

This is tamper-evident. It is not a legal digital signature. The key lives in the library, so anyone with the library can produce a valid trailer. Say that in the Help. Do not claim non-repudiation.

## 1. What PR01 delivers

- A PDF with a report body and a trailer page.
- An identity block: user, machine, domain, UTC time, job id, verb, source, destination, counts, hash algorithm, app version.
- One row per up network adapter: name, IP, MAC. Not a single guessed address.
- A SHA-256 of the body, from `Vestigium.Helpers.Hashing`.
- An HMAC-SHA256 seal of the body hash plus the canonical identity, from `HashingHelper.HmacString`. The key is not in the PDF.
- `PdfEvidence.Verify(path)` returns Valid, Tampered, or NoTrailer. On the machine that sealed it, Valid means the HMAC matches. Off that machine, the body hash can still be checked. The seal cannot.
- MIT license, README, packable.

## 2. What PR01 does not do

- PKCS#7, PAdES, or a certificate signature. That is a later PR.
- Put the HMAC key in the PDF or in the library. The key is per machine, held with DPAPI. A PDF alone cannot be re-sealed.
- Claim the MAC or IP is the one true address. Adapters are listed. VPN and spoofed MACs are out of scope.
- Embed the verify jsonl. The PDF is the evidence. The jsonl can stay beside it.

## 3. Trailer

Visible last page, and the same block in PDF metadata so a tool can read it without rendering.

```
VESTIGIUM EVIDENCE
format        1
job           <id>
verb          Copy (Verify)
user          <name>
sid           <sid or none>
machine       <name>
domain        <domain or none>
utc           <round trip>
version       <app>
algorithm     <hash>
matched       <n>
failed        <n>
source        <path>
destination   <path>
adapters
  <name>  <ip>  <mac>
body          <sha256 of the report pages>
seal          <hmac-sha256 of body hash + canonical identity>
```

Canonical identity is the fields above the body hash, in that order, UTF-8, one field per line. The seal is HMAC-SHA256 of that text plus the body hash, keyed with the machine key. An edit to a report page changes the body hash, which changes the seal, which fails `Verify`.

The body hash uses `HashingHelper` with `HashingAlgorithm.Sha256`. The seal uses `HashingHelper.HmacString` with an `HmacKey` of 32 bytes. Pdf does not hash for itself. FileIo is not involved. It supplies the counts and the per-file digests that go in the report.

## 4. Verify

The check is public. A host calls it. The library does not hide it behind an internal type.

```csharp
public static PdfVerdict Verify(string path);
public static PdfVerdict Verify(ReadOnlySpan<byte> pdf);
```

`PdfVerdict` carries the result, the body hash from the trailer, the body hash just computed, and whether the seal was checked.

| Result | Meaning |
|---|---|
| Valid | Body hash matches, and on this machine the HMAC seal matches. |
| Tampered | Trailer is present and the body hash or the seal does not match. |
| NoTrailer | Not a Vestigium evidence PDF. |

Off the sealing machine, `Verify` can still confirm the body hash. It cannot confirm the seal, because the key is not in the file. That result is reported as body-valid, seal-unchecked. It is not called Valid.

`Verify` does not throw for a bad file. A missing file, a file that is not a PDF, and a PDF with no trailer all come back as `NoTrailer`. A hash that does not match comes back as `Tampered`.

## 5. License

MIT. README states the tamper-evident limit in plain words.
