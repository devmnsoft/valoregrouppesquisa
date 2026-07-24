# IIS Publish Checklist
1. `dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj -c Release -o publish/windows`.
2. Confirmar SDK/Hosting Bundle compatível.
3. Configurar app pool sem secrets em arquivos versionados.
4. Aplicar `database/migrate.sql`.
5. Validar `/`, `/login`, `/superadmin/system-health`.
6. Não commitar `publish/`, `bin/`, `obj/` ou backups.
