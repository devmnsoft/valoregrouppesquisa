const {read,fail,between}=require('./validate-admin-crud-common');const app=read('app.js');
if(/function audit[\s\S]{0,800}(save\(|saveChanges\(|persist\()/.test(between(app,'function audit','function mapLogCategory')))fail('audit persiste estado geral');
if(!app.includes('isHandlingError'))fail('sem guarda isHandlingError');
process.exit(process.exitCode||0);
