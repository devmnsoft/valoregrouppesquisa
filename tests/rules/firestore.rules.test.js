const assert = require('assert');
const fs = require('fs');
const {
  initializeTestEnvironment,
  assertSucceeds,
  assertFails,
} = require('@firebase/rules-unit-testing');
const {
  doc,
  getDoc,
  setDoc,
  updateDoc,
  addDoc,
  collection,
} = require('firebase/firestore');

const PROJECT_ID = 'valora-security-rules-test';

let testEnv;

function authed(uid) {
  return testEnv.authenticatedContext(uid).firestore();
}

function anon() {
  return testEnv.unauthenticatedContext().firestore();
}

async function seed() {
  await testEnv.withSecurityRulesDisabled(async (ctx) => {
    const db = ctx.firestore();
    const docs = {
      'users/admin-valora': { uid: 'admin-valora', role: 'admin_valora', companyId: 'valora', status: 'active', name: 'Admin' },
      'users/consultor': { uid: 'consultor', role: 'consultor_valora', companyId: 'valora', status: 'active', name: 'Consultor' },
      'users/empresa-admin': { uid: 'empresa-admin', role: 'empresa_admin', companyId: 'empresa-a', status: 'active', name: 'Empresa Admin' },
      'users/gestor': { uid: 'gestor', role: 'gestor_pesquisa', companyId: 'empresa-a', status: 'active', name: 'Gestor' },
      'users/participante': { uid: 'participante', role: 'participante', companyId: 'empresa-a', status: 'active', name: 'Participante' },
      'users/participante-b': { uid: 'participante-b', role: 'participante', companyId: 'empresa-b', status: 'active', name: 'Participante B' },
      'organizations/empresa-a': { companyId: 'empresa-a', name: 'Empresa A', planId: 'pro', billingStatus: 'active' },
      'organizations/empresa-b': { companyId: 'empresa-b', name: 'Empresa B', planId: 'pro', billingStatus: 'active' },
      'settings/public': { appName: 'Valora Pulse' },
      'settings/private': { smtp: 'secret' },
      'logs/log-1': { action: 'seed', companyId: 'empresa-a' },
      'forms/form-a': { companyId: 'empresa-a', name: 'Form A', isGlobal: false },
      'surveys/survey-a': { companyId: 'empresa-a', formId: 'form-a', status: 'active' },
      'surveys/survey-b': { companyId: 'empresa-b', formId: 'form-b', status: 'active' },
      'responses/resp-a': { companyId: 'empresa-a', participantUid: 'participante', surveyId: 'survey-a' },
      'responses/resp-b': { companyId: 'empresa-b', participantUid: 'participante-b', surveyId: 'survey-b' },
      'invoices/inv-a': { companyId: 'empresa-a', total: 100 },
    };
    await Promise.all(Object.entries(docs).map(([path, data]) => setDoc(doc(db, path), data)));
  });
}

