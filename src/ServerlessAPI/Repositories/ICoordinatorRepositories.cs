using ServerlessAPI.Dtos;

namespace ServerlessAPI.Repositories;

public interface IInstitutionalConfigurationRepository
{
    Task<InstitutionalConfigurationResponse?> GetAsync(
        CancellationToken ct = default);

    Task<InstitutionalConfigurationResponse> UpsertAsync(
        int userId,
        UpdateInstitutionalConfigurationRequest request,
        CancellationToken ct = default);
}

public interface ICoordinatorProfileRepository
{
    Task<CoordinatorProfileResponse?> GetAsync(
        int userId,
        CancellationToken ct = default);

    Task<CoordinatorProfileResponse> UpdateAsync(
        int userId,
        UpdateCoordinatorProfileRequest request,
        CancellationToken ct = default);
}

public interface ICoordinatorAnnouncementRepository
{
    Task<IReadOnlyList<CoordinatorAnnouncementResponse>> GetAsync(
        int userId,
        string? status,
        CancellationToken ct = default);

    Task<CoordinatorAnnouncementResponse> CreateAsync(
        int userId,
        CreateCoordinatorAnnouncementRequest request,
        CancellationToken ct = default);

    Task<CoordinatorAnnouncementResponse> UpdateAsync(
        int userId,
        int announcementId,
        UpdateCoordinatorAnnouncementRequest request,
        CancellationToken ct = default);
}
