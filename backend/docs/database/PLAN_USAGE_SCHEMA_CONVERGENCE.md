# Convergência do schema de consumo

O modelo oficial usa `metric_key`, `quantity`, `consumed` e `reserved`. A migração `20260801_007_schema_convergence_and_registration.sql` detecta instalações históricas, copia `resource_code`, `amount` e `used_value`, cria as colunas e índices canônicos e somente então remove as colunas antigas, dentro de uma transação.

O bootstrap contém uma única definição de cada tabela e cria a unicidade global parcial do CNPJ ativo.
