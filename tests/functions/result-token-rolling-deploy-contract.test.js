const assert = require('assert');
const fs = require('fs');

const source = () => fs.readFileSync('functions/index.js', 'utf8');
const functionBlock = (name) => {
  const s = source();
  const start = s.indexOf(`exports.${name}=onCall`);
  assert.ok(start >= 0, `${name} export missing`);
  const next = s.indexOf('\nexports.', start + 1);
  return s.slice(start, next > start ? next : s.length);
};

describe('result token rolling deploy compatibility contract', () => {
  it('new admin share link works with new getPublicResult and returns contract version 2', () => {
    const adminShare = functionBlock('adminCreateResultShareLink');
    const getPublic = functionBlock('getPublicResult');
    assert.match(adminShare, /rotateResultTokenForResponse\(responseId,'whatsapp_result_share'\)/);
    assert.match(adminShare, /buildPublicResultUrl\(responseId,access\.resultToken\)/);
    assert.match(adminShare, /tokenContractVersion:2/);
    assert.match(getPublic, /tokenContractVersion:2/);
    assert.match(getPublic, /verifyAndTouchResultAccess\(responseId,resultToken,response\)/);
  });

  it('new share link works with old getPublicResult through resultTokenHash compatibility pointer', () => {
    const s = source();
    const createStart = s.indexOf('async function createResultAccessToken');
    const createEnd = s.indexOf('async function rotateResultTokenForResponse', createStart);
    const createBlock = s.slice(createStart, createEnd);
    assert.match(createBlock, /accessCol\.doc\(tokenHash\)/);
    assert.match(createBlock, /resultTokenHash:tokenHash/);
    assert.match(createBlock, /resultTokenVersion:2/);
    assert.match(createBlock, /resultTokenUpdatedAt:TS\(\)/);
  });

  it('allows multiple active subcollection tokens in the new getPublicResult', () => {
    const verifyStart = source().indexOf('async function verifyAndTouchResultAccess');
    const verifyEnd = source().indexOf('function createSmtpTransporter', verifyStart);
    const verifyBlock = source().slice(verifyStart, verifyEnd);
    assert.match(verifyBlock, /collection\('resultAccessTokens'\)\.doc\(tokenHash\)/);
    assert.match(verifyBlock, /accessData\.status==='active'/);
    assert.doesNotMatch(verifyBlock, /where\('status','==','active'\).*limit\(1\)/s);
  });

  it('rejects nonexistent, revoked, and expired tokens as invalid_result_token with sanitized diagnostics', () => {
    const getPublic = functionBlock('getPublicResult');
    const verifyStart = source().indexOf('async function verifyAndTouchResultAccess');
    const verifyEnd = source().indexOf('function createSmtpTransporter', verifyStart);
    const verifyBlock = source().slice(verifyStart, verifyEnd);
    assert.match(getPublic, /code:'invalid_result_token'/);
    assert.match(verifyBlock, /accessData\.status!=='active'/);
    assert.match(verifyBlock, /exp\.getTime\(\)>Date\.now\(\)/);
    const diagnosticStart = source().indexOf('async function writePublicResultAccessDiagnostic');
    const diagnosticBlock = source().slice(diagnosticStart, verifyStart);
    for (const field of ['responseId', 'reason', 'tokenHashPrefix', 'hasLegacyHash', 'accessDocumentExists', 'accessStatus', 'deployedContractVersion', 'createdAt']) {
      assert.ok(diagnosticBlock.includes(field), `diagnostic must include ${field}`);
    }
  });

  it('never persists, logs, audits, errors, or URLs with raw-token exposure beyond rt', () => {
    const s = source();
    assert.doesNotMatch(s, /resultToken:\s*resultTokenHash|rt=resultTokenHash/);
    assert.match(s, /tokenHashPrefix:tokenHash\.slice\(0,8\)/);
    assert.match(s, /url\.searchParams\.set\('result',safeResponseId\)/);
    assert.match(s, /url\.searchParams\.set\('rt',safeToken\)/);
    assert.doesNotMatch(functionBlock('getPublicResult'), /providedTokenLength|userAgent:req\.rawRequest/);
  });

  it('WhatsApp link keeps result and rt for anonymous public-result context', () => {
    const app = fs.readFileSync('app.js', 'utf8');
    const repo = fs.readFileSync('firebase-repository.js', 'utf8');
    assert.match(source(), /function buildPublicResultUrl\(responseId,resultToken\)[\s\S]*searchParams\.set\('result',safeResponseId\)[\s\S]*searchParams\.set\('rt',safeToken\)/);
    assert.match(repo, /callFunction\('getPublicResult',\{responseId,resultToken\}\)/);
    assert.match(app, /getPublicResultRouteParams\(\)/);
  });
});
