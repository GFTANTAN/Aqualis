namespace AquaMaterials.Api.Models;

public class GoogleSheetOptions
{
    public string SpreadsheetId { get; set; } = "";
    public int CacheSeconds { get; set; } = 60;
    /// <summary>true: số kiểu Việt Nam (1.030 = một nghìn không trăm ba mươi, 24,9 = 24.9).</summary>
    public bool DecimalComma { get; set; } = true;
    public SheetNames Sheets { get; set; } = new();
    public HeaderAliases Headers { get; set; } = new();
}

public class SheetNames
{
    public string Catalog { get; set; } = "QUYCACHBAOBI";
    public string NhapKho { get; set; } = "NHAPKHO";
    public string XuatKho { get; set; } = "XUATKHO";
    public string TonKho { get; set; } = "TONKHO";
}

/// <summary>Tên cột (đã bỏ dấu, viết thường) để tự nhận diện cột trong từng sheet.</summary>
public class HeaderAliases
{
    public List<string> Customer { get; set; } = new();
    public List<string> ItemCode { get; set; } = new();
    public List<string> ItemName { get; set; } = new();
    public List<string> ProductCode { get; set; } = new();
    public List<string> ProductName { get; set; } = new();
    public List<string> Quantity { get; set; } = new();
    public List<string> FillDown { get; set; } = new();
}
