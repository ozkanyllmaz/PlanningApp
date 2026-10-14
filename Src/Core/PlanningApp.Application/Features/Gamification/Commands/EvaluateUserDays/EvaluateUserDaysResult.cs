namespace PlanningApp.Application.Features.Gamification.Commands.EvaluateUserDays;

// Bildirimler için: bu çalıştırmada değerlendirilen gün sayısı ve güncel seri değerleri
public sealed record EvaluateUserDaysResult(int EvaluatedDayCount, int CurrentStreak, int LongestStreak);