describe('Firestore security rules — matriz multiempresa', () => {
  before(async () => {
    testEnv = await initializeTestEnvironment({
      projectId: PROJECT_ID,
      firestore: { rules: fs.readFileSync('firestore.rules', 'utf8') },
    });
  });

  beforeEach(async () => {
    await testEnv.clearFirestore();
    await seed();
  });

  after(async () => {
    await testEnv.cleanup();
  });

  it('Admin Valora pode criar empresa, criar usuário, alterar plano, ler logs e ler respostas de todas as empresas', async () => {
    const db = authed('admin-valora');
    await assertSucceeds(setDoc(doc(db, 'organizations/empresa-c'), { name: 'Empresa C', planId: 'starter' }));
    await assertSucceeds(setDoc(doc(db, 'users/novo-admin'), { uid: 'novo-admin', role: 'empresa_admin', companyId: 'empresa-c' }));
    await assertSucceeds(updateDoc(doc(db, 'organizations/empresa-a'), { planId: 'enterprise' }));
    await assertSucceeds(getDoc(doc(db, 'logs/log-1')));
    await assertSucceeds(getDoc(doc(db, 'responses/resp-a')));
    await assertSucceeds(getDoc(doc(db, 'responses/resp-b')));
  });

  it('Consultor Valora lê clientes, pesquisas e respostas, mas não altera plano nem settings/private', async () => {
    const db = authed('consultor');
    await assertSucceeds(getDoc(doc(db, 'organizations/empresa-a')));
    await assertSucceeds(getDoc(doc(db, 'surveys/survey-a')));
    await assertSucceeds(getDoc(doc(db, 'responses/resp-b')));
    await assertFails(updateDoc(doc(db, 'organizations/empresa-a'), { planId: 'enterprise' }));
    await assertFails(updateDoc(doc(db, 'settings/private'), { smtp: 'changed' }));
  });

  it('Empresa Admin gerencia usuários da própria empresa sem escalar privilégios ou acessar dados globais/de terceiros', async () => {
    const db = authed('empresa-admin');
    await assertSucceeds(getDoc(doc(db, 'organizations/empresa-a')));
    await assertSucceeds(setDoc(doc(db, 'users/novo-user-a'), { uid: 'novo-user-a', role: 'participante', companyId: 'empresa-a' }));
    await assertFails(setDoc(doc(db, 'users/novo-admin-valora'), { uid: 'novo-admin-valora', role: 'admin_valora', companyId: 'valora' }));
    await assertFails(updateDoc(doc(db, 'users/participante'), { companyId: 'empresa-b' }));
    await assertFails(updateDoc(doc(db, 'users/participante'), { role: 'admin_valora' }));
    await assertFails(getDoc(doc(db, 'organizations/empresa-b')));
    await assertFails(getDoc(doc(db, 'logs/log-1')));
  });

  it('Gestor Pesquisa cria formulário/pesquisa da própria empresa e não acessa usuários críticos nem financeiro global', async () => {
    const db = authed('gestor');
    await assertSucceeds(setDoc(doc(db, 'forms/form-novo'), { companyId: 'empresa-a', name: 'Novo', isGlobal: false }));
    await assertSucceeds(setDoc(doc(db, 'surveys/survey-novo'), { companyId: 'empresa-a', formId: 'form-novo', status: 'draft' }));
    await assertFails(updateDoc(doc(db, 'users/empresa-admin'), { role: 'participante' }));
    await assertFails(getDoc(doc(db, 'invoices/inv-a')));
  });

  it('Participante lê próprio perfil, altera apenas campos permitidos e não acessa respostas/empresas alheias', async () => {
    const db = authed('participante');
    await assertSucceeds(getDoc(doc(db, 'users/participante')));
    await assertSucceeds(updateDoc(doc(db, 'users/participante'), { name: 'Novo Nome', phone: '11999999999', preferences: { theme: 'dark' } }));
    await assertFails(updateDoc(doc(db, 'users/participante'), { role: 'admin_valora' }));
    await assertFails(updateDoc(doc(db, 'users/participante'), { companyId: 'empresa-b' }));
    await assertFails(addDoc(collection(db, 'responses'), { companyId: 'empresa-b', participantUid: 'participante' }));
    await assertSucceeds(getDoc(doc(db, 'responses/resp-a')));
    await assertFails(getDoc(doc(db, 'responses/resp-b')));
  });

  it('Usuário não autenticado só lê settings/public', async () => {
    const db = anon();
    await assertFails(getDoc(doc(db, 'surveys/survey-a')));
    await assertSucceeds(getDoc(doc(db, 'settings/public')));
    await assertFails(getDoc(doc(db, 'users/participante')));
    await assertFails(getDoc(doc(db, 'responses/resp-a')));
    await assertFails(getDoc(doc(db, 'invoices/inv-a')));
    await assertFails(getDoc(doc(db, 'logs/log-1')));
  });
});
