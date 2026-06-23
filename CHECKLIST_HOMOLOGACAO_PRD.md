# Checklist de Homologação PRD — Valora Pulse™

Use este checklist após importar a base local para o projeto `gestordepesquisa` e publicar no IIS.

## 1. Publicação completa

```bat
tools\windows\07-publicar-tudo.bat
```

A rotina valida código, prepara o export local, importa dados para Firestore PRD, valida coleções/Auth/claims, gera `dist`, copia `dist` para `C:\inetpub\wwwroot\valoragroup`, copia `templates\iis\web.config` e roda health check.

## 2. Publicação por etapa

1. `tools\windows\01-validar-codigo.bat`
2. `tools\windows\02-importar-base-producao.bat`
3. `tools\windows\03-validar-base-producao.bat`
4. `tools\windows\04-gerar-dist-producao.bat`
5. `tools\windows\05-publicar-iis.bat`
6. `tools\windows\06-healthcheck-prd.bat`
7. `tools\windows\08-abrir-producao.bat`

## 3. Validação da base PRD

```powershell
node scripts\validate-prd-data.js --project gestordepesquisa
```

Esperado mínimo: `plans=4`, `modules=10`, `organizations=2`, `companies=2`, `users=6`, `forms=1`, `surveys=1`, `responses=2`, `invoices=1`, `actionPlans=2`, `notifications=8`, `knowledgeBase=2`, `supportCategories=12`, `supportSlaPolicies=4`, `authOk=6`, `claimsOk=6` e `errors=[]`.

## 4. Health check

```powershell
node scripts\healthcheck-prd.js --url https://valoragroup.mnsoft.com.br --project gestordepesquisa
```

No plano Spark, a ausência de Cloud Functions não deve reprovar o health check. `getEmailStatus` deve aparecer como não obrigatório quando `ENABLE_CLOUD_FUNCTIONS=false`.

## 5. Validação visual em aba anônima

- Login sem menções a Firebase, README, demo, `users/{uid}` ou credenciais de teste.
- Vitrine pública: somente Essencial, Profissional, Corporativo e Enterprise; Profissional recomendado; Gratuito ausente.
- Admin > Status do Ambiente: modo atual, plano Firebase, Cloud Functions, Project ID, usuário, UID, claims, contagens e último erro Firestore.
- Admin > Planos, Empresas, Usuários, Formulários, Pesquisas e Respostas com dados reais.
- Resultado do Valora Insight™ com devolutiva estratégica, radar textual e 5 dimensões oficiais.

## 6. Limitações atuais Spark

- Sem Cloud Functions obrigatórias.
- Envio/status de e-mail usa fallback amigável.
- Validações server-side, convites públicos avançados, webhooks e integrações reais devem ser ativados na migração para Blaze.

## 7. Próximos passos Blaze

- Ativar Cloud Functions para e-mail, validação pública de links, APIs, webhooks e tarefas pesadas.
- Revisar secrets no Secret Manager.
- Reativar validação backend obrigatória onde aplicável.

## Correção runtime capabilities e e-mail por ambiente

- Local: `server.py` fornece API local, outbox e SMTP opcional.
- PRD Spark: IIS estático + Firebase Auth/Firestore, sem API local, sem Cloud Functions, sem envio automático de e-mail.
- PRD Blaze futuro: Cloud Functions com Secret Manager para e-mail seguro e logs remotos.
- Backend externo futuro: API autenticada para transporte externo.
- Validações: `node scripts/validate-runtime-capabilities.js` e `node scripts/validate-email-environment.js` garantem que PRD Spark não chame `/api/email/*`, `/api/outbox`, `getEmailStatus` ou `logServerEvent`.
