# Relatório de Migração

Este arquivo é o modelo de saída para a migração Firebase -> PostgreSQL.

## Contagens a validar
- plans
- organizations
- users
- forms
- surveys
- responses
- answers
- certificates
- communications

## Regras
- Não migrar senhas em texto puro.
- Preservar `firebase_uid` para ponte de autenticação.
- Validar scores antes de ativar backend como fonte oficial.
