using System.Text.Json;
using Valora.Application.ReadModels;
namespace Valora.Application.Services;
public sealed class PublicAnswerNormalizer
{
    public IReadOnlyList<NormalizedAnswer> Normalize(IReadOnlyList<QuestionPublicReadModel> questions, Dictionary<string, object>? answers)
    {
        answers ??= new(); var result = new List<NormalizedAnswer>();
        foreach (var q in questions)
        {
            var raw = TryGet(answers, q.Id.ToString()) ?? TryGet(answers, $"q{q.DisplayOrder}");
            if (raw is null) { result.Add(new(q.Id, null, "null", null)); continue; }
            var text = ToSafeText(raw); var numeric = ToDecimal(raw);
            result.Add(new(q.Id, text, JsonSerializer.Serialize(new { value = text }), numeric));
        }
        return result;
    }
    static object? TryGet(Dictionary<string, object> values,string key) => values.TryGetValue(key,out var v) ? v : null;
    static decimal? ToDecimal(object value) => value switch { decimal d=>d, int i=>i, long l=>l, double d=>(decimal)d, float f=>(decimal)f, string s when decimal.TryParse(s,out var n)=>n, JsonElement e when e.ValueKind==JsonValueKind.Number && e.TryGetDecimal(out var n)=>n, JsonElement e when e.ValueKind==JsonValueKind.String && decimal.TryParse(e.GetString(),out var n)=>n, _=>null };
    static string? ToSafeText(object value) => value switch { null=>null, string s=>s, int or long or decimal or double or float=>Convert.ToString(value,System.Globalization.CultureInfo.InvariantCulture), JsonElement e when e.ValueKind==JsonValueKind.String=>e.GetString(), JsonElement e when e.ValueKind==JsonValueKind.Number=>e.GetRawText(), JsonElement e when e.ValueKind==JsonValueKind.Array=>string.Join(",", e.EnumerateArray().Select(x=>x.ToString())), IEnumerable<string> xs=>string.Join(",",xs), _=>throw new InvalidOperationException("Resposta com objeto complexo não é aceita.") };
}
