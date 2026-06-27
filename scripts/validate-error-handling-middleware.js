const fs=require('fs');const p='backend/Valora.Api/Middleware/ErrorHandlingMiddleware.cs';const s=fs.readFileSync(p,'utf8');
const req=['try','catch (Exception ex)','ok','message','code','traceId','correlationId','MapException','Status400BadRequest','Status500InternalServerError','WriteAsync'];
const miss=req.filter(x=>!s.includes(x)); if(miss.length) {console.error('ErrorHandlingMiddleware incompleto:',miss);process.exit(1);} console.log('ErrorHandlingMiddleware validado.');
