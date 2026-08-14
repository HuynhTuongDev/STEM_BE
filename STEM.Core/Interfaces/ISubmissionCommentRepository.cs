using STEM.Core.Entities.Projects;

namespace STEM.Core.Repository;

public interface ISubmissionCommentRepository : IRepository<SubmissionComment>
{
    Task<IEnumerable<SubmissionComment>> GetBySubmissionIdAsync(int submissionId, CancellationToken cancellationToken = default);
}
