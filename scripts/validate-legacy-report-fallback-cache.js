const {ok,app}=require('./_legacy-final-validators');
ok(/function readLastPublicResultFromSession/.test(app),'session cache reader exists');
ok(/async function loadPublicResultBundleForAction/.test(app)&&/lastPublicResultActionFallback/.test(app),'report action has cache fallback');
ok(/reportResponsePdf[\s\S]*loadPublicResultBundleForAction/.test(app),'reportResponsePdf uses bundle loader');
