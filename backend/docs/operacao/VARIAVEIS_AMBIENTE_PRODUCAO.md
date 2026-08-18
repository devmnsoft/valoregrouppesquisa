# Variáveis de ambiente de produção
Use a notação `Secao__Chave`. A referência completa e sem secrets está em `.env.example`.

Obrigatórias críticas: `ConnectionStrings__Postgres`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` (aleatória, mínimo 32 caracteres), `App__PublicBaseUrl`, `App__AdminBaseUrl`, `Security__RequireHttps=true` e origens CORS explícitas. Produção exige `App__EnableDemoSeed=false` e `App__EnableDetailedErrors=false`.

E-mail requer host, porta, usuário, senha e remetente quando habilitado. Storage requer provider e caminho persistente externo à publicação. PDF e integrações devem permanecer desabilitados até terem dependências reais configuradas. A validação é sanitizada e aparece em `/api/v1/system-health`; valores secretos nunca são retornados.
