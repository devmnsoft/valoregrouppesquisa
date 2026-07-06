const fs=require('fs');const s=fs.readFileSync('functions/index.js','utf8');
for(const x of ['function normalizeParticipantEmail','function participantAccessHash','emailHash:sha256(normalizeParticipantEmail(participant.email))','passwordHash:participantAccessHash','exports.getParticipantResultsByPassword=onCall','resultTokenHash:sha256(newToken)','resultToken:newToken'])if(!s.includes(x))throw new Error('participant access ausente: '+x);
if(/accessPassword\s*[,}]/.test((s.match(/const response=\{[\s\S]*?\};await db\.runTransaction/)||[''])[0]))throw new Error('accessPassword pode estar sendo salvo puro na response');
console.log('functions participant access password: PASS');
