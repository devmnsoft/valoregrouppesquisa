# Deploy Windows — Communication Gateway

1. Copie `communication-gateway\.env.production.example` para `.env`.
2. Configure SMTP real em `.env` e a variável `GOOGLE_APPLICATION_CREDENTIALS` apontando para a chave do Firebase Admin no servidor.
3. Execute `npm install --omit=dev` dentro de `communication-gateway`.
4. Valide com `node tests\run-tests.js` e `curl http://localhost:8097/health`.
5. Instale como serviço com NSSM conforme `communication-gateway\tools\windows\06-instalar-servico-nssm.md`.
6. Publique por DNS/reverse proxy em `https://api.valoragroup.mnsoft.com.br`.

O endpoint principal é `POST /communication/result/send` com `{ responseId, resultToken, channels }`. O gateway busca a resposta no Firestore com Firebase Admin, valida `resultToken`, monta o e-mail e registra a comunicação.
