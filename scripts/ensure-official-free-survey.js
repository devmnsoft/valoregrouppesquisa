#!/usr/bin/env node
'use strict';

const crypto = require('crypto');

function token(bytes = 24) { return crypto.randomBytes(bytes).toString('base64url'); }
function sha256(value) { return crypto.createHash('sha256').update(String(value)).digest('hex'); }

async function main() {
  let admin;
  try { admin = require('firebase-admin'); } catch (_) {
    console.error('firebase-admin não instalado. Rode dentro do ambiente com dependências do projeto/Firebase.');
    process.exit(1);
  }
  if (!admin.apps.length) admin.initializeApp();
  const db = admin.firestore();
  const now = admin.firestore.FieldValue.serverTimestamp();

  const orgId = process.env.OFFICIAL_FREE_SURVEY_ORG_ID || 'valora-group-oficial';
  const formId = process.env.OFFICIAL_FREE_SURVEY_FORM_ID || 'form_valora_insight_gratuito';
  const surveyId = process.env.OFFICIAL_FREE_SURVEY_ID || 'survey_valora_insight_gratuito';
  const existing = await db.collection('surveys').doc(surveyId).get();
  const publicToken = existing.get('publicToken') || existing.get('token') || existing.get('accessToken') || token(24);

  const org = {
    id: orgId,
    name: 'Valora Group',
    publicName: 'Valora Group',
    slug: 'valora-group',
    status: 'active',
    planId: 'free',
    updatedAt: now,
    createdAt: existing.exists ? (existing.get('createdAt') || now) : now
  };
  const questions = [
    { id: 'governanca', text: 'A empresa possui rituais claros de governança e tomada de decisão?', type: 'scale', required: true, weight: 1 },
    { id: 'lideranca', text: 'As lideranças comunicam prioridades de forma consistente?', type: 'scale', required: true, weight: 1 },
    { id: 'pessoas', text: 'Os times têm clareza sobre papéis, metas e responsabilidades?', type: 'scale', required: true, weight: 1 },
    { id: 'crescimento', text: 'A operação está preparada para crescer com controles e indicadores?', type: 'scale', required: true, weight: 1 }
  ];
  const form = { id: formId, companyId: orgId, title: 'Valora Insight — Diagnóstico Gratuito', status: 'active', isFree: true, questions, updatedAt: now, createdAt: now };
  const survey = {
    id: surveyId,
    companyId: orgId,
    organizationId: orgId,
    formId,
    title: 'Valora Insight — Diagnóstico Gratuito',
    status: 'active',
    visibility: 'public',
    isFree: true,
    planId: 'free',
    publicToken,
    tokenHash: sha256(publicToken),
    publicUrl: `https://valoragroup.mnsoft.com.br/?survey=${encodeURIComponent(surveyId)}&token=${encodeURIComponent(publicToken)}&org=valora-group`,
    publicLink: `https://valoragroup.mnsoft.com.br/?survey=${encodeURIComponent(surveyId)}&token=${encodeURIComponent(publicToken)}&org=valora-group`,
    visibleOnHome: true,
    featuredOnHome: true,
    revoked: false,
    updatedAt: now,
    createdAt: existing.exists ? (existing.get('createdAt') || now) : now
  };
  const batch = db.batch();
  batch.set(db.collection('organizations').doc(orgId), org, { merge: true });
  batch.set(db.collection('forms').doc(formId), form, { merge: true });
  batch.set(db.collection('surveys').doc(surveyId), survey, { merge: true });
  await batch.commit();
  console.log(JSON.stringify({ ok: true, orgId, formId, surveyId, publicUrl: survey.publicUrl.replace(publicToken, '[redacted]') }, null, 2));
}

main().catch(err => { console.error(err && err.message ? err.message : err); process.exit(1); });
