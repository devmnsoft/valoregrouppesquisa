# Valora Pesquisa backend-v2

Fundação .NET 8 paralela para o fluxo vertical mínimo: organização, usuário, autenticação JWT, formulário, pesquisa, link público, resposta, resultado e auditoria.

## Pré-requisitos
- .NET SDK 8
- Docker / Docker Compose
- PostgreSQL 16 ou container equivalente
- Node.js apenas para o validador local

## Subir banco
```bash
cd backend-v2
docker compose up -d postgres
```

## Aplicar script
```bash
psql postgresql://postgres:postgres@localhost:5432/valorapesquisa -f database/postgresql/scriptbd_completo.sql
```

## Rodar API
```bash
cd backend-v2
dotnet run --project src/ValoraPesquisa.Api/ValoraPesquisa.Api.csproj
```
API local: http://localhost:5000

## Rodar Web MVC
```bash
cd backend-v2
dotnet run --project src/ValoraPesquisa.Web/ValoraPesquisa.Web.csproj
```
Web local: http://localhost:5001

## Credenciais demo
Seeds de desenvolvimento são criados por `scriptbd_completo.sql`. As senhas sugeridas para ambientes locais são:
- `admin@valoragroup.com` / `Valora@2026`
- `gestor@empresa.com` / `Empresa@2026`

Os hashes versionados são apenas de desenvolvimento e devem ser substituídos em qualquer ambiente compartilhado ou produtivo.

## Testes
```bash
dotnet build ValoraPesquisa.sln
dotnet test ValoraPesquisa.sln
node tools/validate-backend-v2-foundation.js
```

## Docker Compose completo
```bash
cd backend-v2
docker compose up --build
```
- PostgreSQL: localhost:5432
- API: http://localhost:5000
- Web: http://localhost:5001

## Fluxo vertical manual
1. Entrar no Web MVC.
2. Criar organização e usuário.
3. Fazer login.
4. Criar formulário com perguntas/opções.
5. Criar pesquisa vinculada ao formulário.
6. Publicar pesquisa.
7. Criar link público.
8. Abrir a URL pública e enviar resposta.
9. Conferir resultado público.
10. Conferir eventos em Auditoria.

## Scripts
Scripts Windows ficam em `tools/windows`; scripts Linux ficam em `tools/linux`.

## Troubleshooting
- PostgreSQL: confira host, porta, usuário, senha e se o schema `valorapesquisa` foi criado.
- JWT: use chave de desenvolvimento com pelo menos 32 caracteres e refaça login após troca da chave.
- CORS: em execução separada, ajuste a origem permitida da API antes de publicar.
- Build: instale o .NET SDK 8 e restaure pacotes com acesso à internet.
- Migrations: nesta fundação, o script SQL idempotente é a fonte local; migrations EF ainda não são usadas.

## Sprint 04 — SaaS: planos, módulos, limites e dashboard

A camada SaaS do `backend-v2` usa PostgreSQL/Dapper no schema `valorapesquisa` e mantém `organizations.plan_code` como compatibilidade enquanto a assinatura real fica em `subscriptions`.

### Planos disponíveis

| Plano | Código | Limites principais |
| --- | --- | --- |
| Free | `free` | 1 pesquisa ativa, 30 respostas/mês, 1 usuário/gestor, 1 formulário, 1 link público. |
| Essential | `essential` | 3 pesquisas ativas, 150 respostas/mês, 3 usuários, 3 formulários, 5 links públicos e convite/exportação básica. |
| Growth | `growth` | 12 pesquisas ativas, 1000 respostas/mês, 10 usuários, 20 formulários, 50 links, relatórios, certificados, benchmark e white label básico. |
| Enterprise | `enterprise` | Limites ilimitados via `-1`, API, suporte, LGPD, white label e auditoria avançada. |

### Módulos disponíveis

`clientes`, `planos`, `modulos`, `usuarios`, `formularios`, `pesquisas`, `links_publicos`, `respostas`, `relatorios`, `certificados`, `convites_email`, `auditoria`, `lgpd`, `suporte`, `white_label`, `benchmark`, `exportacoes`, `integracoes`, `dashboard` e `configuracoes`.

### Assinatura e bloqueio comercial

* `GET /organizations/{organizationId}/subscription` consulta a assinatura atual.
* `PUT /organizations/{organizationId}/subscription` altera o plano; nesta sprint apenas `admin_valora` deve executar.
* `PATCH /organizations/{organizationId}/subscription/status` suspende/reativa a assinatura.
* `GET /organizations/{organizationId}/can-create-survey`, `/can-create-form`, `/can-create-user`, `/can-create-link` e `/can-use/{moduleCode}` validam entitlements.

Os endpoints de criação de formulários, pesquisas, links, respostas públicas e usuários validam assinatura, módulo e limite antes de persistir. Bloqueios retornam `MODULE_NOT_ENABLED`, `PLAN_LIMIT_REACHED` ou `SUBSCRIPTION_INACTIVE` e registram auditoria comercial.

### Dashboard e menu

* `GET /dashboard/global` retorna indicadores reais globais para `admin_valora`.
* `GET /dashboard/organization` retorna plano, assinatura, uso, módulos, respostas e auditoria da organização.
* `GET /me/menu` retorna menu dinâmico por perfil, plano, módulo e assinatura.

### Validação Sprint 04

```bash
dotnet build backend-v2/ValoraPesquisa.sln
dotnet test backend-v2/ValoraPesquisa.sln
npm run backend-v2:validate
npm run backend-v2:saas-validate
```
