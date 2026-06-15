using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BabaiBazaar.API.Data;
using BabaiBazaar.API.DTOs;
using BabaiBazaar.API.Helpers;
using BabaiBazaar.API.Models;

namespace BabaiBazaar.API.Controllers;

// ════════════════════════════════════════════════════════════════
// CATEGORIES
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/categories")]
[Tags("Catalogue")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? type, [FromQuery] string lang = "en")
    {
        var q = _db.Categories.Where(c => c.IsActive);
        if (!string.IsNullOrEmpty(type)) q = q.Where(c => c.Type == type);
        var cats = await q.OrderBy(c => c.SortOrder).ThenBy(c => c.NameEn).ToListAsync();
        return Ok(cats.Select(c => new { c.Id, NameEn = c.NameEn, NameTe = c.NameTe, c.Emoji, c.Type, c.ColorBg, c.ColorText, c.ImageUrl, c.Slug, c.SortOrder }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var cat = await _db.Categories.Include(c => c.SubCategories).FirstOrDefaultAsync(c => c.Id == id);
        return cat == null ? NotFound() : Ok(cat);
    }

    [HttpPost, Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        var cat = new Category { NameEn=req.NameEn, NameTe=req.NameTe??"", Emoji=req.Emoji??"📦", Type=req.Type??"grocery", ColorBg=req.ColorBg??"#E6F9EE", ColorText=req.ColorText??"#1E9E4F", ImageUrl=req.ImageUrl, Slug=req.Slug??(req.NameEn.ToLower().Replace(" ","-")), SortOrder=req.SortOrder };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();
        return Ok(cat);
    }

    [HttpPut("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCategoryRequest req)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.NameEn=req.NameEn; cat.NameTe=req.NameTe??""; cat.Emoji=req.Emoji??cat.Emoji; cat.Type=req.Type??cat.Type;
        cat.ColorBg=req.ColorBg??cat.ColorBg; cat.ColorText=req.ColorText??cat.ColorText; cat.ImageUrl=req.ImageUrl??cat.ImageUrl; cat.SortOrder=req.SortOrder;
        await _db.SaveChangesAsync();
        return Ok(cat);
    }

    [HttpDelete("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Category deactivated" });
    }

    // Sub-categories
    [HttpGet("{catId}/sub-categories")]
    public async Task<IActionResult> SubList(int catId)
    {
        var subs = await _db.SubCategories.Where(s => s.CategoryId == catId && s.IsActive).OrderBy(s => s.SortOrder).ToListAsync();
        return Ok(subs);
    }

    [HttpPost("{catId}/sub-categories"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SubCreate(int catId, [FromBody] CreateSubCategoryRequest req)
    {
        var cat = await _db.Categories.FindAsync(catId);
        if (cat == null) return NotFound();
        var sub = new SubCategory { CategoryId=catId, NameEn=req.NameEn, NameTe=req.NameTe??"", Emoji=req.Emoji??"📁", ImageUrl=req.ImageUrl, SortOrder=req.SortOrder };
        _db.SubCategories.Add(sub);
        await _db.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpPut("{catId}/sub-categories/{subId}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SubUpdate(int catId, int subId, [FromBody] CreateSubCategoryRequest req)
    {
        var sub = await _db.SubCategories.FirstOrDefaultAsync(s => s.Id == subId && s.CategoryId == catId);
        if (sub == null) return NotFound();
        sub.NameEn=req.NameEn; sub.NameTe=req.NameTe??""; sub.Emoji=req.Emoji??sub.Emoji; sub.ImageUrl=req.ImageUrl??sub.ImageUrl; sub.SortOrder=req.SortOrder;
        await _db.SaveChangesAsync();
        return Ok(sub);
    }

    [HttpDelete("{catId}/sub-categories/{subId}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SubDelete(int catId, int subId)
    {
        var sub = await _db.SubCategories.FirstOrDefaultAsync(s => s.Id == subId && s.CategoryId == catId);
        if (sub == null) return NotFound();
        sub.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

// ════════════════════════════════════════════════════════════════
// PRODUCTS
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/products")]
[Tags("Catalogue")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? subCategoryId, [FromQuery] int? vendorId,
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _db.Products.Include(p => p.SubCategory).Where(p => p.IsActive);
        if (subCategoryId.HasValue) q = q.Where(p => p.SubCategoryId == subCategoryId);
        if (vendorId.HasValue)      q = q.Where(p => p.VendorId == vendorId);
        if (!string.IsNullOrEmpty(search)) q = q.Where(p => p.NameEn.Contains(search) || p.NameTe.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderBy(p => p.SortOrder).ThenBy(p => p.NameEn)
            .Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var p = await _db.Products.Include(x => x.SubCategory).FirstOrDefaultAsync(x => x.Id == id);
        return p == null ? NotFound() : Ok(p);
    }

    [HttpPost, Authorize(Roles = "Admin,SuperAdmin,Vendor")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var product = new Product
        {
            NameEn=req.NameEn, NameTe=req.NameTe??"", Emoji=req.Emoji??"📦",
            SubCategoryId=req.SubCategoryId, VendorId=req.VendorId,
            Price=req.Price, Mrp=req.Mrp??req.Price, Stock=req.Stock,
            Unit=req.Unit, TagEn=req.TagEn, TagTe=req.TagTe,
            ImageUrl=req.ImageUrl, Description=req.Description, IsActive=req.IsActive
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return Ok(product);
    }

    [HttpPut("{id}"), Authorize(Roles = "Admin,SuperAdmin,Vendor")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateProductRequest req)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        p.NameEn=req.NameEn; p.NameTe=req.NameTe??""; p.Emoji=req.Emoji??p.Emoji;
        p.SubCategoryId=req.SubCategoryId; p.Price=req.Price; p.Mrp=req.Mrp??req.Price;
        p.Stock=req.Stock; p.Unit=req.Unit; p.TagEn=req.TagEn; p.TagTe=req.TagTe;
        p.ImageUrl=req.ImageUrl??p.ImageUrl; p.Description=req.Description??p.Description; p.IsActive=req.IsActive;
        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpPut("{id}/toggle-active"), Authorize(Roles = "Admin,SuperAdmin,Vendor")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = !p.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { id = p.Id, isActive = p.IsActive });
    }

    [HttpDelete("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Product deactivated" });
    }
}

// ════════════════════════════════════════════════════════════════
// SERVICES
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/services")]
[Tags("Services")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ServicesController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? category, [FromQuery] int page=1, [FromQuery] int pageSize=50)
    {
        var q = _db.Services.Where(s => s.IsActive);
        if (!string.IsNullOrEmpty(category)) q = q.Where(s => s.Category == category);
        var total = await q.CountAsync();
        var items = await q.OrderBy(s => s.SortOrder).ThenBy(s => s.NameEn).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var s = await _db.Services.Include(x=>x.Includes).Include(x=>x.Instructions).FirstOrDefaultAsync(x=>x.Id==id);
        return s == null ? NotFound() : Ok(s);
    }

    [HttpPost, Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest req)
    {
        var svc = new Service { NameEn=req.NameEn, NameTe=req.NameTe??"", Emoji=req.Emoji??"🔧", Category=req.Category??"", Price=req.Price, Mrp=req.Mrp??req.Price, DurationLabel=req.DurationLabel, ImageUrl=req.ImageUrl, Description=req.Description, IsActive=req.IsActive };
        _db.Services.Add(svc);
        await _db.SaveChangesAsync();
        return Ok(svc);
    }

    [HttpPut("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateServiceRequest req)
    {
        var svc = await _db.Services.FindAsync(id);
        if (svc == null) return NotFound();
        svc.NameEn=req.NameEn; svc.NameTe=req.NameTe??""; svc.Emoji=req.Emoji??svc.Emoji;
        svc.Category=req.Category??svc.Category; svc.Price=req.Price; svc.Mrp=req.Mrp??req.Price;
        svc.DurationLabel=req.DurationLabel??svc.DurationLabel; svc.ImageUrl=req.ImageUrl??svc.ImageUrl;
        svc.Description=req.Description??svc.Description; svc.IsActive=req.IsActive;
        await _db.SaveChangesAsync();
        return Ok(svc);
    }

    [HttpPut("{id}/toggle-active"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var svc = await _db.Services.FindAsync(id);
        if (svc == null) return NotFound();
        svc.IsActive = !svc.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { id = svc.Id, isActive = svc.IsActive });
    }

    [HttpDelete("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var svc = await _db.Services.FindAsync(id);
        if (svc == null) return NotFound();
        svc.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Service deactivated" });
    }

    // ── BOOKINGS ─────────────────────────────────────────────
    [HttpGet("bookings"), Authorize]
    public async Task<IActionResult> Bookings([FromQuery] string? status, [FromQuery] int page=1, [FromQuery] int pageSize=20)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        var q = _db.ServiceBookings.Include(b=>b.Service).Include(b=>b.User).AsQueryable();

        if (role == "Customer") q = q.Where(b => b.UserId == Uid);
        if (!string.IsNullOrEmpty(status)) q = q.Where(b => b.Status == status);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(b => b.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("bookings/{id}"), Authorize]
    public async Task<IActionResult> GetBooking(int id)
    {
        var b = await _db.ServiceBookings.Include(x=>x.Service).Include(x=>x.User).FirstOrDefaultAsync(x=>x.Id==id);
        return b == null ? NotFound() : Ok(b);
    }

    [HttpPost("bookings"), Authorize]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest req)
    {
        var svc = await _db.Services.FindAsync(req.ServiceId);
        if (svc == null || !svc.IsActive) return NotFound(new { message = "Service not found" });

        var otp = new Random().Next(1000, 9999).ToString();
        var booking = new ServiceBooking
        {
            UserId = Uid, ServiceId = req.ServiceId, Status = "CONFIRMED",
            Amount = svc.Price, Address = req.Address,
            ScheduledDate = req.ScheduledDate, TimeSlot = req.TimeSlot, Otp = otp
        };
        _db.ServiceBookings.Add(booking);
        await _db.SaveChangesAsync();
        return Ok(new { booking, customerOtp = otp });
    }

    [HttpPut("bookings/{id}/status"), Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateBookingRequest req)
    {
        var b = await _db.ServiceBookings.FindAsync(id);
        if (b == null) return NotFound();
        var old = b.Status;
        if (req.Status != null) b.Status = req.Status;
        if (req.ServicePersonId.HasValue) b.ServicePersonId = req.ServicePersonId;
        if (req.Reason != null) b.CancelReason = req.Reason;
        if (req.Status == "COMPLETED") b.CompletedAt = DateTime.UtcNow;

        _db.BookingStatusLogs.Add(new BookingStatusLog { BookingId=id, StatusFrom=old, StatusTo=b.Status, ChangedBy=Uid });
        await _db.SaveChangesAsync();
        return Ok(b);
    }

    [HttpPost("bookings/{id}/verify-otp"), Authorize]
    public async Task<IActionResult> VerifyOtp(int id, [FromBody] VerifyOtpBookingRequest req)
    {
        var b = await _db.ServiceBookings.FindAsync(id);
        if (b == null) return NotFound();
        if (b.Otp != req.Otp) return Unauthorized(new { message = "Incorrect OTP" });
        b.Status = "IN_PROGRESS";
        _db.BookingStatusLogs.Add(new BookingStatusLog { BookingId=id, StatusFrom="ASSIGNED", StatusTo="IN_PROGRESS", ChangedBy=Uid });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Job started", booking = b });
    }
}

// ════════════════════════════════════════════════════════════════
// CART
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/cart")]
[Tags("Cart & Orders")]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;
    public CartController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet, Authorize]
    public async Task<IActionResult> Get()
    {
        var items = await _db.CartItems
            .Include(c => c.Product)
            .Where(c => c.UserId == Uid)
            .ToListAsync();
        var subtotal = items.Sum(i => i.Product.Price * i.Qty);
        return Ok(new { items, subtotal, itemCount = items.Sum(i => i.Qty) });
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest req)
    {
        var product = await _db.Products.FindAsync(req.ProductId);
        if (product == null || !product.IsActive) return NotFound(new { message = "Product not found" });
        if (product.Stock < req.Qty) return BadRequest(new { message = "Insufficient stock" });

        var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == Uid && c.ProductId == req.ProductId);
        if (existing != null)
            existing.Qty += req.Qty;
        else
            _db.CartItems.Add(new CartItem { UserId = Uid, ProductId = req.ProductId, Qty = req.Qty });

        await _db.SaveChangesAsync();
        return Ok(new { message = "Added to cart" });
    }

    [HttpPut("{id}"), Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCartRequest req)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == Uid);
        if (item == null) return NotFound();
        if (req.Qty <= 0) { _db.CartItems.Remove(item); }
        else item.Qty = req.Qty;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Cart updated" });
    }

    [HttpDelete("{id}"), Authorize]
    public async Task<IActionResult> Remove(int id)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.UserId == Uid);
        if (item == null) return NotFound();
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Removed" });
    }

    [HttpDelete, Authorize]
    public async Task<IActionResult> Clear()
    {
        var items = _db.CartItems.Where(c => c.UserId == Uid);
        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Cart cleared" });
    }
}

// ════════════════════════════════════════════════════════════════
// ORDERS
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/orders")]
[Tags("Cart & Orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet, Authorize]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int page=1, [FromQuery] int pageSize=20)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        var q = _db.Orders.Include(o=>o.Items).Include(o=>o.User).AsQueryable();

        if (role == "Customer") q = q.Where(o => o.UserId == Uid);
        if (!string.IsNullOrEmpty(status)) q = q.Where(o => o.Status == status);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(o => o.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}"), Authorize]
    public async Task<IActionResult> Get(int id)
    {
        var o = await _db.Orders.Include(x=>x.Items).ThenInclude(i=>i.Product).Include(x=>x.User).FirstOrDefaultAsync(x=>x.Id==id);
        return o == null ? NotFound() : Ok(o);
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest req)
    {
        var cartItems = await _db.CartItems.Include(c => c.Product).Where(c => c.UserId == Uid).ToListAsync();
        if (!cartItems.Any()) return BadRequest(new { message = "Cart is empty" });

        // Validate stock
        foreach (var item in cartItems)
            if (item.Product.Stock < item.Qty)
                return BadRequest(new { message = $"Insufficient stock for {item.Product.NameEn}" });

        var addr = await _db.Addresses.FindAsync(req.AddressId);

        var subtotal = cartItems.Sum(i => i.Product.Price * i.Qty);
        var deliveryFee = req.DeliveryFee;
        var discount = 0m;

        var order = new Order
        {
            UserId = Uid, AddressId = req.AddressId,
            Status = req.PaymentMethod == "COD" ? "CONFIRMED" : "PENDING_PAYMENT",
            SubTotal = subtotal, DeliveryFee = deliveryFee, Discount = discount,
            TotalAmount = subtotal + deliveryFee - discount,
            PaymentMethod = req.PaymentMethod,
            PaymentStatus = req.PaymentMethod == "COD" ? "PENDING" : "PENDING",
            DeliveryAddress = addr?.FullAddress,
            Items = cartItems.Select(i => new OrderItem
            {
                ProductId   = i.ProductId,
                ProductName = i.Product.NameEn,
                UnitPrice   = i.Product.Price,
                Qty         = i.Qty,
                Total       = i.Product.Price * i.Qty
            }).ToList()
        };

        _db.Orders.Add(order);

        // Deduct stock
        foreach (var item in cartItems)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null) product.Stock -= item.Qty;
        }

        // Clear cart
        _db.CartItems.RemoveRange(cartItems);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Order placed successfully", order });
    }

    [HttpPut("{id}/status"), Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var o = await _db.Orders.FindAsync(id);
        if (o == null) return NotFound();
        o.Status = req.Status;
        if (req.Status == "DELIVERED") { o.DeliveredAt = DateTime.UtcNow; o.PaymentStatus = "PAID"; }
        if (req.Status == "CANCELLED") o.DeliveryAddress = req.Reason ?? o.DeliveryAddress;
        await _db.SaveChangesAsync();
        return Ok(o);
    }

    [HttpPut("{id}/cancel"), Authorize]
    public async Task<IActionResult> Cancel(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var o = await _db.Orders.Include(x=>x.Items).FirstOrDefaultAsync(x=>x.Id==id);
        if (o == null) return NotFound();
        if (o.Status == "DELIVERED") return BadRequest(new { message = "Cannot cancel a delivered order" });
        o.Status = "CANCELLED";
        // Restore stock
        foreach (var item in o.Items)
        {
            var p = await _db.Products.FindAsync(item.ProductId);
            if (p != null) p.Stock += item.Qty;
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Order cancelled" });
    }
}

// ════════════════════════════════════════════════════════════════
// BANNERS
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/banners")]
[Tags("Catalogue")]
public class BannersController : ControllerBase
{
    private readonly AppDbContext _db;
    public BannersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? type)
    {
        var q = _db.Banners.Where(b => b.IsActive && (b.ValidTill == null || b.ValidTill > DateTime.UtcNow));
        if (!string.IsNullOrEmpty(type)) q = q.Where(b => b.Type == type);
        return Ok(await q.OrderBy(b => b.SortOrder).ToListAsync());
    }

    [HttpPost, Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] Banner req)
    {
        _db.Banners.Add(req);
        await _db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpDelete("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await _db.Banners.FindAsync(id);
        if (b == null) return NotFound();
        b.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

// ════════════════════════════════════════════════════════════════
// UPLOAD
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/upload")]
[Tags("Upload")]
public class UploadController : ControllerBase
{
    private readonly UploadHelper _upload;
    public UploadController(UploadHelper upload) => _upload = upload;

    // POST /api/upload/image?folder=products
    [HttpPost("image"), Authorize]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> Image(IFormFile file, [FromQuery] string folder = "general")
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded" });
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowed.Contains(ext)) return BadRequest(new { message = "Only image files allowed" });

        var url = await _upload.SaveLocalAsync(file, folder);
        return Ok(new { url, imageUrl = url, filePath = url, message = "Uploaded" });
    }

    // POST /api/upload/document?folder=vendor-docs
    [HttpPost("document"), Authorize]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Document(IFormFile file, [FromQuery] string folder = "documents")
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded" });
        var url = await _upload.SaveLocalAsync(file, folder);
        return Ok(new { url, message = "Document uploaded" });
    }
}

