using System.Text;

namespace PlanningApp.Persistence.Conventions;

// Ek paket olmadan PascalCase adları snake_case'e çeviren küçük yardımcı
internal static class SnakeCase
{
    public static string Convert(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];

            if (char.IsUpper(current))
            {
                // Küçük harf/rakamdan sonra gelen büyük harf, veya büyük harf dizisinin sonundaki (ardından küçük harf gelen) harf yeni kelime başlatır
                var previousIsLowerOrDigit = i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));
                var previousIsUpper = i > 0 && char.IsUpper(name[i - 1]);
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                if (previousIsLowerOrDigit || (previousIsUpper && nextIsLower))
                    builder.Append('_');

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}
