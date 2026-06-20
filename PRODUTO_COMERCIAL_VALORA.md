# Produto comercial Valora Pulse

Esta evolução consolida a base SaaS multiempresa do Valora Pulse: perfis, módulos, planos, limites, cadastro de empresas, funcionários/respondentes e controles de acesso.

## Fluxo comercial recomendado

1. `admin_valora` cadastra plano e módulos liberados.
2. `admin_valora` cadastra a empresa com dados fiscais/contato/endereço.
3. `admin_valora` cria o administrador principal da empresa (`empresa_admin`).
4. `empresa_admin` cadastra funcionários com perfil e departamento quando aplicável.
5. `gestor_pesquisa` cria formulários/pesquisas e envia convites se o plano permitir.
6. `analista_resultados` acompanha respostas e relatórios.
7. `gestor_area` consulta apenas dados da própria área.
8. `participante` e `convidado_externo` respondem pesquisas sem acesso administrativo.

## Segurança multiempresa

- `companyId` delimita escopo operacional.
- Perfis globais não podem ser criados por empresas.
- Firestore Rules reconhecem `analista_resultados`, `gestor_area` e `convidado_externo`.
- Respostas públicas continuam bloqueadas no Firestore direto e passam por Cloud Function com token.
- Logs continuam sem escrita direta pelo frontend.

## Próximo passo sugerido

Finalizar Firebase Repository com dados reais do Firestore.
