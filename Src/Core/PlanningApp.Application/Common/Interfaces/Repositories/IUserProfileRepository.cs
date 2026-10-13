using System;
using PlanningApp.Domain.Users;

namespace PlanningApp.Application.Common.Interfaces.Repositories;

public interface IUserProfileRepository
{
    // UserProfile.Id, kimlik kullanıcı Id'si ile aynıdır; kayıt yoksa null döner
    Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
}
