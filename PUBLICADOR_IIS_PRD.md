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

## Health Check PRD pós-publicação

Use o script `scripts/healthcheck-prd.js` para validar IIS, HTML, assets JS/CSS, MIME, Firebase, Functions, Firestore opcional, pesquisa pública opcional e ValoraBot público após publicar.

```bash
node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa --check-firebase --check-functions
```

Integrado ao publicador IIS:

```bash
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --apply --health-url https://valoragroup.mnsoft.com.br --project gestordepesquisa
```

Relatórios são gerados em `publish/reports/` e não devem ser commitados.

## Publicador Windows com menu

A forma recomendada no Windows Server é executar `tools\windows\Publicar-Valora-PRD.bat`. O BAT abre `tools\windows\Publicar-Valora-PRD.ps1`, que chama `scripts/publish-iis-prd.js` para dry-run, apply, package-only, publicação com dados, health check e rollback.

Relatórios ficam em `publish/reports/` e backups em `backups/iis/`. Esses diretórios são ignorados pelo Git.
