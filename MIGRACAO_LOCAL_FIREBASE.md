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

## Bootstrap inicial PRD Firebase

Use os scripts de bootstrap somente a partir de uma estação segura com credenciais administrativas via Application Default Credentials ou ambiente controlado de CI/CD. Não versione service accounts, exports, backups ou senhas.

### Dry-run obrigatório

```bash
node scripts/bootstrap-firebase-prd.js --project gestordepesquisa --dry-run
```

O dry-run lista as criações/mesclagens planejadas e não grava no Firebase. Sem `--apply`, o script nunca escreve dados.

### Aplicar bootstrap

```bash
node scripts/bootstrap-firebase-prd.js --project gestordepesquisa --apply --admin-email admin@valoragroup.com.br --admin-name "Admin Valora"
```

Parâmetros principais: `--project`, `--dry-run`, `--apply`, `--admin-email`, `--admin-name`, `--admin-password`, `--seed-demo-survey`, `--seed-demo-response`, `--merge` e `--overwrite`. O padrão é `--merge`; `--overwrite` exige `--confirm-overwrite gestordepesquisa`. A senha nunca é exibida em log. Se `--admin-password` não for informado, uma senha forte temporária é gerada e deve ser substituída por fluxo seguro de redefinição.

O bootstrap cria/atualiza de forma idempotente: Admin Valora em Auth e `users/{uid}`, custom claims `{ role: 'admin_valora', companyId: '' }`, `settings/global`, planos comerciais, módulos, `organizations/org_valora_prd`, compatibilidade em `companies/org_valora_prd`, formulário Valora Insight™, pesquisa ativa com `tokenHash`, base de conhecimento, categorias de atendimento e políticas de SLA. Resposta demo só é criada com `--seed-demo-response`.

### Validar pós-bootstrap

```bash
node scripts/validate-prd-bootstrap.js --project gestordepesquisa
```

A validação confirma Admin em Auth/Firestore, custom claims, planos, módulos, `settings/global`, organização, formulário, pesquisa ativa, vínculo survey→form e referências plan→modules. Depois, validar manualmente no IIS: login admin, portal admin, planos, empresa/plano contratado, perguntas, pesquisa pública, envio de resposta, resultado, certificado e ValoraBot.

### getEmailStatus em PRD

Em modo Firebase, `getEmailStatus` deve continuar sendo chamada por `httpsCallable('getEmailStatus')`, nunca por `fetch` direto para `cloudfunctions.net/getEmailStatus`. Se o e-mail não estiver configurado, a aplicação deve mostrar status amigável e não quebrar o carregamento.
