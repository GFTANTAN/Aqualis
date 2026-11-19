namespace AquaMaterials.Api.Models;

public enum WarehouseType
{
    XUATKHO,
    NHAPKHO,
    TONKHO
}

public record OptionItem(string Value, string Label);

public record ProductItem(
    string ItemCode,       // Mã bao bì / mã vật tư, vd: DT.EOEM1
    string ItemName,       // Quy cách / tên vật tư, vd: Hộp giấy Trà lựu EOEM 1990
    string ProductCode,    // Mã hàng, vd: DT.EOEM
    string ProductName,    // Tên sản phẩm, vd: Trà lựu EOEM 1990
    string Group);         // Nhóm hàng: Bao bì / Tem nhãn / Chai lọ

public class SearchResult
{
    public string Type { get; set; } = "";
    public string Sheet { get; set; } = "";
    public string? Customer { get; set; }
    public string? ItemCode { get; set; }
    public int Total { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, string>> Rows { get; set; } = new();
    public Dictionary<string, double> Totals { get; set; } = new();
    public string? Note { get; set; }
}

public class SheetTable
{
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, string>> Rows { get; set; } = new();
}
