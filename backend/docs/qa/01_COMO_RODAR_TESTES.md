# Como rodar

Pré-requisito: SDK definido por `global.json` (atualmente .NET 10) e PowerShell 7 para os scripts `.ps1`.

```powershell
cd backend
dotnet restore Valora.sln
dotnet test Valora.sln -c Release
./scripts/qa/run_tests.ps1
./scripts/qa/run_sql_static_checks.ps1
./scripts/qa/run_release_checks.ps1 -SkipExternalIntegration
```

`run_release_checks.ps1` limpa, restaura, compila, testa, publica API/Web e valida o SQL; qualquer etapa crítica encerra com código diferente de zero. `-SkipExternalIntegration` documenta a intenção da execução local e é reservado para integrações opcionais; não pula os testes críticos.

## PostgreSQL opcional

Defina `VALORA_TEST_POSTGRES_CONNECTION` somente para uma base isolada de QA. O nome da base deve identificar teste/homologação. A suíte nunca deve receber connection string de produção e não executa `DROP DATABASE` ou limpeza destrutiva. Sem a variável, testes de banco são ignorados; testes unitários e estáticos continuam executando.
