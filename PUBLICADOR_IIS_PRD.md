# Publicador PRD para IIS — Valora Pulse

O publicador automatiza build, validação, `web.config`, backup, pacote e cópia para IIS. O navegador **não** publica diretamente no IIS por segurança; o Admin apenas gera export/checklist/config e mostra comandos.

## Dry-run

```powershell
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --dry-run
```

## Publicar sem dados

```powershell
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --apply
```

## Publicar levando dados locais

```powershell
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --with-data --data-file .\exports\valora-prd-export.json --project gestordepesquisa --apply
```

## Gerar pacote sem copiar

```powershell
node scripts/publish-iis-prd.js --mode firebase --package-only
```

## NPM

```powershell
npm run publish:iis:dry -- --iis-path C:\inetpub\wwwroot\valoragroup
npm run publish:iis -- --iis-path C:\inetpub\wwwroot\valoragroup
npm run package:iis
```

## Validações críticas

- bloqueia Firebase PRD sem `STORAGE_MODE='firebase'`, `FIREBASE_ENABLED=true` e `apiKey/authDomain/projectId/appId`;
- valida `dist/index.html`, `dist/assets`, `assets/app.*.js`, `assets/style.*.css`;
- confere se o HTML referencia arquivos existentes, evitando JS/CSS com MIME `text/html`;
- bloqueia `.map`, `.env`, service account, tokens e senhas;
- gera `dist/web.config` sem URL Rewrite para evitar 500.19;
- no `--apply`, cria backup em `backups/iis/valoragroup-YYYYMMDD-HHMM/` antes de limpar e copiar;
- gera relatório em `publish/reports/iis-prd-publish-YYYYMMDD-HHMM.md`.
