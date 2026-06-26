using System.Data;
namespace Valora.Application.Contracts;
public interface IResultRepository { Task<dynamic?> GetByResponseAsync(Guid responseId); Task SaveResultAsync(Guid organizationId,Guid responseId,decimal total,decimal max,decimal percentage,string maturityLabel,string radarText,string strategicTruth,string risk,string nextLevel,IDbTransaction transaction); Task SaveDimensionScoresAsync(Guid organizationId,Guid responseId,IEnumerable<dynamic> dimensions,IDbTransaction transaction); Task<IReadOnlyList<dynamic>> GetDimensionsByResponseIdAsync(Guid responseId); }
