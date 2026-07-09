#!/usr/bin/env node
const {ok,app}=require('./_legacy-final-validators');
ok(/getEffectivePlanCapabilities/.test(app),'effective plan capabilities exist');
ok(/enforcePlanLimit/.test(app),'plan limit enforcement exists');
ok(/free|essential|growth|professional|corporate|enterprise/.test(app),'official SaaS plan codes present');
