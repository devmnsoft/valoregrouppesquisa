# Cadastro empresarial transacional

Toda escrita que integra o cadastro deve compartilhar `IUnitOfWork.Connection` e `IUnitOfWork.Transaction`. O handler é o dono do limite transacional; controllers apenas traduzem HTTP. Sessão e tokens pertencem à fase pós-commit. Em exceção ou descarte sem commit, `DapperUnitOfWork` executa rollback.

A idempotência usa chave única e hash canônico; senha é transformada pelo `IPasswordHasher` antes de qualquer persistência. Conflitos de mesma chave com hash diferente devem resultar em HTTP 409.
