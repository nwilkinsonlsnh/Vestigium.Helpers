# Hashing — PR02 EVENTID catalog

**Status:** Done  
**Priority:** P1  
**Depends on:** PR01  
**Publish:** No

Kept 13000–13020. Added 13025–13070. Twins: HashingEvents + HashingCatalog + EventCatalog/hashing.json.

Deleted HelperCompat.cs. Path helpers call HashingLog.RequireNotBlank (EVENTID 13070).

HashingLog.Success routes by verb. Named helpers exist as thin wrappers.

Close gate: HashString JSONL is 13025 with no digest; HashFile JSONL is 13030 and may include digest hex; catalog has 15 rows matching constants.
