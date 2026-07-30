const fs=require('fs'); const path=require('path');
const root=process.cwd(); let ok=true;
function has(file, needles){const p=path.join(root,file); if(!fs.existsSync(p)){console.error('MISSING',file); ok=false; return '';} const txt=fs.readFileSync(p,'utf8'); for(const n of needles){ if(!txt.includes(n)){console.error('MISSING',file,n); ok=false;} else console.log('OK',file,n);} return txt;}
has('backend/database/postgresql/script_completo.sql',['migration_batches','migration_rollback_items']);
has('backend/Valora.Domain/Entities/LegacyMigrationEntities.cs',['MigrationSourceFile','MigrationRecord','MigrationMapping','MigrationConflict','MigrationRollbackItem']);
has('backend/Valora.Domain/Entities/MigrationDomainEntities.cs',['MigrationBatch','SourceType','SummaryJson']);
for(const dto of ['MigrationBatchDto','MigrationDryRunRequest','MigrationApplyRequest','MigrationRollbackRequest','CutoverReadinessDto']) has(`backend/Valora.Application/DTOs/${dto}.cs`,[dto]);
has('backend/Valora.Application/Contracts/Repositories/MigrationImportRepositories.cs',['IMigrationBatchRepository','IMigrationRollbackRepository']);
has('backend/Valora.Application/Contracts/Services/MigrationImportServices.cs',['IFirestoreExportReader','ILocalStorageExportReader','IManualJsonReader','IMigrationDryRunService','ICutoverReadinessService']);
has('backend/Valora.Infrastructure/Repositories/MigrationImportRepositories.cs',['Dapper','CommandDefinition','valorapesquisa.migration_records']);
has('backend/Valora.Api/Controllers/MigrationController.cs',['/migration/sources','/migration/batches/{batchId:guid}/dry-run','/migration/batches/{batchId:guid}/apply','/migration/batches/{batchId:guid}/rollback','cutover-readiness']);
for(const v of ['Index','Batches','Batch','Upload','DryRun','Conflicts','Reconciliation','Rollback','CutoverReadiness']) has(`backend/Valora.Web/Views/Migration/${v}.cshtml`,['payload sensível','Divergências']);
const dto=fs.readdirSync(path.join(root,'backend/Valora.Application/DTOs')).filter(f=>/^(Migration|Cutover)/.test(f)).map(f=>fs.readFileSync(path.join(root,'backend/Valora.Application/DTOs',f),'utf8')).join('\n');
if(/password|senha|token_hash|result_token_hash|refresh token|connection string|segredo/i.test(dto.replace(/MaskedJson/g,''))){console.error('DTO expõe nome sensível não mascarado'); ok=false;} else console.log('OK DTOs não expõem senha/hash/token sem máscara');
const pkg=JSON.parse(fs.readFileSync(path.join(root,'package.json'),'utf8'));
if(pkg.scripts?.['backend:migration-import-validate']) console.log('OK package script'); else {console.error('MISSING package script'); ok=false;}
const sln=fs.readFileSync(path.join(root,'backend/Valora.sln'),'utf8'); if(sln.includes('projeto .NET predecessor removido')){console.error('projeto .NET predecessor removido usado na solution oficial'); ok=false;} else console.log('OK projeto .NET predecessor removido fora do build oficial');
process.exit(ok?0:1);
