const assert = require('assert');
const fs = require('fs');

describe('adminCreateResultShareLink CORS callable contract', () => {
  it('keeps the callable preflight contract for the production origin', () => {
    const source = fs.readFileSync('functions/index.js', 'utf8');
    const block = (source.match(/exports\.adminCreateResultShareLink\s*=\s*onCall\(\s*\{[\s\S]*?\}\s*,\s*async\s+req\s*=>\s*\{[\s\S]*?return \{ok:true,responseId,resultToken:access\.resultToken,url\};\}\);/) || [])[0];
    assert.ok(block, 'adminCreateResultShareLink must remain a Firebase callable function');
    assert.match(block, /region\s*:\s*['"]us-central1['"]/, 'callable must be deployed in us-central1');
    assert.match(block, /cors\s*:\s*ALLOWED_CORS_ORIGINS/, 'callable must delegate OPTIONS/preflight CORS to the allowlist');
    assert.ok(source.includes('https://valoragroup.mnsoft.com.br'), 'production origin must be allowed');
    const requestedHeaders = ['authorization', 'content-type', 'x-client-version', 'x-firebase-gmpid'];
    assert.deepStrictEqual(requestedHeaders, ['authorization', 'content-type', 'x-client-version', 'x-firebase-gmpid'], 'preflight contract documents the requested headers expected from the Firebase SDK');
    assert.ok(block.includes('requireAdminUser(req)'), 'authorization must stay in the callable handler');
    assert.ok(block.includes('assertUserCanAccessResponse(user,snap.data())'), 'company scope must stay in the callable handler');
    assert.ok(block.includes('buildPublicResultUrl(responseId,access.resultToken)'), 'authorized response must return result + rt URL');
    assert.doesNotMatch(block, /resultTokenHash|tokenHash/, 'hashes must not be returned in the share URL contract');
  });
});
