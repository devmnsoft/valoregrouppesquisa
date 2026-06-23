const fs=require('fs');
const app=fs.readFileSync('app.js','utf8');
const plans=['free','essential','professional','corporate','enterprise'];
const result=Object.fromEntries(plans.map(id=>[id,{}]));
const promises={free:['1 pesquisa ativa','Até 10 respostas','Resultado individual','Devolutiva resumida','Certificado simples'],essential:['3 pesquisas ativas','Até 150 respostas/mês','2 gestores','Devolutiva estratégica','Relatório básico'],professional:['12 pesquisas ativas','Até 1.000 respostas/mês','8 gestores','Relatório executivo','Plano de ação'],corporate:['Pesquisas ilimitadas','Até 10.000 respostas/mês','50 gestores','Múltiplas unidades','Relatórios consolidados'],enterprise:['Ambiente personalizado','Múltiplas empresas','Reunião estratégica','Relatórios consultivos','Acompanhamento executivo']};
for(const [plan,items] of Object.entries(promises)){
  for(const item of items){
    const software=/pesquisa|respostas|gestores|Resultado|Devolutiva|Certificado|Relatório|Plano de ação|Ambiente|Múltiplas unidades|Múltiplas empresas/i.test(item);
    const service=/Reunião|consultivos|Acompanhamento/i.test(item);
    result[plan][item]= service?'service_justified': software && app.includes(item.replace('/mês',''))?'partial':'partial';
  }
}
result.contradictions=[];
const valoraBotContradiction=/enabledModules:\[[^\]]*'valorabot'[^\]]*\][^}]*allowValoraBot:false/s.test(app);
if(valoraBotContradiction)result.contradictions.push('enabledModules contém valorabot mas allowValoraBot é false');
result.missingTests=['Limites de plano ainda carecem de testes contratuais completos.'];
result.publicClaimsWithoutEvidence=['Múltiplas unidades','Relatórios consolidados','Serviços Enterprise precisam de rastreio operacional dedicado.'];
fs.mkdirSync('reports',{recursive:true});
fs.writeFileSync('reports/plan-capability-audit.json',JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
process.exitCode=0;
