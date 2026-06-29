#!/usr/bin/env node
const {read,must,has}=require('./sprint48-validator-lib');
has('backend/Valora.Api/Configuration/FreeSurveySecurityOptions.cs',['MaxSubmissionsPerIpPerHour','MaxSubmissionsPerEmailPerDay','MinSecondsToSubmit','EnableHoneypot']);
has('backend/Valora.Api/Controllers/PublicSurveysController.cs',['ip-rate-limit','email-rate-limit','token-rate-limit','honeypot-filled','minimum-fill-time','duplicate-submission-window','FREE_SURVEY_SECURITY_BLOCKED','SHA256']);
has('database/postgresql/048_free_survey_security_events.sql',['free_survey_security_events','ip_hash','email_hash','user_agent_hash','metadata_json']);
console.log('validate-free-survey-security: PASS');
