const report={legacyIds:{growth:'professional','enterprise_legacy':'corporate'},steps:['Mapear organizations.planId','Mapear organizations.subscription.planId','Preservar faturas por planId antigo em campo legacyPlanId','Criar novo enterprise consultivo sem confundir com limite corporativo de 10.000 respostas'],dryRun:!process.argv.includes('--apply')};
console.log(JSON.stringify(report,null,2));
