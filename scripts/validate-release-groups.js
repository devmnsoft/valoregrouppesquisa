const fs = require('fs');
const cp = require('child_process');
const manifest = JSON.parse(fs.readFileSync('functions/release-groups.json', 'utf8'));
const args = process.argv.slice(2);
const onlyArg = args.find(a => a.startsWith('--only='));
const override = process.env.RELEASE_GROUP_OVERRIDE === 'true';
const changed = (() => {
  try { return cp.execSync('git diff --name-only HEAD~1...HEAD', { encoding: 'utf8' }).trim().split(/\n/).filter(Boolean); }
  catch (_) { return []; }
})();
for (const [name, group] of Object.entries(manifest.groups || {})) {
  if (!Array.isArray(group.functions) || !group.functions.length) throw new Error(`release group ${name} sem functions`);
  if (!group.requiresAtomicDeploy) continue;
  if (onlyArg && onlyArg.includes('functions:')) {
    const deployed = onlyArg.replace(/^--only=/, '').split(',').map(x => x.replace(/^functions:/, '').trim()).filter(Boolean);
    const impacted = deployed.some(fn => group.functions.includes(fn));
    const missing = group.functions.filter(fn => !deployed.includes(fn));
    if (impacted && missing.length && !override) throw new Error(`Deploy isolado bloqueado para ${name}; faltam: ${missing.join(', ')}`);
  }
}
if (changed.some(f => /^functions\//.test(f) || f === 'firebase-repository.js' || f === 'app.js')) {
  for (const [name, group] of Object.entries(manifest.groups || {})) {
    if (!group.requiresAtomicDeploy) continue;
    if (!group.overridePolicy) throw new Error(`release group ${name} sem overridePolicy`);
  }
}
console.log('validate-release-groups: PASS');
