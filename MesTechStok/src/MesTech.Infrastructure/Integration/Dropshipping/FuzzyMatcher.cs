namespace MesTech.Infrastructure.Integration.Dropshipping;

/// <summary>
/// Kategori fuzzy matching yardımcı sınıfı.
/// Levenshtein mesafesi + keyword overlap ile normalize edilmiş benzerlik skoru üretir.
/// ENT-DROP-IMP-SPRINT-D — DEV 1 Task D-02
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// Levenshtein mesafesini normalize edilmiş benzerlik skoruna dönüştürür.
    /// 1.0 = birebir eşleşme, 0.0 = tamamen farklı.
    /// </summary>
    public static decimal Similarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0m;
        if (source == target) return 1.0m;

        var distance = LevenshteinDistance(
            source.ToLowerInvariant().Trim(),
            target.ToLowerInvariant().Trim()
        );

        var maxLen = Math.Max(source.Length, target.Length);
        return 1m - (decimal)distance / maxLen;
    }

    /// <summary>
    /// Anahtar kelime örtüşmesi skoru.
    /// "Cep Telefonu Kılıfı" vs "Telefon Kılıfları" → 0.67
    /// </summary>
    public static decimal KeywordOverlap(string source, string target)
    {
        var srcWords = Tokenize(source);
        var tgtWords = Tokenize(target);

        if (!srcWords.Any() || !tgtWords.Any()) return 0m;

        var intersection = srcWords.Intersect(tgtWords).Count();
        var union        = srcWords.Union(tgtWords).Count();

        return union == 0 ? 0m : (decimal)intersection / union;
    }

    /// <summary>
    /// Kategori yolunun son kısmını çıkar.
    /// "Elektronik > Cep > Kılıf" → "Kılıf"
    /// </summary>
    public static string ExtractLeafCategory(string categoryPath)
    {
        var separators = new[] { '>', '/', '\\', '|' };
        var parts = categoryPath.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        return parts.LastOrDefault()?.Trim() ?? categoryPath;
    }

    /// <summary>
    /// Bileşik skor: %60 Levenshtein + %40 keyword overlap.
    /// </summary>
    public static decimal CombinedScore(string source, string target)
        => (Similarity(source, target) * 0.60m)
         + (KeywordOverlap(source, target) * 0.40m);

    private static IEnumerable<string> Tokenize(string text)
        => text.ToLowerInvariant()
               .Split([' ', '-', '_', '>', '/', ',', '.', '(', ')'],
                      StringSplitOptions.RemoveEmptyEntries)
               .Where(w => w.Length > 2)
               .Where(w => !StopWords.Contains(w));

    private static readonly HashSet<string> StopWords = new(
        ["ve", "ile", "için", "den", "dan", "ler", "lar", "nin", "nun",
         "the", "and", "for", "with", "from"]);

    private static int LevenshteinDistance(string s, string t)
    {
        var m = s.Length;
        var n = t.Length;
        var d = new int[m + 1, n + 1];

        for (int i = 0; i <= m; i++) d[i, 0] = i;
        for (int j = 0; j <= n; j++) d[0, j] = j;

        for (int i = 1; i <= m; i++)
        for (int j = 1; j <= n; j++)
        {
            var cost = s[i - 1] == t[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(
                Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                d[i - 1, j - 1] + cost);
        }

        return d[m, n];
    }
}
