# Enterprise — Central de Integrações

## Implementado nesta execução

A central usa o catálogo real da API, isolado pelo `organization_id` do usuário autenticado e protegido pelo entitlement Enterprise. O catálogo passou a incluir API Pública, API Keys, Webhooks, Power BI, exportações, SMTP, certificados/PDF, Executive Report/PDF e Importação Assistida.

A configuração agora persiste o código do conector, rejeita campos que aparentem conter token, senha, API key ou secret e produz auditoria pelo serviço Enterprise. Desativação é uma operação explícita. O teste de conexão só aceita conector configurado e, sem adaptador real, retorna estado `501` honesto em vez de simular sucesso.

## Operação

* `GET /api/v1/integrations` lista o catálogo.
* `GET /api/v1/integrations/{code}` consulta o item.
* `PATCH /api/v1/integrations/{code}` grava apenas configuração não sensível.
* `POST /api/v1/integrations/{code}/disable` desativa.
* `POST /api/v1/integrations/{code}/test` nunca simula um provedor.
