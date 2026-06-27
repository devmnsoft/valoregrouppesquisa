const {exists,assert,pass}=require('./validate-sprint27-utils');
['SPRINT_27_SAAS_PRODUCTION_READINESS_AUDIT.md','SAAS_PLANS_AND_BILLING_READINESS.md','SECURITY_PRODUCTION_READINESS.md','LGPD_PRODUCTION_READINESS.md','AUDIT_LOG_POLICY.md'].forEach(file=>assert(exists(file), file+' missing'));
pass('validate-security-production-gate');
