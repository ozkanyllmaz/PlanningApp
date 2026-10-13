using System;
using PlanningApp.Domain.Common;

namespace PlanningApp.Domain.Users;

public sealed class UserProfile
{
    public Guid Id { get; private set; }            // Identity kullanıcı Id'si ile aynı
    public string TimeZoneId { get; private set; }  // IANA, örn. Europe/Istanbul
    public DateTimeOffset CreatedAt { get; private set; }

    private UserProfile(Guid id, string timeZoneId, DateTimeOffset createdAt)
    {
        Id = id;
        TimeZoneId = timeZoneId;
        CreatedAt = createdAt;
    }

    public static UserProfile Create(Guid id, string timeZoneId, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new DomainException("Kullanıcı kimliği boş olamaz.");

        EnsureValidTimeZone(timeZoneId);

        return new UserProfile(id, timeZoneId.Trim(), createdAt);
    }

    public void ChangeTimeZone(string timeZoneId)
    {
        EnsureValidTimeZone(timeZoneId);
        TimeZoneId = timeZoneId.Trim();
    }

    // Verilen anın kullanıcının saat dilimindeki takvim gününü döner ("yerel bugün" hesabı)
    public DateOnly GetLocalDate(DateTimeOffset instant)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);
    }

    private static void EnsureValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId) ||
            !TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out _))
            throw new DomainException("Geçerli bir saat dilimi kimliği verilmelidir.");
    }
}
