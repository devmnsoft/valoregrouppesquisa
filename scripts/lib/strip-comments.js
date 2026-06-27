function stripComments(source, { stripStrings = false } = {}) {
  let out = '';
  let i = 0;
  let state = 'code';
  let quote = '';
  while (i < source.length) {
    const ch = source[i];
    const next = source[i + 1];
    if (state === 'code') {
      if (ch === '/' && next === '/') { state = 'line'; i += 2; continue; }
      if (ch === '/' && next === '*') { state = 'block'; i += 2; continue; }
      if (ch === '"' || ch === "'" || ch === '`') { quote = ch; state = 'string'; out += stripStrings ? '""' : ch; i++; continue; }
      out += ch; i++; continue;
    }
    if (state === 'line') { if (ch === '\n') { out += '\n'; state = 'code'; } i++; continue; }
    if (state === 'block') { if (ch === '*' && next === '/') { state = 'code'; i += 2; } else { if (ch === '\n') out += '\n'; i++; } continue; }
    if (state === 'string') {
      if (ch === '\\') { if (!stripStrings) out += source.slice(i, i + 2); i += 2; continue; }
      if (ch === quote) { if (!stripStrings) out += ch; state = 'code'; i++; continue; }
      if (!stripStrings) out += ch;
      i++; continue;
    }
  }
  return out;
}
module.exports = { stripComments };
