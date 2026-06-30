# Sprint 52 — Auditoria Firebase Credentials, Link Público e Menu Mobile

1. `repair-free-survey-public-link.js` exigia credencial padrão porque inicializava `firebase-admin` sem `credential`, acionando ADC.
2. Agora aceita `--credentials`, `FIREBASE_SERVICE_ACCOUNT_PATH`, `FIREBASE_SERVICE_ACCOUNT_JSON`, `GOOGLE_APPLICATION_CREDENTIALS`, `FIRESTORE_EMULATOR_HOST`, `GCLOUD_PROJECT`, `FIREBASE_PROJECT`, `FIREBASE_PROJECT_ID`.
3. Sim, aceita `--project`.
4. Sim, aceita `--credentials`.
5. Sim, suporta `FIREBASE_SERVICE_ACCOUNT_JSON`.
6. Sim, suporta `FIREBASE_SERVICE_ACCOUNT_PATH`.
7. Sim, suporta `GOOGLE_APPLICATION_CREDENTIALS`.
8. Sim, erro amigável sem stack trace gigante para credenciais ausentes.
9. Sim, gera `reports/free-survey-public-link-repair-report.json` mesmo sem credencial em dry-run controlado.
10. O risco era `expiresAt` curto/formatos inconsistentes; agora a política oficial usa 3650 dias.
11. O link prioriza `publicToken`; `tokenHash` não é token público.
12. O repair persiste `publicToken`, `publicUrl` e `publicLink` em `--apply`.
13. O validador encontrou `resp_demo` porque o `.bat` continha o literal no `echo`.
14. Sim, era falso positivo; o validador não monta mais literal direto e ignora apenas testes/spec/md.
15. Antes era majoritariamente estrutural; agora há teste Playwright runtime.
16. Seletores legado: `[data-action="toggleAdminMobileMenu"]`, `.admin-sidebar`, `.admin-mobile-overlay`, `.admin-sidebar .side-menu button`.
17. O botão aparece em viewport <= 991.98px.
18. O overlay recebe `.active` ao abrir.
19. Clique no item fecha o menu.
20. Valora.Web usa botão `[data-action="toggleWebMobileAdminMenu"]` e offcanvas `#mobileSidebar` com teste runtime.
