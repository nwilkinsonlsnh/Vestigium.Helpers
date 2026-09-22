# SRS v1.3 — PR04 amendment (Demo not shipped)

**Amends:** [`Requirements_v1.0.md`](Requirements_v1.0.md) §8  
**Date:** 20 September 2026

§8 as written describes `Vestigium.Helpers.Hashing.Demo` (`HelperWpfHost.Start`, APPID `Hashing`, gallery tabs).

That project is **not in `Vestigium.Helpers.slnx` and will not be added this wave.** The rest of the SRS is unchanged. Hosts initialize `VestigiumLogger` themselves. Contract coverage is `HashingContractTests`, `HashingLoggingTests`, and `HashingPR01Tests`.

JSONL when a host initializes: `%ProgramData%\Vestigium\Logs\Hashing\`
