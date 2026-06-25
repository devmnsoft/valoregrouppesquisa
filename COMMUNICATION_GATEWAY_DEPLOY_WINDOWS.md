# Publicação do Communication Gateway no Windows Server

1. Copie a pasta `communication-gateway` para `C:\DBBACK\valoregrouppesquisa\communication-gateway`.
2. Copie `.env.production.example` para `.env` e configure SMTP e Firebase Admin.
3. Execute `npm install --omit=dev`.
4. Valide com `npm test`.
5. Instale como serviço com NSSM usando `communication-gateway\tools\windows\06-instalar-servico-nssm.md`.
6. Publique o reverse proxy/IIS para `https://api.valoragroup.mnsoft.com.br` na porta interna `8097`.
7. Teste `/health`, `/communication/status` e `/communication/email/test`.

Para desativar temporariamente, altere o frontend para `EMAIL_TRANSPORT: 'disabled'` e `COMMUNICATION_GATEWAY.enabled: false`; a pesquisa continua finalizando.
