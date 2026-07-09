#!/usr/bin/env node
const {ok,app}=require('./_legacy-final-validators');
ok(/canUpgradePlan/.test(app),'canUpgradePlan exists');
ok(/openPlanUpgradeModal/.test(app),'openPlanUpgradeModal exists');
ok(/Aderir ao plano|Solicitar upgrade|Falar com a Valora Group/.test(app),'upgrade CTAs exist');
