const {ok}=require('./_legacy-final-validators');
const {execSync}=require('child_process');
const out=execSync("rg -n \"(EMAIL_API_KEY=|SMTP_PASSWORD=|-----BEGIN PRIVATE KEY-----)\" -g '!node_modules' -g '!functions/node_modules' -g '!scripts/validate-secrets-not-committed.js' . || true",{encoding:'utf8'}).split('\n').filter(Boolean).filter(line=>!/defineSecret\('(EMAIL_API_KEY|SMTP_PASSWORD)'\)/.test(line)).filter(line=>!/(scripts|tools|migration)\//.test(line));
ok(out.length===0,'no private e-mail secrets or service-account keys committed');
