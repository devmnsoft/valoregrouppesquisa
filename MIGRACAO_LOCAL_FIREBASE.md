# Migração localStorage → Firebase PRD/HML

## O que acontece

No modo local/demo, o Valora Pulse grava o estado no navegador em `localStorage`, na chave `valoraPulseFinal800`. Ao mudar `STORAGE_MODE` para `firebase`, o frontend passa a ler Firestore e Firebase Auth. Essa mudança **não migra dados automaticamente**.

## Como exportar local

Pelo Admin Valora, acesse **Backup e migração** e clique em **Exportar para produção com sanitização**. O arquivo baixado segue o padrão `valora-local-export-YYYYMMDD-HHMM.json` e remove senhas, tokens, SMTP, Telegram, service account e segredos. Marque **Incluir respostas de teste** apenas quando elas realmente forem necessárias.

Alternativa técnica, quando você já tiver um JSON bruto do estado local:

```bash
node scripts/export-local-state.js --file ./local-state.json --out ./exports/valora-local-export.json --include-responses --mask-demo-documents
```

Nunca commite arquivos de `exports/`, `backups/`, `.env` ou `serviceAccount.json`.

## Como preparar Firebase

1. Criar/selecionar projeto Firebase.
2. Habilitar Firestore, Firebase Auth, Functions e Hosting/IIS conforme arquitetura.
3. Cadastrar Authorized domains do ambiente.
4. Publicar Rules revisadas.
5. Configurar credencial local somente fora do repositório:

```bash
export GOOGLE_APPLICATION_CREDENTIALS=/caminho/seguro/serviceAccount.json
```

## Como importar

Faça dry-run primeiro:

```bash
node scripts/import-firestore-seed.js --file ./exports/valora-local-export.json --project gestordepesquisa --dry-run
```

Aplicação real com backup e usuários Auth:

```bash
node scripts/import-firestore-seed.js --file ./exports/valora-local-export.json --project gestordepesquisa --apply --backup --create-auth-users --send-password-reset
```

`--merge` é o comportamento padrão. `--overwrite` exige `--confirm-overwrite IMPORTAR` e só deve ser usado com aprovação explícita.

## Coleções e estratégia

Empresas locais em `companies` são importadas como `organizations/{companyId}`. O importador também mantém cópia legada em `companies/{companyId}` para compatibilidade enquanto o frontend ainda expõe alguns fluxos com o nome antigo. As demais coleções seguem os nomes esperados: `settings`, `modules`, `plans`, `users`, `forms`, `surveys`, `responses`, `invitations`, `invoices`, `actionPlans`, `notifications`, `knowledgeBase`, `supportCategories`, `supportSlaPolicies`, `supportTickets`, `supportMessages`, `integrations`, `webhooks`, `apiKeys` e `logs`.

## Como criar usuários

Use `--create-auth-users`. O script localiza por e-mail para evitar duplicidade, cria usuário ausente no Firebase Auth com senha temporária forte não exibida em log, grava `users/{uid}`, preserva o ID local em `legacyId` e aplica custom claims `{ role, companyId }`. Com `--send-password-reset`, o operador recebe links de reset para envio pelo fluxo operacional seguro.

## Como validar

```bash
node scripts/validate-firebase-seed.js --project gestordepesquisa
```

A validação confere planos, módulos, empresas, usuários, `admin_valora`, formulários, perguntas, pesquisas, vínculos de respostas, vínculos de usuários por empresa e `featuredSurveyId`.

## Rollback

Antes de `--apply`, rode com `--backup` ou execute:

```bash
node scripts/backup-firestore.js --project gestordepesquisa
```

Para rollback, use o JSON em `backups/firestore-backup-YYYYMMDD-HHMM.json`, revise manualmente o escopo e reimporte com o importador após aprovação explícita. Não apague dados de PRD sem confirmação formal.

## Checklist pós-importação

- Planos aparecem na página pública e no portal da empresa.
- Formulários preservam IDs, dimensões, perguntas, faixas e pontuação.
- Pesquisas apontam para formulários existentes.
- Respostas aparecem apenas se `--include-responses` foi usado.
- Usuários entram após reset de senha e claims atualizadas.
- “Empresa Exemplo” não é tratada como cliente real em PRD, salvo ambiente demo.
