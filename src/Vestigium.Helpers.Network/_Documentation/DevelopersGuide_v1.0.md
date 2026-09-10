# Vestigium.Helpers.Network — Developers Guide

**Document ID:** VEST-HLP-NETWORK-DEV-000  
**Version:** 1.2  
**Status:** Draft with SRS v1.2.  
**Date:** 9 September 2026

[`Requirements_v1.0.md`](Requirements_v1.0.md) is the contract (document version **1.2**).

## Platforms

One `net10.0` DLL. Windows and Linux are first-class. The WPF Demo project stays `net10.0-windows`; ProbeHost-on-Linux consumes the library directly.

Do not spawn `ping` / `ip` / `ss` / `traceroute`. Read BCL, then `/proc` or netlink.

Linux ICMP: ICMP DGRAM first so an unprivileged service can echo when `ping_group_range` allows it. If a custom payload is rejected, retry empty and flag `PayloadRestricted`. Route mutations need `CAP_NET_ADMIN`; lack of it is a typed failure.

NetBIOS stays Windows-only.

Campaign files default to `/var/lib/vestigium/network/campaigns/` on Linux. Tests inject a temp root.
