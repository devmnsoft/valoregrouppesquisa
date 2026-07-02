# Guia de Migração — Backend Oficial Valora

## Fonte oficial

A partir desta sprint, `backend/Valora.sln` é a solução oficial da nova versão .NET do Valora. Toda nova feature deve ser implementada em `backend`.

## Estruturas oficiais

- API oficial: `backend/Valora.Api`.
- Application oficial: `backend/Valora.Application`.
- Domain oficial: `backend/Valora.Domain`.
- Infrastructure oficial: `backend/Valora.Infrastructure`.
- Testes oficiais: `backend/Valora.Tests`.
- Frontend oficial: `backend/Valora.Web` com ASP.NET Core MVC/Razor, Bootstrap 5, JavaScript puro e jQuery/AJAX.
- Banco oficial: PostgreSQL/Dapper no schema `valorapesquisa`, com scripts em `database/postgresql`.

## Papel do `backend-v2`

`backend-v2` é apenas referência temporária. Ele não deve receber novas evoluções, não deve ser usado como base de build oficial e não deve gerar outro frontend ou solution paralela.

## Regras de consolidação

- Não criar `backend-v3` ou qualquer solution paralela.
- Não mover a nova versão para outro diretório.
- Não apagar o legado JavaScript/Firebase da raiz.
- Não apagar `backend-v2` ainda.
- Não criar React, Vue, Angular, Vite ou SPA.
- Não expor senha, hashes, refresh tokens, segredo SMTP, connection string, stack trace ou payload sensível.
- Não retornar dados fake em telas/endpoints oficiais.

## Validação oficial

Execute:

```bash
npm run backend:official-validate
```

Quando houver SDK .NET disponível, execute também:

```bash
dotnet build backend/Valora.sln
dotnet test backend/Valora.sln
```

## Sprint SaaS + relatórios + certificados + LGPD + e-mail
A evolução oficial permanece em `backend/Valora.sln`. Novos recursos operacionais usam Dapper/PostgreSQL no schema `valorapesquisa`, controllers oficiais da API e Web MVC/Razor. SMTP real deve usar apenas variáveis `VALORA_SMTP_*`; a UI não expõe senha SMTP.

## Importação controlada legado/Firebase/localStorage

A importação oficial ocorre apenas em `backend/Valora.sln` e usa PostgreSQL. Fontes aceitas: export Firestore em JSON, export localStorage/database.sample e JSON manual. O Firebase real não é chamado.

Fluxo operacional: registrar fonte, criar batch, executar dry-run, revisar divergências/conflitos, reconciliar, aplicar com `confirmApply=true` e perfil `admin_valora`, validar prontidão, e manter rollback por batch. Rollback exige `confirmRollback=true`.

Nenhuma tela ou API exibe senha, hash, token, refresh token, connection string, segredo SMTP, stack trace ou payload bruto sensível. Tokens públicos legados devem ser mascarados/regenerados.
