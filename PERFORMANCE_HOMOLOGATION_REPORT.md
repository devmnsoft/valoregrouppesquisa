# Performance Homologação — Valora Backend Oficial

Data: 2026-07-02
Versão: 0.9.0-rc1

## Ambiente
A medição real ficou pendente neste container por ausência de .NET SDK, Docker e PostgreSQL. Este relatório define os cenários mínimos e o método de coleta para a execução em estação de homologação completa.

## Cenários mínimos
| Cenário | Volume | Métrica esperada | Status |
|---|---:|---|---|
| Respostas públicas | 100 | média, máximo, erros | Pendente ambiente completo |
| Respostas públicas | 1.000 | média, máximo, erros | Pendente viabilidade |
| Dry-run de importação | arquivo médio sanitizado | duração e conflitos | Pendente ambiente completo |
| Listagem paginada | dados simulados | tempo por página | Pendente ambiente completo |
| Dashboard | dados simulados | tempo total | Pendente ambiente completo |
| Exportação CSV | dados simulados | duração e tamanho | Pendente ambiente completo |
| Relatório por pesquisa | dados simulados | duração | Pendente ambiente completo |

## Critérios de ação
Se houver endpoints lentos, aplicar paginação, revisar queries Dapper, criar índices idempotentes e registrar evidência antes de homologação com usuários piloto.
