const {has}=require('./legacy-premium-validator-utils');
has(/limits\s*:\s*\{/,'planos têm limits');
has(/function enforcePlanLimit/,'enforcePlanLimit existe');
has(/function renderPlanLimitModal/,'modal premium de limite existe');
has(/Seu plano atual chegou ao limite/,'bloqueio premium usa título correto');
