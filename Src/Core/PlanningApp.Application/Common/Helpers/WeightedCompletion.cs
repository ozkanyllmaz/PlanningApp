using System;
using System.Collections.Generic;
using System.Linq;
using PlanningApp.Domain.Tasks;

namespace PlanningApp.Application.Common.Helpers;

// Günün ağırlıklı tamamlanma yüzdesinin tek hesaplama noktası.
// Hem Query'ler hem gün değerlendirmesi bunu kullanır; iki yerde farklı sonuç çıkmamalı.
public static class WeightedCompletion
{
    // Ağırlık: (int)Priority (Normal=1, Important=2, Critical=3).
    // Görev yoksa 0 değil null döner (boş gün ile %0 başarı ayrışsın).
    public static decimal? Calculate(IEnumerable<PlanningTask> tasks)
    {
        var taskList = tasks.ToList();
        if (taskList.Count == 0)
            return null;

        var totalWeight = taskList.Sum(task => (decimal)(int)task.Priority);
        var completedWeight = taskList
            .Where(task => task.IsCompleted)
            .Sum(task => (decimal)(int)task.Priority);

        return decimal.Round(completedWeight / totalWeight * 100m, 2, MidpointRounding.AwayFromZero);
    }
}
