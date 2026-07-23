# Legacy to Official Backend Parity Matrix

| Functionality | Legacy | Official backend | Parity | Test | Data migrated | Responsible | Blocker | Cutover decision |
|---|---|---|---|---|---|---|---|---|
| Public survey | Firebase callable flow | Planned in Valora.Api/Web | Partial | Legacy emulator + Playwright | No | Engineering | Contract parity and load test | No cutover |
| Public result token v2 | Implemented in Functions | Pending parity | Partial | Functions tests | No | Engineering | Backend endpoint parity | No cutover |
| Company registration | Callable `registerCompanyAccount` | Pending parity | Partial | Function/static checks | No | Engineering | Auth/profile parity | No cutover |
| Plans/entitlements | Shared catalog, partial enforcement | Domain/Application target | Partial | Existing checks | No | Product/Engineering | Transactional counters for all limits | No cutover |
| Certificates | Legacy fallback | Target server-side PDF/validation | Gap | Backlog | No | Engineering | Registry and public validation endpoint | No cutover |
| E-mail | SMTP/HTTP result email | Pending parity | Partial | Existing tests | No | Operations | Provider webhook/delivery state | No cutover |
| Billing | Manual/guarded | Pending provider | Gap | Backlog | No | Business/Ops | Provider decision and credentials | No cutover |
| Support/audit | Legacy collections | Target backend modules | Partial | Backlog | No | Engineering | Rules/API parity | No cutover |
