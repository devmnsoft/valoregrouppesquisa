# Guia de execução local

## JWT de desenvolvimento

`Valora.Api/appsettings.Development.json` contém uma chave **fake e exclusiva para desenvolvimento**, marcada com `DEV_ONLY_`. Ela permite iniciar a API localmente, mas nunca deve ser copiada para homologação ou produção. `Jwt:SigningKey` não pode estar ausente, em branco ou ter menos de 32 caracteres.

Para substituir a chave local sem alterar arquivos versionados, use User Secrets:

```bash
cd backend/Valora.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "UMA_CHAVE_LOCAL_ALEATORIA_COM_32_OU_MAIS_CARACTERES"
```

## Checklist de startup

```bash
cd backend
dotnet clean Valora.sln
dotnet restore Valora.sln
dotnet build Valora.sln --no-restore
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Valora.Api
```

1. Confirme que o PostgreSQL da connection string está acessível e que `script_completo.sql` foi aplicado.
2. Consulte `/health`, `/health/database` e, autenticado, `/api/v1/system-health`.
3. Não contorne a validação JWT. Se ela falhar, configure `Jwt:SigningKey` por User Secrets ou por `Jwt__SigningKey`.

`/health/config` apresenta somente estados sanitizados (`configured`, `missing`, `invalid` ou `not_configured`) para JWT, PostgreSQL, e-mail, PDF e storage. A chave JWT e as demais credenciais nunca fazem parte da resposta. Em produção, uma chave iniciada por `DEV_ONLY_` aparece como `invalid` e a inicialização permanece bloqueada.
