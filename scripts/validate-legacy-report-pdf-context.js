#!/usr/bin/env node
const {ok,app}=require('./_legacy-final-validators');
ok(/function generateValoraInsightReportPdf/.test(app),'generateValoraInsightReportPdf exists');
ok(/generateValoraInsightReportPdf[\s\S]*buildValoraInsightDevolutiva|createValoraInsightReportDocument[\s\S]*buildValoraInsightDevolutiva/.test(app),'report PDF uses Valora Insight engine');
ok(/Resultado geral/.test(app)&&/Radar organizacional/.test(app)&&/radarBarPdfSafe/.test(app),'report PDF contains required sections and PDF-safe radar');
ok(!/createReport[\s\S]{0,1200}[█░→]/.test(app),'report PDF does not use incompatible unicode radar chars');
