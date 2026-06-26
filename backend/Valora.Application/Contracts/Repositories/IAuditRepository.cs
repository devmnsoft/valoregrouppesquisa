using System.Data;
using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IAuditRepository { Task AddAsync(AuditEntry entry); Task LogAsync(AuditEntry entry,IDbTransaction? transaction=null); }
