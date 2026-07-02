using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Xunit;

namespace Valora.Tests;

public sealed class MigrationImportTests
{
    readonly ILegacyDataNormalizer normalizer = new LegacyDataNormalizer();
    [Fact] public void Normalizes_Email_Document_Status_Role_Module()
    { Assert.Equal("user@example.com", normalizer.NormalizeEmail(" User@Example.COM ")); Assert.Equal("12345678000199", normalizer.NormalizeDocument("12.345.678/0001-99")); Assert.Equal("active", normalizer.NormalizeStatus("ativo")); Assert.Equal("manager", normalizer.NormalizeRole("gestor")); Assert.Equal("pesquisa-clima", normalizer.NormalizeModule("Pesquisa Clima")); }
    [Fact] public async Task Firestore_Reader_Masks_Sensitive_Data()
    { var reader=new FirestoreExportReader(new LegacyMappingService(), normalizer); var result=await reader.ReadAsync(new MigrationUploadRequest("firestore","sample","f.json","application/json","{\"users\":{\"u1\":{\"email\":\"A@B.COM\",\"password\":\"plain\",\"token\":\"abc\"}}}")); Assert.Single(result.Documents); Assert.Contains("***MASKED***", result.Documents[0].MaskedJson); Assert.DoesNotContain("plain", result.Documents[0].MaskedJson); }
    [Fact] public async Task LocalStorage_Reader_Accepts_Array_Structure()
    { var reader=new LocalStorageExportReader(new LegacyMappingService(), normalizer); var result=await reader.ReadAsync(new MigrationUploadRequest("localStorage","db","database.sample.json","application/json","{\"forms\":[{\"id\":\"f1\",\"title\":\"Clima\"}]}")); Assert.Equal("forms", result.Documents[0].Collection); Assert.Equal("forms", result.Documents[0].TargetEntity); }
    [Fact] public void Apply_And_Rollback_Requests_Expose_Confirmations()
    { var apply=new MigrationApplyRequest(Guid.NewGuid(),false,"admin_valora",null); var rollback=new MigrationRollbackRequest(Guid.NewGuid(),false,"admin_valora",null); Assert.False(apply.ConfirmApply); Assert.False(rollback.ConfirmRollback); }
    [Fact] public void Dtos_Do_Not_Expose_Raw_Sensitive_Payload_Names()
    { var props=typeof(MigrationRecordDto).GetProperties().Select(p=>p.Name).ToArray(); Assert.Contains("InputMaskedJson", props); Assert.DoesNotContain("Password", props); Assert.DoesNotContain("TokenHash", props); }
    [Fact] public void Cutover_Readiness_Can_Be_Blocked_By_Conflict_Dto()
    { var conflict=new MigrationConflictDto(Guid.NewGuid(),Guid.NewGuid(),"users","u1","users",null,"identity_mismatch","blocking","{}","{}",null,null,null); Assert.Equal("blocking", conflict.Severity); }
}
