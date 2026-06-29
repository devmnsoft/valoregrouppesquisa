# Painel operacional de diagnósticos gratuitos

O Valora.Web ASP.NET Core possui `/FreeDiagnostics` com Bootstrap, jQuery e AJAX. A página consome a Valora.Api por HTTP via `free-diagnostics-api.js` e não acessa banco, Dapper, EF, repositories ou Firebase.

Indicadores: total respondidos, hoje, mês, e-mails enviados, pendentes, processando, falhos, certificados gerados e links compartilhados. Filtros: data inicial/final, nome, e-mail, status de e-mail, maturidade e status do certificado. Ações: ver resultado, ver certificado, copiar links, reenviar e-mail, regenerar certificado, abrir WhatsApp por clique e marcar falha como revisada.
