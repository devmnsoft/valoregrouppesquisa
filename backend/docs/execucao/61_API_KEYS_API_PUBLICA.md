# API Keys e API Pública

## Implementado nesta execução

A emissão usa 256 bits aleatórios, devolve o token somente na resposta de criação e persiste exclusivamente SHA-256, prefixo, escopos, expiração e autor. O segredo deixou de atravessar o contrato de persistência produtivo. Nome, expiração futura e escopos permitidos são validados.

Foram consolidadas consulta individual, listagem, revogação, rotação e histórico de uso. A rotação revoga a chave anterior antes de emitir a substituta. O endpoint legado de criação foi encerrado com `410`; o fluxo autorizado é `/api/v1/api-keys` e exige entitlement Enterprise.

## Pendência

Os endpoints públicos autenticados por API key ainda não foram liberados: falta middleware dedicado, rate limiting distribuído e homologação de consultas agregadas. Nenhuma API pública insegura foi exposta.
