using Microsoft.EntityFrameworkCore;
using STEM.Core.Entities.Projects;
using STEM.Core.Repository;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Repositories;

public class FileEntityRepository : Repository<SubmissionFile>, IFileRepository
{
    public FileEntityRepository(StemDbContext context) : base(context)
    {
    }
}