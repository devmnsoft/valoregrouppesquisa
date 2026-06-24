# Configuração de produção

1. Execute `node scripts\apply-config.js --env production`.
2. Execute `node scripts\validate-config-profile.js --file config.js`.
3. Confirme `EMAIL_TRANSPORT: 'external-api'` e `COMMUNICATION_GATEWAY.baseUrl: 'https://api.valoragroup.mnsoft.com.br'`.
4. Publique o build no IIS com `tools\windows\29-publicar-producao-com-gateway.bat`.

Para contingência, altere temporariamente o perfil para `EMAIL_TRANSPORT: 'disabled'` e `COMMUNICATION_GATEWAY.enabled: false`, valide e republique. As respostas continuam concluindo; comunicações ficam registradas como pendentes.
