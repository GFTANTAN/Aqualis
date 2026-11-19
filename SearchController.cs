using AquaMaterials.Api.Models;
using AquaMaterials.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AquaMaterials.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly SearchService _search;
    public SearchController(SearchService search) => _search = search;

    /// <summary>Option 1: XUẤT KHO / NHẬP KHO / TỒN KHO</summary>
    [HttpGet("warehouse-types")]
    public ActionResult<List<OptionItem>> WarehouseTypes() => SearchService.GetWarehouseTypes();

    /// <summary>Option 2: danh sách tên khách hàng</summary>
    [HttpGet("customers")]
    public async Task<IActionResult> Customers(CancellationToken ct)
        => await Safe(() => _search.GetCustomersAsync(ct));

    /// <summary>Option 3: mã hàng + tên hàng của 1 khách hàng</summary>
    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] string customer, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customer)) return BadRequest(new { error = "Thiếu tham số customer." });
        return await Safe(() => _search.GetProductsAsync(customer, ct));
    }

    /// <summary>
    /// Option 4: số liệu cụ thể từ sheet XUATKHO / NHAPKHO / TONKHO.
    /// Truyền itemCode để lọc đúng 1 mã; chỉ truyền customer để lấy toàn bộ mã của khách.
    /// </summary>
    [HttpGet("data")]
    public async Task<IActionResult> Data(
        [FromQuery] WarehouseType type,
        [FromQuery] string? customer,
        [FromQuery] string? itemCode,
        CancellationToken ct)
        => await Safe(() => _search.SearchAsync(type, customer, itemCode, ct));

    private async Task<IActionResult> Safe<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (InvalidOperationException ex) { return StatusCode(502, new { error = ex.Message }); }
        catch (HttpRequestException ex) { return StatusCode(502, new { error = "Không kết nối được Google Sheets: " + ex.Message }); }
    }
}
