# Fluxo de autenticação BFF

1. O navegador envia JSON e antiforgery para `/bff/auth/*`.
2. `BffApiClient`, um `HttpClient` tipado, comunica-se com `/api/v1/auth/*`.
3. `BffAuthenticationService` guarda access/refresh tokens exclusivamente no store server-side.
4. O cookie `__Host-Valora.Session`, Secure e HttpOnly, transporta o ticket protegido pelo Data Protection.
5. O navegador recebe somente `BffSafeSession`; `/bff/auth/me` confirma a sessão.
6. Logout revoga na API, apaga o estado no servidor e encerra o cookie.

O store em memória é adequado ao desenvolvimento em nó único. Produção horizontal deve substituí-lo por store distribuído protegido, com TTL e revogação atômica.
