# Contrato do Communication Gateway

Base URL de produção: `https://api.valoragroup.mnsoft.com.br`.

Headers obrigatórios para endpoints protegidos:
- `Authorization: Bearer [GATEWAY_API_TOKEN]`
- `Content-Type: application/json`

Endpoints:
- `GET /health` público.
- `GET /communication/status` protegido.
- `POST /communication/result/send` protegido, envia o e-mail transacional de resultado.
- `POST /communication/email/send` protegido, reservado; retorna JSON controlado.

O payload principal contém participante, empresa, pesquisa, resultado e links. Erros sempre retornam JSON e nunca HTML.