// ════════════════════════════════════════════════════════════════
// PINCODE
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api")]
[Tags("Pincode")]
public class PincodeController : ControllerBase
{
    private readonly AppDbContext _db;
    public PincodeController(AppDbContext db) => _db = db;

    [HttpGet("pincodes")]
    public async Task<IActionResult> List() => Ok(await _db.Pincodes.Where(p => p.IsActive).OrderBy(p => p.Pincode_).ToListAsync());

    [HttpGet("pincode/check")]
    public async Task<IActionResult> Check([FromQuery] string pincode)
    {
        var p = await _db.Pincodes.FirstOrDefaultAsync(x => x.Pincode_ == pincode && x.IsActive);
        if (p == null) return Ok(new { serviceable = false, message = "Sorry, we do not deliver to this area yet." });
        return Ok(new { serviceable = true, pincode = p.Pincode_, area = p.Area, city = p.City, deliveryEta = p.DeliveryEta, message = $"We deliver to {p.Area}! ETA: {p.DeliveryEta} minutes." });
    }

    [HttpPost("pincodes"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreatePincodeRequest req)
    {
        if (await _db.Pincodes.AnyAsync(p => p.Pincode_ == req.Pincode))
            return Conflict(new { message = "Pincode already exists" });
        var p = new Pincode { Pincode_ = req.Pincode, Area = req.Area ?? "", City = req.City, State = req.State, DeliveryEta = req.DeliveryEta };
        _db.Pincodes.Add(p);
        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpDelete("pincodes/{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Pincodes.FindAsync(id);
        if (p == null) return NotFound();
        p.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Pincode removed" });
    }
    [HttpPut("pincodes/{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePincodeRequest req)
    {
        var p = await _db.Pincodes.FindAsync(id);

        if (p == null)
            return NotFound(new { message = "Pincode not found" });

        p.Pincode_ = req.Pincode;
        p.Area = req.Area ?? "";
        p.City = req.City;
        p.State = req.State;
        p.DeliveryEta = req.DeliveryEta;

        await _db.SaveChangesAsync();

        return Ok(p);
    }
}
