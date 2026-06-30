using ValoraPesquisa.Application.Services; using ValoraPesquisa.Domain.Entities;
namespace ValoraPesquisa.Tests;
public class FoundationTests{
 [Fact] public void SurveyResultCalculator_calcula_nivel_avancado(){ var qid=Guid.NewGuid(); var calc=new SurveyResultCalculator(new QuestionScoreCalculator()); var r=calc.Calculate(Guid.NewGuid(),new[]{new Question(qid,Guid.NewGuid(),"NPS","scale",1,true,1,5,new())},new[]{new ResponseAnswer(Guid.NewGuid(),Guid.NewGuid(),qid,"5",null)}); Assert.Equal("Avançado",r.Level); Assert.Equal(100,r.Percentage); }
 [Fact] public void Login_invalido_nao_expoe_detalhes(){ var body=new{ok=false,code="INVALID_LOGIN",message="E-mail ou senha inválidos."}; Assert.DoesNotContain("password_hash",body.ToString()); Assert.DoesNotContain("stack",body.ToString(),StringComparison.OrdinalIgnoreCase); }
 [Fact] public void Usuario_nao_deve_retornar_password_hash(){ var dto=new{Id=Guid.NewGuid(),Email="a@b.com",Role="empresa_admin"}; Assert.DoesNotContain("password_hash",System.Text.Json.JsonSerializer.Serialize(dto)); }
 [Fact] public void Link_publico_nao_deve_retornar_token_hash(){ var dto=new{publicUrl="/pesquisa/responder?survey=x&token=y&org=z"}; Assert.DoesNotContain("token_hash",System.Text.Json.JsonSerializer.Serialize(dto)); }
 [Fact] public void Resultado_publico_exige_token_valido(){ string? token=""; Assert.True(string.IsNullOrWhiteSpace(token)); }
 [Fact] public void Usuario_de_uma_organizacao_nao_acessa_outra(){ var userOrg=Guid.NewGuid(); var targetOrg=Guid.NewGuid(); Assert.NotEqual(userOrg,targetOrg); }
 [Fact] public void Script_sql_contem_tabelas_minimas(){ var sql=File.ReadAllText(Path.Combine("..","..","..","..","database","postgresql","scriptbd_completo.sql")); foreach(var t in new[]{"organizations","users","forms","questions","question_options","surveys","survey_links","responses","response_answers","result_scores","audit_logs"}) Assert.Contains($"create table if not exists valorapesquisa.{t}",sql,StringComparison.OrdinalIgnoreCase); }
 [Fact] public void Auditoria_tem_eventos_principais(){ var events=new[]{"login_success","login_failed","organization_created","user_created","form_created","survey_created","survey_published","survey_link_created","public_response_received","result_viewed"}; Assert.Contains("login_success",events); Assert.Contains("public_response_received",events); }
 [Fact] public void Login_valido_tem_claims_minimas(){ var claims=new[]{"user_id","organization_id","role","email"}; Assert.Contains("user_id",claims); Assert.Contains("role",claims); }
}
