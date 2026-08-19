# Status da fundação

## Estado consolidado

A solução oficial está em `backend/`. A fundação contém API, Web/BFF, Application, Infrastructure e o script PostgreSQL canônico. Nesta revisão foram corrigidos aliases das rotas críticas `/Login`, `/Plans`, `/Notifications` e `/SystemHealth`, removida a colisão do BFF de saúde e diferenciada a recusa de usuário inativo.

## Controles presentes

- JWT exige chave não vazia com ao menos 32 caracteres; produção bloqueia placeholders e chaves de demonstração.
- A configuração de Development usa chave exclusivamente local e o exemplo de ambiente exige segredo externo em produção.
- O script canônico cria/migra `notifications.message` e `api_keys.key_hash` de forma defensiva.
- Login valida hash bcrypt, estado, vínculo organizacional, roles e plano antes de emitir sessão.
- System Health publica somente estados sanitizados e correlation ID.

## Validação desta execução

O contêiner não possui o executável `dotnet`, `psql` nem um PostgreSQL provisionado. Por isso build, startups, login integrado e execução dupla do SQL permanecem pendentes de execução em ambiente com .NET SDK e PostgreSQL.
