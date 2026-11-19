using System.Text;
using AquaMaterials.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AquaMaterials.Api.Services;

/// <summary>
/// Đọc 1 tab của Google Sheet (chế độ "Anyone with the link can view") dưới dạng CSV,
/// không cần API key. Kết quả được cache trong bộ nhớ.
/// </summary>
public class GoogleSheetService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly GoogleSheetOptions _opt;

    public GoogleSheetService(HttpClient http, IMemoryCache cache, IOptions<GoogleSheetOptions> opt)
    {
        _http = http;
        _cache = cache;
        _opt = opt.Value;
    }

    public async Task<SheetTable> GetTableAsync(string sheetName, CancellationToken ct = default)
    {
        var table = await _cache.GetOrCreateAsync($"sheet:{sheetName}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(0, _opt.CacheSeconds));
            return await LoadAsync(sheetName, ct);
        });
        return table!;
    }

    private async Task<SheetTable> LoadAsync(string sheetName, CancellationToken ct)
    {
        var url = $"https://docs.google.com/spreadsheets/d/{_opt.SpreadsheetId}/gviz/tq" +
                  $"?tqx=out:csv&headers=1&sheet={Uri.EscapeDataString(sheetName)}";

        using var resp = await _http.GetAsync(url, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode || text.TrimStart().StartsWith('<'))
            throw new InvalidOperationException(
                $"Không đọc được tab '{sheetName}'. Kiểm tra: tên tab đúng chưa, và sheet đã bật " +
                "chia sẻ 'Anyone with the link - Viewer' chưa.");

        var grid = ParseCsv(text.TrimStart('\uFEFF'));
        var table = new SheetTable();
        if (grid.Count == 0) return table;

        // Header: đặt tên cho cột rỗng/trùng để không bị lỗi key
        var seen = new Dictionary<string, int>();
        for (int i = 0; i < grid[0].Count; i++)
        {
            var h = grid[0][i].Trim();
            if (h.Length == 0) h = $"Cột {i + 1}";
            if (seen.TryGetValue(h, out var n)) { seen[h] = n + 1; h = $"{h} ({n + 1})"; }
            else seen[h] = 1;
            table.Headers.Add(h);
        }

        var fillKeys = _opt.Headers.FillDown;
        var last = new Dictionary<string, string>();

        foreach (var cells in grid.Skip(1))
        {
            var row = new Dictionary<string, string>();
            for (int i = 0; i < table.Headers.Count; i++)
                row[table.Headers[i]] = i < cells.Count ? cells[i].Trim() : "";

            if (row.Values.All(string.IsNullOrWhiteSpace)) continue;

            // Ô gộp (merged) trong Google Sheet chỉ có giá trị ở dòng đầu -> điền xuống các dòng dưới
            foreach (var h in table.Headers)
            {
                if (!fillKeys.Contains(TextHelper.Normalize(h))) continue;
                if (string.IsNullOrWhiteSpace(row[h]))
                {
                    if (last.TryGetValue(h, out var v)) row[h] = v;
                }
                else last[h] = row[h];
            }
            table.Rows.Add(row);
        }
        return table;
    }

    /// <summary>Parser CSV chuẩn RFC 4180 (hỗ trợ ô có xuống dòng, dấu phẩy, nháy kép).</summary>
    private static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else sb.Append(c);
            }
            else
            {
                switch (c)
                {
                    case '"': inQuotes = true; break;
                    case ',': row.Add(sb.ToString()); sb.Clear(); break;
                    case '\r': break;
                    case '\n':
                        row.Add(sb.ToString()); sb.Clear();
                        rows.Add(row); row = new List<string>();
                        break;
                    default: sb.Append(c); break;
                }
            }
        }
        if (sb.Length > 0 || row.Count > 0) { row.Add(sb.ToString()); rows.Add(row); }
        return rows;
    }
}
