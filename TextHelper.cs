using System.Globalization;
using System.Text;

namespace AquaMaterials.Api.Services;

public static class TextHelper
{
    /// <summary>Bỏ dấu tiếng Việt, viết thường, gộp khoảng trắng. "Duyên Thị" -> "duyen thi".</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var s = input.Trim().Replace('đ', 'd').Replace('Đ', 'D')
                      .Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(c));
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
