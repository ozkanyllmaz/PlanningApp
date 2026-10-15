using System;

namespace PlanningApp.Application.Common.Models;

// Identity kullanıcısının Application'a açılan minimal görünümü (Identity tipleri sızdırılmaz)
public sealed record AppUserInfo(Guid UserId, string Email);
