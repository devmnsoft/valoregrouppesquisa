# Sprint Valora Insight™ — Diagnóstico inicial de correção fina mobile

## Achados
1. `Valora Pulse` ainda aparecia em textos da Web autenticada, API/Application e documentação; textos públicos foram normalizados para `Valora Insight™`.
2. `Valora Insight™` faltava em pontos de certificado, e-mail transacional e resultado público.
3. `HOME` não estava no topo público principal, mas o validador agora bloqueia `HOME`/`Home` visível em views públicas.
4. `Invalid date` não apareceu como literal, porém datas eram renderizadas sem formatação segura em scripts de resultado.
5. A data do diagnóstico não era exibida com fallback amigável no resultado público.
6. Botões de WhatsApp apontavam para `/whatsapp`, `wa.me` sem número ou número placeholder.
7. O resultado podia estourar largura mobile por falta de regras globais de `max-width`, `box-sizing`, score responsivo e CTA empilhado.
8. Havia repetição visual entre card principal e seções brancas de devolutiva, especialmente em enquadramento/leitura executiva.
9. Certificado/relatório não tinha CSS público de impressão compacto dedicado.
10. Baixar relatório/certificado podia exibir retorno técnico/JSON quando a API devolvia erro.
11. Clique em topo/aba podia ser confundido com handlers genéricos; a correção restringiu mensagens a ações reais como envio/cópia.
12. Plano: padronizar nomenclatura, WhatsApp, data, CSS mobile/print, resultado público, documentação, checklist e validador.
