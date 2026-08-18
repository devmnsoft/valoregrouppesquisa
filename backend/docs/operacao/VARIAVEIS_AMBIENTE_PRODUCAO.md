# Variáveis de ambiente de produção
Use a notação `Secao__Chave`. A referência completa e sem secrets está em `.env.example`.

Obrigatórias críticas: `ConnectionStrings__Postgres`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` (aleatória, mínimo 32 caracteres), `App__PublicBaseUrl`, `App__AdminBaseUrl`, `Security__RequireHttps=true` e origens CORS explícitas. Produção exige `App__EnableDemoSeed=false` e `App__EnableDetailedErrors=false`.

E-mail requer host, porta, usuário, senha e remetente quando habilitado. Storage requer provider e caminho persistente externo à publicação. PDF e integrações devem permanecer desabilitados até terem dependências reais configuradas. A validação é sanitizada e aparece em `/api/v1/system-health`; valores secretos nunca são retornados.
> Nunca use a chave `DEV_ONLY_` presente no arquivo de Development em produção e nunca versione um segredo real.

## Configuração JWT obrigatória

O ASP.NET Core converte `__` em `:`. Produção deve fornecer os valores via variáveis do processo, cofre de segredos ou mecanismo equivalente:

```dotenv
Jwt__Issuer=ValoraInsight
Jwt__Audience=ValoraInsight
Jwt__SigningKey=CHANGE_ME_WITH_AT_LEAST_32_CHARACTERS_FOR_PRODUCTION
```

Gere para `Jwt__SigningKey` um valor aleatório e exclusivo com pelo menos 32 caracteres. Valor ausente, em branco, curto ou iniciado por `DEV_ONLY_` bloqueia a inicialização/validação segura de produção. O painel de Saúde do Sistema informa apenas o estado da configuração e nunca devolve a chave.
