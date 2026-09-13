using System;
using System.Text.RegularExpressions;

namespace DetelinaPivotReports.Services;

/// <summary>
/// Логика за извличане и нормализация на модел и размер от наименование на артикул (PLU_NAME).
/// Съобразена със спецификите на българската търговска номенклатура за училищни униформи.
/// </summary>
public static class UniformItemParser
{
    // 1. Размер в скоби на края на името: напр. "Тениска к.р. (128)", "Суитшърт (L)", "Панталон (146/152)"
    private static readonly Regex TrailingParenthesesSizeRegex = new(
        @"^(.*?)\s*[\(\[\{]\s*(?:р-р|р\.|размер|no|№)?\s*([0-9]{2,3}(?:\s*[\/\-]\s*[0-9]{2,3})?|[1-6]?[xXхХ]{1,4}[sSсС]|[1-6]?[xXхХ]{0,4}[sSсСmMмLlл]|[1-6]?[xXхХ]+[lLл]?|[0-9]{2,3})\s*[\)\]\}]\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // 2. Размер на края на стринга, предшестван от разделител или префикс (р-р, р., размер, №, No, тире, наклонена черта)
    private static readonly Regex TrailingSizeWithPrefixRegex = new(
        @"^(.*?)(?:\s*[-–—\/,]\s*|\s+)(?:р-р|р\.|размер|разм\.|№|no\.?|номер)\s*[:.]?\s*([0-9]{2,3}(?:\s*[\/\-]\s*[0-9]{2,3})?|[1-6]?[xXхХ]{1,4}[sSсС]|[1-6]?[xXхХ]{0,4}[sSсСmMмLlл]|[1-6]?[xXхХ]+[lLл]?|[0-9]{2,3})\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // 3. Директен размер на края на стринга: числов (116, 122...) или буквен (XS, S, M, L, XL, 2XL...)
    private static readonly Regex TrailingSizeDirectRegex = new(
        @"^(.*?)(?:\s*[-–—\/]\s*|\s+)([0-9]{2,3}(?:\s*[\/\-]\s*[0-9]{2,3})|[1-6]?[xXхХ]{1,4}[sSсС]|[1-6]?[xXхХ]{0,4}[sSсСmMмLlл]|[1-6]?[xXхХ]+[lLл]?|[0-9]{2,3})\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // 4. Текстов размер "Универсален"
    private static readonly Regex UniversalSizeRegex = new(
        @"^(.*?)(?:\s*[-–—\/,]\s*|\s+)(?:р-р|р\.)?\s*(универсален|универсална|universal|one\s*size)\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Разделя наименованието на артикула на (Базов модел, Размер).
    /// </summary>
    public static (string ModelName, string Size) Parse(string rawArticleName)
    {
        if (string.IsNullOrWhiteSpace(rawArticleName))
            return ("Неизвестен артикул", "Без размер");

        string name = rawArticleName.Trim();

        // 1. Проверка за размер в скоби: "Тениска (128)"
        var mParen = TrailingParenthesesSizeRegex.Match(name);
        if (mParen.Success)
        {
            string model = CleanModelName(mParen.Groups[1].Value);
            string size = NormalizeSize(mParen.Groups[2].Value);
            return (string.IsNullOrWhiteSpace(model) ? name : model, size);
        }

        // 2. Проверка за размер с префикс: "Тениска р-р 128", "Суитшърт - М"
        var mPrefix = TrailingSizeWithPrefixRegex.Match(name);
        if (mPrefix.Success)
        {
            string model = CleanModelName(mPrefix.Groups[1].Value);
            string size = NormalizeSize(mPrefix.Groups[2].Value);
            return (string.IsNullOrWhiteSpace(model) ? name : model, size);
        }

        // 3. Проверка за универсален размер
        var mUni = UniversalSizeRegex.Match(name);
        if (mUni.Success)
        {
            string model = CleanModelName(mUni.Groups[1].Value);
            return (string.IsNullOrWhiteSpace(model) ? name : model, "Универсален");
        }

        // 4. Проверка за директен размер на края: "Тениска 128", "Блуза XL"
        var mDirect = TrailingSizeDirectRegex.Match(name);
        if (mDirect.Success)
        {
            string candidateModel = CleanModelName(mDirect.Groups[1].Value);
            string candidateSize = NormalizeSize(mDirect.Groups[2].Value);

            // Ако след премахване на размера остава празен модел (напр. артикулът се казва само "128"),
            // го оставяме като име на модела
            if (!string.IsNullOrWhiteSpace(candidateModel))
            {
                return (candidateModel, candidateSize);
            }
        }

        // 5. Не е открит специфичен размер -> артикул без размер (напр. емблема, вратовръзка)
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

        // Стандартизиране на XXL -> 2XL, XXXL -> 3XL и др.
        if (s == "XXL" || s == "2X" || s == "2XL") return "2XL";
        if (s == "XXXL" || s == "3X" || s == "3XL") return "3XL";
        if (s == "XXXXL" || s == "4X" || s == "4XL") return "4XL";
        if (s == "XXXXXL" || s == "5X" || s == "5XL") return "5XL";
        if (s == "XXS" || s == "2XS") return "2XS";
        if (s == "XXXS" || s == "3XS") return "3XS";
        if (s == "XXXXS" || s == "4XS") return "4XS";

        return s;
    }

    /// <summary>
    /// Изчиства паразитни символи на края на базовото наименование на модела.
    /// </summary>
    private static string CleanModelName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        string cleaned = name.Trim();
        // Премахва висящи тирета, наклонени черти и разделители на края
        cleaned = Regex.Replace(cleaned, @"[\s\-–—\/,;]+$", string.Empty);
        // Премахва висящи префикси като "р-р", "размер", "№", ако са останали
        cleaned = Regex.Replace(cleaned, @"(?:\s*[-–—\/,]\s*|\s+)(?:р-р|р\.|размер|разм\.|№|no\.?|номер)\s*$", string.Empty, RegexOptions.IgnoreCase);
        return cleaned.Trim();
    }

    /// <summary>
    /// Генерира ключ за логическо сортиране на размерите:
    /// 1. Детски височинни номера (напр. 98, 104, 110, 116, 116/122, 122... 182) във възходящ числов ред.
    /// 2. Стандартни буквени размери (4XS -> 3XS -> 2XS -> XS -> S -> M -> L -> XL -> 2XL -> 3XL -> 4XL...).
    /// 3. Универсален размер.
    /// 4. Без размер (накрая).
    /// </summary>
    public static string GetSortOrderKey(string rawSize)
    {
        string s = NormalizeSize(rawSize);

        // 1. Сдвоен числов размер (116/122): подрежда се непосредствено след 116
        var dual = Regex.Match(s, @"^(\d{2,3})\/(\d{2,3})$");
        if (dual.Success && decimal.TryParse(dual.Groups[1].Value, out decimal d1))
        {
            return $"1_{d1 + 0.5m:000.0}_{s}";
        }

        // 2. Единичен числов размер (детски ръст: 92, 104, 116, 128...)
        if (decimal.TryParse(s, out decimal num))
        {
            return $"1_{num:000.0}_{s}";
        }

        // 3. Стандартни буквени размери в текстила
        int letterRank = s switch
        {
            "4XS" => 1,
            "3XS" => 2,
            "2XS" => 3,
            "XS"  => 4,
            "S"   => 5,
            "M"   => 6,
            "L"   => 7,
            "XL"  => 8,
            "2XL" => 9,
            "3XL" => 10,
            "4XL" => 11,
            "5XL" => 12,
            "6XL" => 13,
            _ => -1
        };

        if (letterRank > 0)
        {
            return $"2_{letterRank:00}_{s}";
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
