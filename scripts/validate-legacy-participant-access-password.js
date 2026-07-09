#!/usr/bin/env node
const {ok,app,fn,repo}=require('./_legacy-final-validators');
ok(/accessPassword:\s*getFormValue\(formEl,\s*'accessPassword'\)/.test(app),'front sends accessPassword from public form');
ok(/participantAccessHash/.test(fn)&&/participantAccess/.test(fn)&&/passwordHash/.test(fn),'backend stores participant password hash');
ok(/exports\.getParticipantResultsByPassword/.test(fn)&&!/getParticipantResultsByPassword[\s\S]{0,600}authedUser\(/.test(fn),'participant result lookup is public and password based');
ok(/getParticipantResultsByPasswordFirebase/.test(repo),'repository exposes participant password lookup');
