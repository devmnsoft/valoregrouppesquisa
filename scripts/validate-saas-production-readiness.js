const {exists,assert,pass}=require('./validate-sprint27-utils');
['SPRINT_27_SAAS_PRODUCTION_READINESS_AUDIT.md','SAAS_PRODUCT_COMPLETION_STATUS.md','PRODUCTION_READINESS_CHECKLIST.md','KNOWN_LIMITATIONS_BEFORE_PRODUCTION.md'].forEach(f=>assert(exists(f),`${f} missing`));
pass('validate-saas-production-readiness');
