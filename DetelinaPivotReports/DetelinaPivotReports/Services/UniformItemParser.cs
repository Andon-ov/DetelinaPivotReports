using System;
using System.Text.RegularExpressions;

namespace DetelinaPivotReports.Services;

/// <summary>
/// Логика за извличане и нормализация на модел и размер от наименование на артикул от продажбите (SPLU_NAME).
/// Съобразена със спецификите на българската търговска номенклатура за училищни униформи.
/// </summary>
public static class UniformItemParser
{
    // Регулярен израз за откриване на размер в самия край на SPLU_NAME
    // Поддържа:
    // - Буквени размери (Latin и кирилица): 4XL, 3XL, 2XL, XXXL, XXL, XL, 4XS, 3XS, 2XS, XXS, XS, S, M, L
    // - Числови номера и детски ръстове: 28, 30, 32, 34, 36, 38, 40, 42, 44, 46, 48, 50... 116, 122... както и сдвоени (116/122, 128-134)
    // - Незадължителни ограждащи скоби (напр. "(36)", "[L]")
    private static readonly Regex SizeRegex = new(
        @"(?:[\(\[\{]\s*|\b)(4XL|3XL|2XL|XXXL|XXL|XL|4XS|3XS|2XS|XXS|XS|S|M|L|4ХЛ|3ХЛ|2ХЛ|ХХХЛ|ХХЛ|ХЛ|4ХС|3ХС|2ХС|ХХС|ХС|С|М|Л|\d{2,3}(?:\s*[\/\-]\s*\d{2,3})?)\s*[\)\]\}]?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Проверка за текстов размер "Универсален"
    private static readonly Regex UniversalSizeRegex = new(
        @"(?:[\(\[\{]\s*|\b)(универсален|универсална|universal|one\s*size)\s*[\)\]\}]?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Разделя наименованието на артикула на (Базов модел, Размер).
    /// Базовият модел е целият текст ПРЕДИ отделения размер.
    /// </summary>
    public static (string ModelName, string Size) Parse(string rawArticleName)
    {
        if (string.IsNullOrWhiteSpace(rawArticleName))
            return ("Неизвестен артикул", "Без размер");

        string name = rawArticleName.Trim();

        // 1. Проверка за размер в самия край на низа
        var match = SizeRegex.Match(name);
        if (match.Success)
        {
            string rawSize = match.Groups[1].Value;
            string rawModel = name.Substring(0, match.Index);
            string model = CleanModelName(rawModel);
            string size = NormalizeSize(rawSize);

            return (string.IsNullOrWhiteSpace(model) ? name : model, size);
        }

        // 2. Проверка за универсален размер в края
        var mUni = UniversalSizeRegex.Match(name);
        if (mUni.Success)
        {
            string rawModel = name.Substring(0, mUni.Index);
            string model = CleanModelName(rawModel);
            return (string.IsNullOrWhiteSpace(model) ? name : model, "Универсален");
        }

        // 3. Не е открит размер в края -> артикул без размер (напр. емблема, вратовръзка)
        return (name, "Без размер");
    }

    /// <summary>
    /// Нормализира размера към стандартен вид (кирилица -> латиница, обединяване на дублиращи се означения).
    /// </summary>
    public static string NormalizeSize(string rawSize)
    {
        if (string.IsNullOrWhiteSpace(rawSize)) return "Без размер";

        string s = rawSize.Trim().ToUpperInvariant();

        // Замяна на кирилски букви с латинските им оптични еквиваленти
        s = s.Replace('Х', 'X')
             .Replace('С', 'S')
             .Replace('М', 'M')
             .Replace('Л', 'L')
             .Replace('В', 'B');

        if (s == "УНИВЕРСАЛЕН" || s == "УНИВЕРСАЛНА" || s == "UNIVERSAL" || s == "ONE SIZE" || s == "ONESIZE")
            return "Универсален";

        if (s == "БЕЗ РАЗМЕР" || s == "-" || s == "НЯМА")
            return "Без размер";

        // Нормализация на сдвоени размери: 116-122 или 116/122 -> 116/122
        var dualMatch = Regex.Match(s, @"^(\d{2,3})\s*[\/\-]\s*(\d{2,3})$");
        if (dualMatch.Success)
        {
            return $"{dualMatch.Groups[1].Value}/{dualMatch.Groups[2].Value}";
        }

        // Стандартизиране на кратки означения
        if (s == "2X") return "2XL";
        if (s == "3X") return "3XL";
        if (s == "4X") return "4XL";
        if (s == "5X") return "5XL";
        if (s == "2XS") return "XXS";
        if (s == "XXXL") return "3XL";

        return s;
    }

    /// <summary>
    /// Изчиства паразитни разделители и префикси на края на базовото наименование на модела.
    /// </summary>
    private static string CleanModelName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        string cleaned = name.Trim();
        // 1. Премахва висящи тирета, наклонени черти, отварящи скоби, двоеточия, запетаи и разделители на края
        cleaned = Regex.Replace(cleaned, @"[\s\-–—\/,;:(\[{]+$", string.Empty);
        // 2. Премахва висящи префикси като "р-р", "р.", "размер", "разм.", "№", "no.", "номер" на края
        cleaned = Regex.Replace(cleaned, @"(?:\s*[-–—\/,]\s*|\s+)(?:р-р|р\.|размер|разм\.|№|no\.?|номер)\s*[:.]?$", string.Empty, RegexOptions.IgnoreCase);
        // 3. Отново премахва евентуални разделители преди префикса
        cleaned = Regex.Replace(cleaned, @"[\s\-–—\/,;:(\[{]+$", string.Empty);
        return cleaned.Trim();
    }

    /// <summary>
    /// Генерира ключ за логическо сортиране на размерите:
    /// 1. Първо числови номера и детски ръстове по възходящ ред (28, 30, 32... 116, 122...).
    /// 2. След тях буквени размери (XXS, XS, S, M, L, XL, XXL, 2XL, 3XL, 4XL...).
    /// 3. Универсален размер.
    /// 4. Без размер (винаги накрая).
    /// </summary>
    public static string GetSortOrderKey(string rawSize)
    {
        string s = NormalizeSize(rawSize);

        // 1. Сдвоен числов размер (116/122): подрежда се непосредствено след 116
        var dual = Regex.Match(s, @"^(\d{2,3})\s*[\/\-]\s*(\d{2,3})$");
        if (dual.Success && decimal.TryParse(dual.Groups[1].Value, out decimal d1))
        {
            return $"1_{d1 + 0.5m:0000.0}_{s}";
        }

        // 2. Единичен числов размер (числови номера 28-50, детски ръстове 116-164)
        if (decimal.TryParse(s, out decimal num))
        {
            return $"1_{num:0000.0}_{s}";
        }

        // 3. Стандартни буквени размери в текстила
        int letterRank = s switch
        {
            "4XS" or "XXXXS" => 1,
            "3XS"            => 2,
            "XXS" or "2XS"   => 3,
            "XS"             => 4,
            "S"              => 5,
            "M"              => 6,
            "L"              => 7,
            "XL"             => 8,
            "XXL" or "2XL"   => 9,
            "3XL" or "XXXL"  => 10,
            "4XL" or "XXXXL" => 11,
            "5XL" or "XXXXXL"=> 12,
            "6XL"            => 13,
            _ => -1
        };

        if (letterRank > 0)
        {
            return $"2_{letterRank:02}_{s}";
        }

        // 4. Универсален
        if (s.Equals("Универсален", StringComparison.OrdinalIgnoreCase))
        {
            return "3_00_Универсален";
        }

        // 5. Без размер (винаги в най-долната част на редовете)
        if (s.Equals("Без размер", StringComparison.OrdinalIgnoreCase))
        {
            return "9_99_БезРазмер";
        }

        // 6. Нестандартен размер
        return $"4_00_{s}";
    }
}
