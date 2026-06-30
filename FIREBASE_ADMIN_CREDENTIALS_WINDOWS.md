# Firebase Admin Credentials no Windows

Use PowerShell:

```powershell
$env:GOOGLE_APPLICATION_CREDENTIALS="C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk.json"
npm run firebase:admin-credentials
npm run home:repair-free-survey-link -- --dry-run --project gestordepesquisa
```

Alternativa por argumento:

```powershell
npm run home:repair-free-survey-link -- --dry-run --project gestordepesquisa --credentials "C:\FirebaseKeys\gestordepesquisa-firebase-adminsdk.json"
```
