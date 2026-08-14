using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class SubmissionCommentRepository : Repository<SubmissionComment>, ISubmissionCommentRepository
{
    public SubmissionCommentRepository(StemDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SubmissionComment>> GetBySubmissionIdAsync(
        int submissionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(comment => comment.Author)
                .ThenInclude(author => author!.Role)
            .Where(comment => comment.SubmissionId == submissionId)
            .OrderBy(comment => comment.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
