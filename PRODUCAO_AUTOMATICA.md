# Produção Automática — Valora Pulse

## Entrar em produção pelo Windows

Dê duplo clique:

```text
tools/windows/Entrar-em-Producao-Valora.bat
```

Escolha:

```text
3. Entrar em produção importando dados locais
```

Essa opção executa:

```bash
node scripts/go-production.js --apply --with-data --open-browser
```

## Entrar em produção pelo terminal

```bash
npm run prod:go:data
```

## Simular sem aplicar

```bash
npm run prod:dry
```

## Importar dados automaticamente

O publicador lê `production.config.json`, configura `GOOGLE_APPLICATION_CREDENTIALS`, copia o export local para `exports/` e executa primeiro o dry-run da importação. Em modo `--apply --with-data`, executa a importação com backup, criação/reuso de usuários Auth, claims e respostas.

## Validar PRD

```bash
npm run prod:health
```

O fluxo completo também roda `scripts/validate-prd-data.js`, `npm run check`, `npm run security:check`, `npm run build:prod`, valida assets em `dist/`, garante `dist/web.config` e executa o health check PRD.

## Relatórios

Os relatórios finais ficam em:

```text
publish/reports/go-production-YYYYMMDD-HHMM.md
```

## Se der erro

O script para na etapa com falha, mostra o motivo, sugere como corrigir e grava o relatório parcial. A produção é bloqueada se `getEmailStatus` estiver usando `fetch` direto ou se o health check apontar CORS/getEmailStatus.
