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

## Fluxo comercial operacional Firebase

A jornada SaaS validada para homologação é: seed mínimo, primeiro `admin_valora`, criação de organização, criação do admin da empresa, cadastro de funcionários, formulário, pesquisa, convite por e-mail via Cloud Function, resposta por link seguro, cálculo backend, dashboards, respostas, relatórios/certificados e auditoria. O modo local/demo continua disponível para demonstração com outbox local.

## Evolução comercial — onboarding guiado

A experiência comercial passa a ter uma jornada prescritiva no dashboard da empresa. O componente **Primeiros passos da implantação** mostra percentual concluído, etapa atual, próximo passo, alertas de plano/perfil e CTAs contextuais para dados da empresa, plano, administrador, funcionários, questionários, pesquisas, convites, respostas e relatórios.

O botão **Configurar minha primeira pesquisa** abre um wizard de seis etapas: revisão cadastral, cadastro rápido de funcionários, escolha/criação do questionário, configuração da pesquisa com LGPD, preparação do envio e conclusão. Quando o cliente não possui questionário, o fluxo cria o **Diagnóstico Essencial de Maturidade** com dimensões de Cultura, Processos e Liderança.

O Admin Valora passa a enxergar o **Status de implantação do cliente** em visão global, incluindo empresas novas, em configuração, com funcionários, com pesquisa criada, com convites, com respostas, implantadas e travadas. Isso melhora demonstrações, CS e priorização comercial sem remover funcionalidades existentes.

Estados vazios inteligentes foram adicionados para funcionários, formulários, pesquisas, respostas e relatórios, sempre com orientação e CTA para a próxima tela. O ValoraBot também responde perguntas de onboarding como “Como começo?”, “Qual próximo passo?” e “Por que meu dashboard está vazio?” considerando o estágio real da empresa.
