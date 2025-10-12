using Helix.Data;

namespace Helix.Services;

public class UnitOfWork(ApplicationDbContext applicationDbContext): IUnitOfWork
{
    public async Task SaveChangesAsync()
    {
        await applicationDbContext.SaveChangesAsync();
    }
}