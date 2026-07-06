# Tratamento seguro de credenciais Firebase

Não versionar JSON de service account, chaves privadas, campos sensíveis ou credenciais em `wwwroot`, backend, SQL ou documentação. Use variável de ambiente ou Secret Manager. Se uma chave foi compartilhada fora do fluxo seguro, revogue e rotacione imediatamente no Firebase/Google Cloud. O comando `npm run security:no-service-account-secrets` bloqueia credenciais nas áreas oficiais.
