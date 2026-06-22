# Planos e Precificação — Valora Pulse™ / Valora Insight™

## Estratégia de preço

O Valora Insight™ é posicionado como SaaS B2B de diagnóstico de maturidade organizacional com devolutiva estratégica automatizada, relatórios executivos e entrada natural para consultoria de evolução empresarial. A precificação não compara o produto a formulários genéricos; ela combina três camadas de valor:

1. **SaaS recorrente** para operação, coleta, portais, relatórios, certificados e gestão de limites.
2. **Diagnóstico estratégico com devolutiva** baseado em 5 dimensões, 25 perguntas e 125 pontos.
3. **Serviços consultivos opcionais** para aprofundar decisões, reuniões executivas e transformação organizacional.

## Comparação com mercado

Ferramentas simples de pesquisa cobram pelo formulário, resposta ou usuário. O Valora cobra pela leitura estratégica: radar de maturidade, benchmarking qualitativo, verdade estratégica central, risco se nada mudar, próximo nível de evolução e plano de ação. Por isso, os preços foram definidos para preservar percepção consultiva e não competir com Google Forms, Typeform básico ou SurveyMonkey simples.

## Planos oficiais

| Plano | Preço mensal | Anual sugerido | Setup recomendado | Uso ideal |
|---|---:|---:|---:|---|
| Essencial | R$ 697/mês | R$ 6.970/ano | R$ 990 | Primeira rodada estruturada de maturidade |
| Profissional | R$ 1.497/mês | R$ 14.970/ano | R$ 2.500 | Plano recomendado para RH, diretoria e consultorias |
| Corporativo | R$ 3.997/mês | R$ 39.970/ano | R$ 6.000 | Múltiplas áreas, unidades e governança contínua |
| Enterprise | Sob consulta | Sob contrato | R$ 15.000 a R$ 45.000 | Projetos estratégicos com implantação assistida |

## Limites

| Limite | Essencial | Profissional | Corporativo | Enterprise |
|---|---:|---:|---:|---:|
| Pesquisas ativas | 3 | 12 | Ilimitadas | Conforme contrato |
| Respostas/mês | 150 | 1.000 | 10.000 | Conforme contrato |
| Gestores | 2 | 8 | 50 | Conforme contrato |
| Empresas/unidades | 1 | 1 | 5 | Conforme contrato |
| Áreas/departamentos | 1 | 5 | 30 | Conforme contrato |

## Diagnóstico Avulso Valora Insight™

Produto de entrada separado dos planos mensais:

- **Preço:** R$ 497.
- **Inclui:** 1 pesquisa Valora Insight™, até 30 respostas, resultado individual, devolutiva estratégica básica, certificado e validade de 15 dias.
- **Quando usar:** lead qualificado, prova de valor, primeira conversa comercial ou diagnóstico pontual.
- **Regra comercial:** deve aparecer em seção separada (“Quer começar com uma única avaliação?”) para não competir com assinatura recorrente.

## Serviços adicionais

| Serviço | Preço | Inclui |
|---|---:|---|
| Devolutiva Executiva Assistida | R$ 1.500 | Análise humana, reunião online de até 90 minutos e orientações estratégicas |
| Workshop de Maturidade Organizacional | R$ 3.500 | Leitura do radar, priorização de gargalos e direção de evolução |
| Diagnóstico Profundo por Área | R$ 6.000 a R$ 12.000 | Diagnóstico por área, entrevistas, relatório executivo e plano de evolução |
| Projeto de Estruturação Organizacional | A partir de R$ 35.000 | Diagnóstico, governança, liderança, rotina de gestão, plano de ação e acompanhamento |

## Regras da vitrine pública

- Exibir apenas planos com `visibleOnPublicPricing: true`, `internalOnly: false` e `isDemo !== true`.
- Não exibir Gratuito, Free, Demo ou Teste publicamente.
- O plano Profissional deve aparecer como **Mais recomendado**.
- O Diagnóstico Avulso pode aparecer, mas em seção separada dos planos recorrentes.

## Regras internas

Planos internos podem existir para seed, homologação, demonstração ou testes, desde que marcados com:

```js
internalOnly: true,
isDemo: true,
visibleOnPublicPricing: false
```

## Quando vender consultoria

Vender serviço consultivo quando o cliente pedir interpretação executiva, priorização de gargalos, plano de evolução, alinhamento de liderança, workshops, múltiplas áreas ou acompanhamento mensal de transformação.

## Quando migrar para Blaze

O projeto está preparado para Firebase Spark com:

```js
FIREBASE_PLAN: 'spark',
ENABLE_CLOUD_FUNCTIONS: false
```

Enquanto estiver no Spark, não vender como ativos: e-mail transacional real, webhooks reais, integrações server-side, Secret Manager ou processamento backend. Esses recursos devem ser tratados como **disponíveis quando migrar para Blaze**.
