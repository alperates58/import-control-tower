using System.Threading.Tasks;

namespace ImportControlTower.Application.Services;

public interface IDocumentNumberGenerator
{
    Task<string> GenerateCaseNumberAsync(object dbContext, int year);
}
