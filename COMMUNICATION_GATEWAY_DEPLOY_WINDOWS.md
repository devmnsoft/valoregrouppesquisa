# Deploy Windows Server — Communication Gateway

1. Instale Node.js LTS 20+ em https://nodejs.org.
2. Copie `communication-gateway` para `C:\DBBACK\valoregrouppesquisa\communication-gateway`.
3. Execute `npm install`.
4. Copie `.env.example` para `.env` e ajuste SMTP, token e WhatsApp.
5. Rode em teste: `npm run start` na porta `8097`.
6. PM2: `npm i -g pm2` e `pm2 start server.js --name valora-communication-gateway`.
7. NSSM: instale o serviço apontando para `node.exe` com argumento `server.js` no diretório do gateway.
8. IIS: instale URL Rewrite + ARR, crie site/binding `https://api.valoragroup.mnsoft.com.br` e proxy reverso para `http://127.0.0.1:8097`.
9. Libere firewall TCP 443 público e 8097 apenas local/restrito.
10. Configure certificado SSL no binding IIS.
11. Teste: `curl https://api.valoragroup.mnsoft.com.br/health`.
