# Convergência de bancos legados

A convergência copia `resource_code` para `metric_key`, `used_value` para `consumed` e `amount` para `quantity`, valida a preservação e somente então remove colunas obsoletas. Duplicidades empresariais abortam a transação; não há exclusão silenciosa de registros de negócio.
