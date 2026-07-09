#!/usr/bin/env node
const {ok,app}=require('./_legacy-final-validators');
ok(/adminViewResponse/.test(app),'adminViewResponse exists');
ok(/adminReportResponsePdf/.test(app),'adminReportResponsePdf exists');
ok(/adminCertificatePdf/.test(app),'adminCertificatePdf exists');
ok(!/admin(ViewResponse|ReportResponsePdf|CertificatePdf)[\s\S]{0,500}getPublicResult\([^,)]*\)/.test(app),'admin actions do not call public result without token');
