# Cobertura crítica

A suíte existente cobre hosts API/BFF, autenticação e materialização de usuário, fluxo público/LGPD/transação, resultados e certificados, planos, governança, inferência e insights, segurança de dados, migrations e saúde operacional. Esta etapa reforça:

- fronteiras Domain/Application/Infrastructure e composição de controllers;
- chaves JWT ausentes, vazias, curtas e válidas sem exposição do valor;
- SQL canônico não destrutivo, idempotência estrutural e colunas `api_keys.key_hash`/`notifications.message`;
- pipeline completo de build/test/publish por scripts reproduzíveis.

Cobertura é evidência de regressão, não substitui revisão de segurança, pentest nem smoke em ambiente semelhante à produção.
