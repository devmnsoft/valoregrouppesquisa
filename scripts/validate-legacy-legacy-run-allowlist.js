const {ok,app}=require('./_legacy-final-validators');
ok(/const LEGACY_PUBLIC_ACTIONS/.test(app)&&/async function legacyRun/.test(app),'legacy_run allowlist exists');
ok(/reportResponsePdf/.test(app)&&/certificatePdf/.test(app)&&/openWhatsapp/.test(app),'legacy_run public actions mapped');
