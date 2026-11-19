using System.Globalization;
using AquaMaterials.Api.Models;
using Microsoft.Extensions.Options;

namespace AquaMaterials.Api.Services;

public class SearchService
{
    private readonly GoogleSheetService _sheets;
    private readonly GoogleSheetOptions _opt;

    public SearchService(GoogleSheetService sheets, IOptions<GoogleSheetOptions> opt)
    {
        _sheets = sheets;
        _opt = opt.Value;
    }

    // ---------- Option 1: loại kho ----------
    public static List<OptionItem> GetWarehouseTypes() => new()
    {
        new("XUATKHO", "Xuất kho"),
        new("NHAPKHO", "Nhập kho"),
        new("TONKHO",  "Tồn kho")
    };

    // ---------- Option 2: khách hàng ----------
    public async Task<List<string>> GetCustomersAsync(CancellationToken ct)
    {
        var (table, customerCol) = await LoadCatalogAsync(ct);
        return table.Rows
            .Select(r => r[customerCol])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();
    }

    // ---------- Option 3: mã hàng + tên hàng của khách ----------
    public async Task<List<ProductItem>> GetProductsAsync(string customer, CancellationToken ct)
    {
        var (table, customerCol) = await LoadCatalogAsync(ct);
        var codeCol = FindHeader(table, _opt.Headers.ItemCode);
        var nameCol = FindHeader(table, _opt.Headers.ItemName);
        var prodCodeCol = FindHeader(table, _opt.Headers.ProductCode);
        var prodNameCol = FindHeader(table, _opt.Headers.ProductName);
        var groupCol = table.Headers.FirstOrDefault(h => TextHelper.Normalize(h) == "nhom hang");

        var key = TextHelper.Normalize(customer);
        return table.Rows
            .Where(r => TextHelper.Normalize(r[customerCol]) == key)
            .Select(r => new ProductItem(
                Get(r, codeCol), Get(r, nameCol), Get(r, prodCodeCol), Get(r, prodNameCol), Get(r, groupCol)))
            .Where(p => p.ItemCode.Length > 0 || p.ItemName.Length > 0)
            .ToList();
    }

    // ---------- Option 4: số liệu cụ thể ----------
    public async Task<SearchResult> SearchAsync(
        WarehouseType type, string? customer, string? itemCode, CancellationToken ct)
    {
        var sheetName = type switch
        {
            WarehouseType.XUATKHO => _opt.Sheets.XuatKho,
            WarehouseType.NHAPKHO => _opt.Sheets.NhapKho,
            _ => _opt.Sheets.TonKho
        };

        var table = await _sheets.GetTableAsync(sheetName, ct);
        var result = new SearchResult
        {
            Type = type.ToString(),
            Sheet = sheetName,
            Customer = customer,
            ItemCode = itemCode,
            Columns = table.Headers
        };

        var codeCol = FindHeader(table, _opt.Headers.ItemCode);
        var custCol = FindHeader(table, _opt.Headers.Customer);
        IEnumerable<Dictionary<string, string>> rows = table.Rows;

        if (!string.IsNullOrWhiteSpace(itemCode))
        {
            var code = TextHelper.Normalize(itemCode);
            rows = codeCol != null
                ? rows.Where(r => TextHelper.Normalize(r[codeCol]) == code)
                : rows.Where(r => r.Values.Any(v => TextHelper.Normalize(v) == code));
            if (codeCol == null)
                result.Note = "Sheet này không có cột mã vật tư rõ ràng, đã dò khớp trên tất cả các cột.";
        }
        else if (!string.IsNullOrWhiteSpace(customer))
        {
            if (custCol != null)
            {
                var key = TextHelper.Normalize(customer);
                rows = rows.Where(r => TextHelper.Normalize(r[custCol]) == key);
            }
            else if (codeCol != null)
            {
                // Sheet kho không có cột khách hàng -> lấy danh sách mã vật tư của khách từ DANHMUCHANG
                var products = await GetProductsAsync(customer, ct);
                var codes = products.Select(p => TextHelper.Normalize(p.ItemCode))
                                    .Where(c => c.Length > 0).ToHashSet();
                rows = rows.Where(r => codes.Contains(TextHelper.Normalize(r[codeCol])));
            }
            else
            {
                result.Note = "Không tìm thấy cột khách hàng/mã vật tư trong sheet nên không lọc được.";
            }
        }

        result.Rows = rows.ToList();
        result.Total = result.Rows.Count;

        // Chỉ cộng tổng khi đã chọn 1 mã vật tư cụ thể (khác mã thì khác ĐVT, cộng lại không có nghĩa)
        if (!string.IsNullOrWhiteSpace(itemCode))
        {
            foreach (var h in table.Headers.Where(h => _opt.Headers.Quantity.Contains(TextHelper.Normalize(h))))
            {
                double sum = 0; bool any = false;
                foreach (var r in result.Rows)
                    if (TryParseNumber(r[h], out var v)) { sum += v; any = true; }
                if (any) result.Totals[h] = sum;
            }
        }
        return result;
    }

    // ---------- helpers ----------
    private async Task<(SheetTable table, string customerCol)> LoadCatalogAsync(CancellationToken ct)
    {
        var table = await _sheets.GetTableAsync(_opt.Sheets.Catalog, ct);
        var col = FindHeader(table, _opt.Headers.Customer)
                  ?? throw new InvalidOperationException(
                      $"Tab '{_opt.Sheets.Catalog}' không có cột khách hàng. Sửa Headers.Customer trong appsettings.json.");
        return (table, col);
    }

    /// <summary>Tìm cột theo danh sách alias, alias đứng trước được ưu tiên.</summary>
    private static string? FindHeader(SheetTable table, List<string> aliases)
    {
        foreach (var alias in aliases)
        {
            var hit = table.Headers.FirstOrDefault(h => TextHelper.Normalize(h) == alias);
            if (hit != null) return hit;
        }
        return null;
    }

    private bool TryParseNumber(string? s, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (_opt.DecimalComma) s = s.Replace(".", "").Replace(",", ".");
        else s = s.Replace(",", "");
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string Get(Dictionary<string, string> row, string? col)
        => col != null && row.TryGetValue(col, out var v) ? v : "";
}
