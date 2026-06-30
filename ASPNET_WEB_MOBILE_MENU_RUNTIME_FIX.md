# Correção runtime menu mobile Valora.Web

## Status Sprint 53
- Projeto antigo da raiz preservado e mantido com Firebase como provedor principal.
- Pesquisa gratuita oficial deve usar `publicToken` em URL pública e nunca `tokenHash` como token compartilhável.
- Expiração da pesquisa gratuita é tolerante no runtime e corrigida para longo prazo pelo repair seguro.
- Cadastro de cliente e usuário possui contratos estruturais validados.
- Menu administrativo mobile possui funções explícitas, overlay, ARIA e fechamento por ESC/resize/item.
- Valora.Web permanece ASP.NET Core MVC/Razor Pages com Bootstrap, JavaScript, jQuery e AJAX.

## Riscos restantes
- Execução com dados reais depende de credenciais Firebase Admin e homologação em ambiente controlado.
- Cloud Functions precisam ser implantadas após aprovação.
