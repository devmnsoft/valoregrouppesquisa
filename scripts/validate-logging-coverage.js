const fs=require('fs');const {stripComments}=require('./lib/strip-comments');const checks={
'backend/Valora.Application/Services/PublicSurveys/PublicSurveySubmitter.cs':'ILogger<PublicSurveySubmitter>',
'backend/Valora.Application/Services/PublicSurveys/PublicResponseTransactionService.cs':'ILogger<PublicResponseTransactionService>',
'backend/Valora.Application/Services/PublicResults/PublicResultService.cs':'ILogger<PublicResultService>',
'backend/Valora.Application/Services/PublicResults/PublicResultValidator.cs':'ILogger<PublicResultValidator>',
'backend/Valora.Infrastructure/Database/MigrationRunner.cs':'ILogger<MigrationRunner>',
'backend/Valora.Infrastructure/Email/SmtpEmailSender.cs':'ILogger<SmtpEmailSender>',
'backend/Valora.Application/Communication/EmailJobService.cs':'ILogger<EmailJobService>',
'backend/Valora.Application/Services/Auth/AuthService.cs':'ILogger<AuthService>'};
let fail=[];for(const [p,t] of Object.entries(checks)){const s=stripComments(fs.readFileSync(p,'utf8'));if(!s.includes(t))fail.push(`${p} sem ${t}`);if(!s.includes('LogInformation')&&!s.includes('LogWarning')&&!s.includes('LogError'))fail.push(`${p} sem logs`)} if(fail.length){console.error(fail.join('\n'));process.exit(1)} console.log('Cobertura de logging validada.');
