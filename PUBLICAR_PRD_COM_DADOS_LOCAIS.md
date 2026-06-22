# Publicar PRD com dados locais

Este fluxo leva para o Firebase PRD os dados preparados no modo local/demo (`localStorage`, chave `valoraPulseFinal800`) sem versionar exports, backups, service accounts ou `dist/`.

## 1. Exportar no Admin Valora

1. Abra o ambiente local.
2. Entre como **Admin Valora**.
3. Revise planos, módulos, empresas, usuários, formulários, pesquisas e base do ValoraBot.
4. Acesse **Backup e migração**.
5. Clique em **Exportar dados para PRD Firebase**.
6. Salve em `exports/` com o padrão `valora-prd-export-YYYYMMDD-HHMM.json`.

Para publicar apenas estrutura, clique em **Exportar somente estrutura para PRD**. Essa opção inclui `settings`, `modules`, `plans`, `companies`, `users`, `forms`, `surveys`, `knowledgeBase`, `supportCategories` e `supportSlaPolicies`, mas exclui respostas, mensagens, logs e temporários.

## 2. Dry-run no terminal seguro

Configure credenciais administrativas por Application Default Credentials ou variável segura do Firebase Admin SDK. Não use service account versionada.

```bash
node scripts/import-local-export-to-firebase.js --file ./exports/valora-prd-export.json --project gestordepesquisa --dry-run
```

O dry-run valida o export, bloqueia campos sensíveis, mostra contagens por coleção e não escreve no Firebase.

## 3. Publicar PRD levando dados

```bash
node scripts/release-prd-with-data.js --file ./exports/valora-prd-export.json --project gestordepesquisa --iis-path C:\inetpub\wwwroot\valoragroup --apply
```

No `--apply`, o orquestrador executa backup, importação Firestore/Auth, claims, validação, `npm run check`, `npm run build:prod`, cópia do `dist/` para IIS e relatório local `release-prd-report-YYYYMMDD-HHMM.md`.

## 4. Validar dados PRD

```bash
node scripts/validate-prd-data.js --project gestordepesquisa
```

Valide também no Firebase Console:

- usuários existem no Firebase Auth;
- `users/{uid}` possui `legacyId`, `role`, `companyId` e `source: local_export`;
- custom claims têm `{ role, companyId }`;
- `organizations`, `companies`, `plans`, `modules`, `forms`, `surveys` e `knowledgeBase` existem;
- surveys apontam para forms e organizations válidos.

## 5. Validar IIS publicado

Abra `https://valoragroup.mnsoft.com.br` e confira:

- login admin;
- planos;
- usuários;
- perguntas/formulários;
- pesquisa pública;
- respostas, se importadas com `--include-responses`;
- certificado;
- ValoraBot.

## Backup e rollback

Backups ficam em `backups/firestore-prd-backup-YYYYMMDD-HHMM.json` e são ignorados pelo Git. Para rollback manual, use o backup gerado antes do apply e restaure as coleções necessárias com Admin SDK em janela controlada. Não apague PRD sem confirmação operacional e backup validado.

## Segurança

Os scripts bloqueiam export com `password`, `SMTP_PASSWORD`, `TELEGRAM_BOT_TOKEN`, `serviceAccount`, `private_key`, `apiSecret` ou `webhookSecret`. O importador não importa senha local; ele cria/reutiliza usuários Auth e pode gerar links de reset sem imprimir senhas.
