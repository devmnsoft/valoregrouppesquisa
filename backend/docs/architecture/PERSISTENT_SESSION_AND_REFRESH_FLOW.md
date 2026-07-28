# Sessão persistente e rotação de refresh

Login e cadastro criam `user_sessions`, `refresh_token_families` e `refresh_tokens`. O token bruto é devolvido uma vez ao BFF e o banco recebe somente seu hash SHA-256. O access token de 15 minutos contém `sub`, `organizationId`, `sessionId`, `role` e `locale`.

Na renovação, uma transação seleciona o hash com `FOR UPDATE`, valida token e sessão, insere o sucessor, marca `used_at`, vincula `replaced_by_id` e atualiza `last_used_at`. Uma segunda apresentação do token consumido revoga família e sessão. Logout revoga token e sessão; logout global revoga todas as sessões do usuário.
