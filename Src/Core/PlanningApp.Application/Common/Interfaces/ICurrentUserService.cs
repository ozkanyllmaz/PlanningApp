using System;

namespace PlanningApp.Application.Common.Interfaces;

public interface ICurrentUserService
{
    // Giriş yapmış kullanıcının kimliği (WebApi'de JWT'den okunacak)
    Guid UserId { get; }
}
