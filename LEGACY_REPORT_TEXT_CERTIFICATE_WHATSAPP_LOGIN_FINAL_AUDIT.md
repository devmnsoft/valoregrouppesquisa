# Auditoria final — relatório, texto, certificado, WhatsApp e login

1. O PDF executivo é gerado em `pdf.js` por `ValoraPDF.createReport`; a devolutiva individual monta o documento em `app.js` (`createValoraInsightReportDocument`) e chama `generateValoraInsightReportPdf`.
2. A linha problemática `91/125 — ? Estruturada` vinha da montagem do resultado geral com `devolutiva.level.title`/ícones de nível. Foi substituída por `pdfScoreLine(totalScore, level)` com hífen ASCII e título saneado.
3. Campos com risco de emoji no PDF: `level.icon`, `maturityLevel.icon`, `dimension.level.icon`, títulos/labels de nível e textos HTML de radar. O PDF agora usa `pdfLevelTitle` e `toPdfSafeText`.
4. O radar incompatível estava em `radarBar` usando blocos `█` e `░`. O HTML pode usar CSS; o PDF usa `radarBarPdfSafe` com `#` e `-`.
5. O corte de texto podia ocorrer em `pdf.js` na tabela do relatório com limite visual de linhas e textos longos sem paginação suficiente.
6. Ocorrências relevantes: `pdf.js` usava `.slice(0,5)` em linhas de célula e helpers de texto com limites; o relatório individual em `app.js` agora envia texto completo, e `pdf.js` expõe helpers de wrap/paginação.
7. O Benchmarking estrutural era montado por `buildBenchmarking` em `app.js`.
8. A referência qualitativa a GPTW Brasil e mercado foi adicionada em `MARKET_BENCHMARK_REFERENCES` e `buildStructuralBenchmarking`, sem números oficiais ou alegação de certificação.
9. O certificado aparecia na tela pública via botões `certificatePdf`, `safeCertificateHtml` e áreas de resultado; os botões visíveis foram removidos e `safeCertificateHtml` retorna vazio.
10. Na área admin, aparecia em cards móveis e ações de respostas; ações visíveis foram removidas.
11. Nos planos, `Certificado simples` foi substituído por link seguro de acesso ao resultado, e capacidades de certificado foram desativadas.
12. `certificatePdf`/`adminCertificatePdf` continuam mapeados apenas como fallback no-op seguro, exibindo toast informativo.
13. Variações como `Fale com a Valora Group` foram padronizadas.
14. CTA/link de WhatsApp usa `Fale com o Valora Group` e o número oficial `5591992545353`.
15. A tela de login está em `renderLogin` e agora mostra `Entrar no Valora Pulse™` com subtítulo `Acesse a gestão do Valora Insight™.`.
16. `Valora Pulse™` permanece restrito a plataforma/admin/login; a devolutiva pública/PDF usa `Valora Insight™` no HTML e `Valora Insight` no PDF.
17. Correções feitas: remoção visual de certificado, no-op seguro para funções legadas, texto PDF seguro, radar ASCII no PDF, benchmarking qualitativo com GPTW Brasil, CTA oficial de WhatsApp, login correto, CSS mobile-first e validadores legados.
