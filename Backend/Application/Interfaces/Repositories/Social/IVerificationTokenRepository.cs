using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

public interface IVerificationTokenRepository : IGenericRepository<VerificationToken>
{
    Task<VerificationToken?> GetValidTokenAsync(Guid userId, VerificationPurpose purpose, string tokenHash, CancellationToken cancellationToken = default);
    Task<VerificationToken?> GetLatestTokenAsync(Guid userId, VerificationPurpose purpose, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VerificationToken>> GetActiveTokensAsync(Guid userId, VerificationPurpose purpose, CancellationToken cancellationToken = default);
}
