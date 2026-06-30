using System.Text.Json;
using ValoraPesquisa.Domain.Entities;
namespace ValoraPesquisa.Application.Services;
public sealed class QuestionScoreCalculator {
 public decimal Calculate(Question q, ResponseAnswer? a){ if(a is null) return 0; decimal raw=0; var max=q.MaxScore<=0?5:q.MaxScore; switch(q.Type){ case "scale": raw=Math.Clamp(decimal.TryParse(a.AnswerValue,out var v)?v:0,1,5)/5m*max; break; case "single_choice": raw=q.Options.FirstOrDefault(o=>o.Id.ToString()==a.AnswerValue)?.Score??0; break; case "multiple_choice": var ids=(a.AnswerValue??"").Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries); raw=q.Options.Where(o=>ids.Contains(o.Id.ToString())).Sum(o=>o.Score); raw=Math.Min(raw,max); break; case "short_text": raw=string.IsNullOrWhiteSpace(a.AnswerText)?0:max; break; } return Math.Round(raw*Math.Max(q.Weight,0),2); }
 public decimal Max(Question q)=>Math.Round((q.MaxScore<=0?5:q.MaxScore)*Math.Max(q.Weight,0),2);
}
public sealed class SurveyResultCalculator(QuestionScoreCalculator questionCalculator){
 public ResultScore Calculate(Guid responseId,IEnumerable<Question> questions,IEnumerable<ResponseAnswer> answers){ var ans=answers.ToDictionary(a=>a.QuestionId); decimal total=0,max=0; var details=new List<object>(); foreach(var q in questions.OrderBy(q=>q.Position)){ var score=questionCalculator.Calculate(q,ans.GetValueOrDefault(q.Id)); var qmax=questionCalculator.Max(q); total+=score; max+=qmax; details.Add(new{q.Id,q.Type,score,max=qmax}); } var pct=max==0?0:Math.Round(total/max*100,2); var normalized=Math.Round(pct/20m,2); var level=pct<40?"Inicial":pct<70?"Intermediário":"Avançado"; return new ResultScore(Guid.NewGuid(),responseId,total,max,pct,normalized,level,JsonSerializer.Serialize(details),DateTimeOffset.UtcNow); }
}
