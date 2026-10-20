# RELATÓRIO OFICIAL DE HOMOLOGAÇÃO INTEGRADA E OPERACIONAL (SPRINT R7)
## Valora Insight™ — Release 1.0 Candidate

**Data da Homologação:** 20/10/2026  
**Ambiente de Validação:** PostgreSQL 16 (`sigov-postgres` em `localhost:5433`) + .NET 10  
**Status Geral:** **APROVADO (100% dos testes operacionais e de integração)**

---

## 1. Sumário Executivo de Testes

| Métrica | Resultado | Status |
| :--- | :---: | :---: |
| **Total de Testes na Suíte** | **723** | Concluído |
| **Testes Aprovados** | **723** | **100.0%** |
| **Testes com Falha** | **0** | **0.0%** |
| **Testes Ignorados (Skipped)** | **0** | **0.0%** |
| **Duração Total da Suíte** | ~40 segundos | Nominal |

---

## 2. Endpoints de Integridade e Observabilidade Validados

| Endpoint | Protocolo | Verificações de Saúde Realizadas | Resultado |
| :--- | :--- | :--- | :---: |
| `/health` | GET | Status básico de serviço e migration baseline | `200 OK` |
| `/health/database` | GET | Conectividade ativa com PostgreSQL (`SELECT 1;`) | `200 OK` |
| `/health/ready` | GET | Database probe, backlog de `email_jobs` (pendente/falha), backlog de `intelligence_processing_jobs`, storage provider, PDF renderers | `200 OK` |
| `/health/logging` | GET | Verificação de gravação Serilog e emissão com `CorrelationId` | `200 OK` |
| Falha de Conexão Simulada | GET | Retorno de `503 Service Unavailable` estruturado sem expor stack traces ou dados sensíveis | `503 Service Unavailable` |

---

## 3. Testes de Concorrência e Isolamento Transacional

- **Mecanismo de Lock em Fila:** Testado via `FOR UPDATE SKIP LOCKED` contra a tabela `valorapesquisa.intelligence_processing_jobs`.
- **Cenário Executado:** 5 threads/workers concorrentes disputando 10 jobs pendentes.
- **Resultado:** Zero processamento duplicado (*double processing*). Cada worker reivindicou exclusivamente seu lote e transitou o status para `running` com carimbo de `locked_at` e `locked_by`.
- **Isolamento de Tenants Concorrentes:** Duas organizações distintas submetendo e consultando jobs em paralelo no mesmo milissegundo demonstraram isolamento estrito — nenhum registro da Organização A vazou para a Organização B.

---

## 4. Tolerância a Falhas e Recuperação (Disaster Recovery)

- **Cenário de Queda Repentina do Worker:** Job interrompido no estado `running` (simulando timeout ou reinício de container).
- **Tratamento:** Transição atômica para `retry_scheduled` com registro do código de erro (`TRANSIENT_TIMEOUT`), liberação do bloqueio (`locked_by = NULL`, `locked_at = NULL`), e preservação do histórico de tentativas (`attempts` incrementado até o limite de `max_attempts = 3`).
- **Garantia de Idempotência:** O índice único condicional `ux_intelligence_processing_active_key` em `(organization_id, idempotency_key)` impede duplicidade de processamento ativo mesmo diante de múltiplos disparos idênticos.

---

## 5. Validação de Exportações e Fidelidade de Dados

- **Exportação CSV:**
  - Codificação UTF-8 com BOM.
  - Sanitização de injeção de fórmulas de planilhas (`=` `@` `+` `-` prefixados com aspas simples de escape).
  - Fidelidade numérica absoluta com valores calculados (ex: `raw_score` 85.0 e `maturity_level` 4.25 exportados sem distorção).
- **Exportação XLSX:**
  - Pacote OpenXML SpreadsheetML válido (`xl/workbook.xml`, `xl/worksheets/sheet1.xml`).
  - Cabeçalhos e células estruturadas gerados sem dependência de drivers COM ou ferramentas de terceiros proprietárias.
- **Exportação PDF & JSON:**
  - Assinatura binária canônica `%PDF`.
  - JSON estruturado com metadados de filtros, evidências e remoção estrita de campos sensíveis de identificação de colaboradores (PII).
- **Controle de Integridade:** Hash SHA-256 gerado e carimbado em cada artefato exportado.
