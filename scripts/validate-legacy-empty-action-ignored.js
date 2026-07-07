const {ok,app}=require('./_legacy-final-validators');
ok(/lastEmptyActionClick/.test(app)&&/if\(el\?\.tagName==='A'&&el\.href\)return true/.test(app),'empty data-action anchor navigates and diagnostics recorded');
