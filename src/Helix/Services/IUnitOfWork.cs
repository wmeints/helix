namespace Helix.Services;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}