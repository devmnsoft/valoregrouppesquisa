# Free Survey Public Link Repair Report

- Status: SKIPPED_CREDENTIALS_MISSING
- Mode: DRY_RUN
- Survey: n/a
- Backup: none
- Message: Credenciais Firebase Admin ausentes.

Para executar:
PowerShell:
$env:GOOGLE_APPLICATION_CREDENTIALS="C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk.json"
npm run home:repair-free-survey-link -- --dry-run --project gestordepesquisa

Ou:
npm run home:repair-free-survey-link -- --dry-run --project gestordepesquisa --credentials "C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk.json"
