using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Enities;

namespace WebBuild.Service;

public class ApplicationHistoryService
{
    private readonly AppDbContext _db;

    public ApplicationHistoryService(AppDbContext db)
    {
        _db = db;
    }
    public async Task LogChangeAsync(
        long applicationId,
        long employeeId,
        long? newStatusId,
        string? comment = null)
    {
        var entry = new ApplicationStatusHistory
        {
            ApplicationId = applicationId,
            ChangedByEmployeeId = employeeId,
            StatusId = newStatusId,             
            ChangeComment = comment ?? string.Empty, 
            ChangedAt = DateTime.UtcNow
        };

        _db.ApplicationStatusHistories.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<List<ApplicationStatusHistory>> GetHistoryAsync(long applicationId)
    {
        return await _db.ApplicationStatusHistories
            .Include(h => h.Status)               
            .Include(h => h.ChangedByEmployee)    
                .ThenInclude(e => e.PersonData)    
            .Where(h => h.ApplicationId == applicationId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();
    }
}
