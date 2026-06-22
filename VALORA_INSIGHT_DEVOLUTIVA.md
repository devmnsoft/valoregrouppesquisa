# Valora Insight™ — Devolutiva Estratégica

## Objetivo
A Devolutiva Inteligente Valora Insight™ transforma respostas do diagnóstico em leitura estratégica executiva, direta e sem suavização da realidade. Ela é parte central do produto: não é texto complementar.

## Dimensões oficiais
O diagnóstico principal usa exatamente 5 dimensões, com 5 perguntas por dimensão:

1. Cultura e Propósito
2. Gestão e Governança
3. Liderança
4. Pessoas e Talentos
5. Resultados e Crescimento

Cada pergunta usa escala de 1 a 5. Cada dimensão soma até 25 pontos. O total máximo oficial é 125 pontos.

## Tratamento de ESG
Sustentabilidade e ESG não entram no cálculo principal do Valora Insight™. Respostas antigas com ESG ou máximo 135 são normalizadas: o cálculo principal ignora ESG, recalcula as 5 dimensões oficiais para 125 pontos e preserva o dado original em `legacyScore` quando necessário.

## Níveis oficiais
- 🔴 Crítico: 25–55
- 🟡 Em estruturação: 56–85
- 🟢 Estruturada: 86–110
- 🔵 Alta maturidade: 111–125

## Estrutura da devolutiva
A função `generateValoraInsightDevolutiva(result, form, response)` gera uma estrutura rica com:

- Enquadramento geral sem adoçamento
- Leitura executiva da realidade
- Diagnóstico por dimensão
- Radar organizacional textual
- Benchmarking qualitativo
- Verdade estratégica central
- Risco se nada mudar
- Próximo nível de evolução
- Transição natural para solução
- Princípio final obrigatório

## Radar textual
O radar usa 10 blocos por dimensão:

```text
Cultura e Propósito: ████████░░ 18/25
Gestão e Governança: ██████░░░░ 12/25
Liderança: ███████░░░ 16/25
Pessoas e Talentos: ██████░░░░ 14/25
Resultados e Crescimento: ████████░░ 20/25
```

## Benchmarking qualitativo
O benchmarking é estrutural e qualitativo. Ele compara a empresa com padrões de maturidade de:

- Empresas iniciantes
- Empresas em crescimento
- Empresas maduras

A aplicação não inventa números oficiais nem usa estatísticas externas falsas.

## Regras de tom
A devolutiva deve ser direta, humana, executiva, sem jargão excessivo, sem elogio vazio e sem frases bajuladoras como “Parabéns pelo excelente resultado”. O texto traduz dados em verdade organizacional.

## Onde aparece
- Página de resultado do participante
- Área do participante em “Pesquisas respondidas” ao abrir o resultado
- Admin > Respostas > Ver resultado
- Painel da empresa em respostas consolidadas, ao abrir resultado individual
- Relatório PDF estratégico
- Certificado permanece como conclusão; o relatório estratégico aprofunda a devolutiva

## Como validar
1. Responda o Valora Insight™ com soma total 72/125.
2. O nível deve ser “Em estruturação”.
3. A página deve conter “VALORA INSIGHT™ — DEVOLUTIVA ESTRATÉGICA”.
4. O radar textual deve aparecer.
5. A verdade estratégica, o risco se nada mudar, o próximo nível e o CTA devem aparecer.
6. Não deve aparecer `undefined`, `NaN` ou pontuação acima de 125.

## Princípio final obrigatório
```text
Empresas não evoluem quando entendem o diagnóstico.
Elas evoluem quando aceitam a verdade que ele revela.
```
