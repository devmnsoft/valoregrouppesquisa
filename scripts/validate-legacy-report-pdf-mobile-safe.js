const {has,no}=require('./legacy-final-validator-lib');has('report-service.js',/radarBarPdfSafe[\s\S]*#.*-/,'safe radar missing');no('pdf.js',/[█░]|\?\?\?/, 'pdf has unsafe chars');console.log('ok');
