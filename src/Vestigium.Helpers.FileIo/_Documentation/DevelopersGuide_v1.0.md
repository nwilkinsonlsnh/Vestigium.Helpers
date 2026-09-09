# Vestigium.Helpers.FileIo — Developers Guide

**Document ID:** VEST-HLP-FILEIO-DEV-000  
**Version:** 1.0  
**Status:** Proposed with SRS v1.0. Implementation has not started.  
**Date:** 9 September 2026

Open `Vestigium.Helpers.slnx`. Implementation will live in `src/Vestigium.Helpers.FileIo/`.

## Read first

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract. This file is the design companion once code exists.

Until acceptance, do not grow `FileIoHelper` past Identity and Probe.

## Locked (do not drift while designing)

- Robocopy is the behavior reference, not a process we spawn.
- Category `Helpers`, APPID `FileIo`, subcategories in the SRS §8. Register them on `HelperLog` or audit lines become `Uncategorized`.
- Lead time default 15 s, range 0–180 s. No 300 s.
- Certainty 100 % only when recon is complete. Progress is job-wide and per bucket.
- Live `FileIoProgress` is chatty. JSONL is sparse. That split is §6 of the SRS.
- Product name for the no-write pass is **Audit Mode**.
- Default collision is UniqueName (`.##`). Cap fails the file; it does not overwrite.
- Pause resumes committed bytes. Cancel aborts the current write and removes a dest this job created.
- Same recon team and buckets for delete.
- LAD, scheduler, admin, ACL copy: later. Not this revision.

## Sibling

Hashing for digests. Encryption for envelopes. Logging for JSONL. FileIo does not absorb those façades.
