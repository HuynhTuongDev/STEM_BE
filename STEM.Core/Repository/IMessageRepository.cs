using STEM.Core.Entities.Common;

namespace STEM.Core.Repository;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetConversationAsync(int user1Id, int user2Id, CancellationToken cancellationToken = default);
}