using Microsoft.EntityFrameworkCore;
using STEM.Application.Interfaces;
using STEM.Core.Entities.Simulations;
using STEM.Infrastructure.Data;

namespace STEM.Infrastructure.Services;

public class AiQuotaUsageStore : IAiQuotaUsageStore
{
    private readonly StemDbContext _context;

    public AiQuotaUsageStore(StemDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetTodayUsedTokensAsync(int userId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var row = await _context.AiQuotaUsages
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.UsageDate == today, cancellationToken);
        return row?.TotalTokens ?? 0;
    }

    public async Task<int> AddTodayUsageAsync(int userId, int tokens, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var row = await _context.AiQuotaUsages
            .FirstOrDefaultAsync(u => u.UserId == userId && u.UsageDate == today, cancellationToken);

        if (row == null)
        {
            row = new AiQuotaUsage
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UsageDate = today,
                TotalTokens = tokens,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.AiQuotaUsages.Add(row);
        }
        else
        {
            row.TotalTokens += tokens;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return row.TotalTokens;
    }

    public async Task<int> GetTotalUsedByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.AiQuotaUsages
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .SumAsync(u => u.TotalTokens, cancellationToken);
    }
}
