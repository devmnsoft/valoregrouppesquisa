using System.Collections.Concurrent;

namespace Valora.Web.Services.Bff;

public interface IBffSessionStore
{
    void Set(string ticket, BffServerSession session);
    bool TryGet(string ticket, out BffServerSession? session);
    void Remove(string ticket);
}

public sealed class BffSessionStore : IBffSessionStore
{
    private readonly ConcurrentDictionary<string, BffServerSession> sessions = new(StringComparer.Ordinal);
    public void Set(string ticket, BffServerSession session) => sessions[ticket] = session;
    public bool TryGet(string ticket, out BffServerSession? session) => sessions.TryGetValue(ticket, out session);
    public void Remove(string ticket) => sessions.TryRemove(ticket, out _);
}
