# Estratégia de testes

A suíte `Valora.Tests` usa xUnit e prioriza regras com maior impacto: autenticação/JWT, isolamento por organização, LGPD, fluxo público, entitlements, inteligência, governança, contratos HTTP e segurança do SQL canônico. Testes unitários não usam rede nem banco. Testes de host usam configuração isolada. Testes que acessam PostgreSQL são opcionais e somente podem usar `VALORA_TEST_POSTGRES_CONNECTION` apontando para banco cujo nome contenha `test`, `teste`, `homolog` ou `qa`.

## Pirâmide

1. **Unitário/contrato:** determinístico, fakes e inspeção de assembly/source quando a fronteira é arquitetural.
2. **Host/API:** `WebApplicationFactory`, sem serviço externo real.
3. **SQL estático:** bloqueia operações destrutivas e ausência de contratos críticos.
4. **PostgreSQL opcional:** valida idempotência somente quando explicitamente configurado; nunca remove o banco.

Nenhum teste pode registrar credencial, token, senha, secret ou dado pessoal real. Falha de integração externa deve ser representada por fake e não tornar a suíte local dependente de Docker.
