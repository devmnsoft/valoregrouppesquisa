const {assert,has,done}=require('./_legacy-validator-lib');
['officialPlansFallback','free','essential','professional','corporate','enterprise','plansSection','admin/plans'].forEach(x=>assert(has('app.js',x)||has('module-definitions.js',x),`plans missing ${x}`));done('legacy plans tab');
