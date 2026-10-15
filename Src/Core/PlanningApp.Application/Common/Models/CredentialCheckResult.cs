using System;

namespace PlanningApp.Application.Common.Models;

// Başarısızsa (bilinmeyen e-posta, yanlış parola, kilitli hesap) UserId ve Email null'dır
public sealed record CredentialCheckResult(bool Succeeded, Guid? UserId, string? Email);
