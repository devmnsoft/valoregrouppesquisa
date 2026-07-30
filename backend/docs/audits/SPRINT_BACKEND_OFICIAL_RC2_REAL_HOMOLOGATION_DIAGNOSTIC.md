# Diagnóstico Inicial — Sprint Backend Oficial RC2 Real Homologation

1. **Projeto novo em `backend`**: `backend/Valora.sln` contém API, Application, Domain, Infrastructure, Tests e Web MVC/Razor como base oficial.
2. **Legado da raiz**: preservado como referência funcional em Bootstrap/JavaScript/Firebase, iniciado por `index.html`; não foi alterado nesta etapa diagnóstica.
3. **`projeto .NET predecessor removido`**: permanece referência histórica e fora do build oficial.
4. **Scripts SQL**: scripts oficiais estão em `backend/database/postgresql` e no `script_completo.sql` raiz; arquivos de archive contêm histórico e não são build oficial.
5. **Schema de `plans`**: schema oficial usa `id` UUID e `code` como chave natural, sem colunas legadas de apresentação.
6. **Seed de planos**: seed oficial usa `ON CONFLICT (code)` e valores compatíveis com o schema real.
7. **Validador `backend:sql-schema-validate`**: existente no `package.json` e responsável por bloquear regressões de schema/seed de planos.
8. **`.NET SDK`**: `dotnet` não está instalado neste container (`dotnet: command not found`).
9. **PostgreSQL local/homologação**: Docker não está instalado neste container (`docker: command not found`), portanto o PostgreSQL descartável não pôde subir aqui.
10. **`dotnet restore`**: pendente/impossibilitado inicialmente por ausência do SDK.
11. **`dotnet build`**: pendente/impossibilitado inicialmente por ausência do SDK.
12. **`dotnet test`**: pendente/impossibilitado inicialmente por ausência do SDK.
13. **Validadores Node**: scripts oficiais existem; serão executados e corrigidos quando necessário.
14. **API**: código e health controllers existem; runtime real depende de SDK .NET indisponível neste container.
15. **Web**: Web oficial existe em `backend/Valora.Web`; runtime real depende de SDK .NET indisponível neste container.
16. **Health checks**: rotas `/health*` e telas `/Operations/*` estão presentes no código.
17. **Fluxos críticos**: público, administrativo, importação e backup/restore estão implementados/documentados, mas validação runtime completa depende de SDK/PostgreSQL.
18. **Gaps existentes**: ambiente sem .NET SDK, sem Docker/PostgreSQL e sem validação HTTP runtime completa.
19. **Riscos de produção**: falta de homologação com usuários piloto reais, necessidade de PostgreSQL real e confirmação de SMTP/storage/backup em ambiente controlado.
20. **Plano objetivo da sprint**: executar validadores disponíveis, corrigir falhas reais detectadas, registrar limitações de ambiente, adicionar documentação/validador RC2 e preparar RC2 sem alterar legado ou `projeto .NET predecessor removido`.
