# Diagnóstico inicial — Sprint Backend Centralização Valora Insight

1. Legado: `index.html`, `app.js` e serviços JS concentram a jornada pública, menus, perfis, LGPD, certificado, PDF e devolutiva. As perguntas reais estão em `valoraInsightForm()` no `app.js`.
2. Backend novo: `backend/Valora.sln` já contém API, Application, Domain, Infrastructure, Tests e Web MVC ASP.NET.
3. Jornada pública pendente: paridade visual fina com o site publicado e validação integral em mobile/desktop.
4. Diagnóstico gratuito pendente: remover seeds genéricos e oficializar 25 perguntas reais no PostgreSQL.
5. Área administrativa pendente: ampliar guardas por perfil e validar menu por papel.
6. Menus hoje: `_PublicLayout` separado do `_AdminLayout`; admin usa `_Sidebar`.
7. Perfis hoje: definidos no backend e em `role-definitions.js`, com `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa`, `analista_resultados`, `gestor_area`, `participante` e `convidado_externo`.
8. Filtros hoje: parte dos repositórios aplica `organization_id`; regras precisam continuar no backend.
9. SQL hoje: há script completo e seeds modulares em `backend/database/postgresql`.
10. Seed de perguntas hoje: `012_seed_demo_valora_insight.sql` usava perguntas genéricas; a sprint substitui por perguntas reais.
11. Seed de admin hoje: script completo possui `admin@valoragroup.local` com hash BCrypt local/homologação.
12. Devolutiva hoje: calculadora inicial existia com textos simples.
13. PDF exige: enquadramento geral, leitura executiva, dimensão, radar, benchmarking, verdade estratégica, risco, próximo nível, transição e CTA.
14. Link público exige: diagnóstico gratuito com token público, LGPD, identificação, 5 dimensões e resultado instantâneo.
15. Segurança: credenciais Firebase/service account não podem ser versionadas, expostas em `wwwroot`, SQL, backend ou documentação; recomenda-se revogar/rotacionar a chave compartilhada fora do fluxo seguro.
16. Plano: centralizar seed oficial no PostgreSQL, reforçar devolutiva determinística, validar menu/jornada/segredos, documentar e testar comandos obrigatórios.
