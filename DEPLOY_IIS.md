
## Observação para produção Firebase

O deploy IIS com `STORAGE_MODE: 'firebase'` não carrega dados do `localStorage`. Antes da validação funcional em PRD/HML, execute a migração documentada em `MIGRACAO_LOCAL_FIREBASE.md`.

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

## Publicador automatizado PRD

Use `PUBLICADOR_IIS_PRD.md` como fluxo principal. Exemplo Windows:

```powershell
node scripts/publish-iis-prd.js --iis-path C:\inetpub\wwwroot\valoragroup --mode firebase --apply
```

O script gera build, copia `web.config`, valida assets hashados, cria backup e só então publica no IIS.

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

## Publicação PRD via duplo clique

Para operadores que não querem decorar comandos, use `tools\windows\Publicar-Valora-PRD.bat`. O menu executa o publicador Node, cria backup automático antes de copiar para o IIS, valida `index.html`, `assets` e `web.config`, e gera relatório em `publish/reports/`.
