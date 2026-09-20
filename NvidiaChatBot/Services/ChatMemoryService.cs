using System.Collections.Concurrent;
using NvidiaChatBot.Models;

namespace NvidiaChatBot.Services;

public class ChatMemoryService
{
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();
    
    public ChatSession GetOrCreateSession(string sessionId)
    {
        return _sessions.GetOrAdd(sessionId, id => new ChatSession { SessionId = id });
    }

    public void UpdateSessionActivity(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastActive = DateTime.UtcNow;
        }
    }

    public List<ChatMessage> GetRecentHistory(string sessionId, int maxPairs = 5)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return new List<ChatMessage>();
            
        // 5 chats = 5 user msgs + 5 assistant msgs = 10 messages
        var messages = session.Messages;
        int takeCount = maxPairs * 2;
        
        return messages.Skip(Math.Max(0, messages.Count - takeCount)).ToList();
    }

    public void AddMessageToSession(string sessionId, ChatMessage message)
    {
        var session = GetOrCreateSession(sessionId);
        session.Messages.Add(message);
        session.LastActive = DateTime.UtcNow;
    }

    public void CleanupInactiveSessions(TimeSpan inactiveThreshold)
    {
        var now = DateTime.UtcNow;
        var keysToRemove = _sessions.Where(kvp => now - kvp.Value.LastActive > inactiveThreshold)
                                    .Select(kvp => kvp.Key)
                                    .ToList();

        foreach (var key in keysToRemove)
        {
            _sessions.TryRemove(key, out _);
        }
    }
}
