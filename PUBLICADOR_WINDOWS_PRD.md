# Publicador Windows PRD/IIS — Valora Pulse

## Modo mais simples: duplo clique

No Windows Server, abra a pasta do projeto e dê duplo clique em:

```text
tools\windows\Publicar-Valora-PRD.bat
```

O menu PowerShell permite simular, publicar, gerar pacote, publicar com dados, rodar health check, restaurar backup e abrir relatórios.

## Modo terminal

```powershell
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --apply --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
```

## Simular publicação

```powershell
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --dry-run --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
```

## Gerar pacote IIS sem copiar

```powershell
node scripts/publish-iis-prd.js --mode firebase --package-only --project gestordepesquisa
```

## Publicar levando dados locais

```powershell
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --with-data --data-file .\exports\valora-prd-export.json --project gestordepesquisa --apply
```

## Restaurar backup

```powershell
node scripts/restore-iis-backup.js --iis-path C:\inetpub\wwwroot\valoragroup --latest
```

## Health check

```powershell
node scripts/healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa --check-firebase
```

## Onde ficam os artefatos locais

- Backups IIS: `backups/iis/valoragroup-YYYYMMDD-HHMM`.
- Relatórios: `publish/reports/publicacao-prd-YYYYMMDD-HHMM.md`.
- Pacotes: `publish/packages/valoragroup-iis-prd-YYYYMMDD-HHMM`.

Esses diretórios são ignorados pelo Git e não devem ser commitados.

## Proteções

O publicador bloqueia Firebase PRD sem `STORAGE_MODE: 'firebase'`, `FIREBASE_ENABLED: true` e `FIREBASE_CONFIG` completo, valida assets JS/CSS, impede `.map`, `.env`, service account e padrões de segredo no build, cria backup antes de limpar o IIS e não publica `dist/` como subpasta.

## Melhoria futura

Uma interface gráfica pode ser criada no futuro, mas a versão atual evita Electron para manter instalação simples, leve e auditável.
