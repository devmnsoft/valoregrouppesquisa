const {ok,app}=require('./_legacy-final-validators');
ok(/__valoraPublicSubmitInProgress/.test(app),'submit in-progress guard exists');
ok(/stopImmediatePropagation/.test(app),'submit stops duplicate propagation');
