# Hashing — PR01 security hygiene

**Status:** Done  
**Priority:** P0  
**Publish:** No

## Steps

| Step | Work | Status |
|---|---|---|
| PR01.001 | Bad PHC → `VerifyPassword` returns `false` and logs Failed. m/t/p over cap still throws `CryptographicException`. | **Done** |
| PR01.002 | `HmacKey` finalizer calls Dispose / ZeroMemory. Explicit Dispose stays. | **Done** |
| PR01.003 | Do not override ToString to hex. | **Done** |
| PR01.004 | Tests: bad PHC is false; HMAC JSONL has `keyBytes=` and not the key hex. File hash may log digest hex. | **Done** |

Fixtures live in `HashingPR01Tests`. HMAC already logged `keyBytes={length}` — no production change this step.
