# Runbook de cadastro empresarial

## Pré-condições
Validar CNPJ e consentimentos, consultar a fonte econômica sem registrar documento completo e aplicar política de senha.

## Transação alvo
Criar organização, pessoa jurídica, unidade principal, administrador, role, assinatura Gratuita, módulos, configuração, branding, onboarding, consentimento, consumo e outbox em uma única transação. Qualquer falha exige rollback integral.

## Situação
O value object CNPJ e o schema-base existem; o serviço transacional e sua prova PostgreSQL permanecem pendentes. Não operar este fluxo em produção.
