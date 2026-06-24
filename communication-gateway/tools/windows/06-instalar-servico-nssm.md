# Instalar Communication Gateway como serviço Windows com NSSM

1. Copie `.env.production.example` para `.env` e preencha SMTP real, `GOOGLE_APPLICATION_CREDENTIALS` e origens permitidas.
2. Execute `npm install --omit=dev` em `C:\DBBACK\valoregrouppesquisa\communication-gateway`.
3. Instale com NSSM:
   `nssm install ValoraCommunicationGateway "C:\Program Files\nodejs\node.exe" "C:\DBBACK\valoregrouppesquisa\communication-gateway\server.js"`.
4. Configure AppDirectory para `C:\DBBACK\valoregrouppesquisa\communication-gateway`.
5. Inicie com `nssm start ValoraCommunicationGateway` e valide `/health`.
