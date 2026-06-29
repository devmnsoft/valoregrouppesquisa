# ASPNET Web API Gaps

## 1. Endpoints existentes e consumidos pelo Valora.Web
- módulo: Dashboard; tela afetada: Dashboard; endpoint: `/me`, `/health`, `/health/database`, `/organization/current/usage`, `/organization/current/limits`, `/surveys`, `/responses`; método: GET; payload: n/a; resposta esperada: JSON seguro; fallback atual: card com `data-gap-controlled="true"`; bloqueia produção: não; sprint prevista: 38.
- módulo: Organização; tela afetada: Organização; endpoint: `/organization/current`; método: GET/PUT; payload: dados públicos e preferências; resposta esperada: organização sanitizada; fallback atual: card controlado; bloqueia produção: não; sprint prevista: 38.
- módulo: Usuários; tela afetada: Usuários; endpoint: `/users`; método: GET/POST/PUT/PATCH; payload: usuário e status; resposta esperada: usuário sem hashes; fallback atual: card controlado; bloqueia produção: não; sprint prevista: 38.
- módulo: Formulários; tela afetada: Formulários; endpoint: `/forms`; método: GET/POST/PUT; payload: título, descrição, dimensões; resposta esperada: formulário; fallback atual: card controlado; bloqueia produção: não; sprint prevista: 38.
- módulo: Pesquisas e links; tela afetada: Pesquisas/Links; endpoint: `/surveys`, `/surveys/{surveyId}/links`, `/survey-links/{linkId}/status`; método: GET/POST/PUT/PATCH; payload: pesquisa/link/status; resposta esperada: dados sanitizados; fallback atual: card controlado; bloqueia produção: não; sprint prevista: 38.
- módulo: Respostas; tela afetada: Respostas; endpoint: `/responses`, `/responses/{responseId}`, `/responses/{responseId}/result`; método: GET; payload: filtros; resposta esperada: respostas sem token/hash; fallback atual: card controlado; bloqueia produção: não; sprint prevista: 38.
- módulo: Auditoria/Configurações; tela afetada: Auditoria/Configurações; endpoint: `/audit/events`, `/settings`; método: GET/PUT; payload: filtros/preferências; resposta esperada: JSON seguro; fallback atual: card controlado; bloqueia produção: não; sprint prevista: 38.

## 2. Endpoints existentes e ainda não consumidos
- módulo: certificados; tela afetada: certificado; endpoint: `/responses/{responseId}/certificate.pdf`; método: GET; payload: n/a; resposta esperada: binário via streaming; fallback atual: link seguro; bloqueia produção: não; sprint prevista: 39.

## 3. Endpoints faltantes bloqueantes
Nenhum bloqueante conhecido após criação dos endpoints internos mínimos em `WebAdminModulesController`.

## 4. Endpoints faltantes não bloqueantes
- módulo: Configurações; tela afetada: senha/notificações avançadas; endpoint: `/settings/password`; método: PUT; payload: senha atual e nova; resposta esperada: confirmação segura; fallback atual: gap controlado; bloqueia produção: não; sprint prevista: 39.

## 5. Fallbacks temporários permitidos
Cards e alertas com `data-gap-controlled="true"`, sem JSON bruto, sem stack trace e com correlationId sanitizado.

## 6. Fallbacks temporários proibidos
Mocks silenciosos, dumps JSON em `<pre>`, exposição de token/hash/senha/connection string, console com payload sensível e cutover automático para API em produção.
