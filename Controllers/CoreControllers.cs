using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BabaiBazaar.API.Data;
using BabaiBazaar.API.DTOs;
using BabaiBazaar.API.Helpers;
using BabaiBazaar.API.Models;

namespace BabaiBazaar.API.Controllers;

// ════════════════════════════════════════════════════════════════
// VENDORS
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/vendors")]
[Tags("Vendors")]
public class VendorController : ControllerBase
{
    private readonly AppDbContext _db;
    public VendorController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet, Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int page=1, [FromQuery] int pageSize=50)
    {
        var q = _db.Vendors.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(v => v.Status == status);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(v => v.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}"), Authorize]
    public async Task<IActionResult> Get(int id)
    {
        var v = await _db.Vendors.Include(x => x.Documents).FirstOrDefaultAsync(x => x.Id == id);
        return v == null ? NotFound() : Ok(v);
    }

    [HttpGet("my"), Authorize(Roles = "Vendor")]
    public async Task<IActionResult> MyProfile()
    {
        var mapping = await _db.VendorUserMappings.FirstOrDefaultAsync(m => m.UserId == Uid);
        if (mapping == null) return NotFound(new { message = "You are not linked to a vendor account" });
        var v = await _db.Vendors.Include(x=>x.Documents).FirstOrDefaultAsync(x=>x.Id==mapping.VendorId);
        return v == null ? NotFound() : Ok(v);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterVendorRequest req)
    {
        var vendor = new Vendor
        {
            BusinessName=req.BusinessName, BusinessType=req.BusinessType??"Grocery",
            Phone=req.Phone.Replace("+91","").Trim(), Email=req.Email, City=req.City,
            Pincode=req.Pincode, GstNumber=req.GstNumber, FssaiNumber=req.FssaiNumber,
            PanNumber=req.PanNumber, Status="PENDING", OwnerName=req.OwnerName, Description=req.Description
        };
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Vendor registered. Pending admin approval.", vendor });
    }

    [HttpPut("{id}"), Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVendorRequest req)
    {
        var v = await _db.Vendors.FindAsync(id);
        if (v == null) return NotFound();
        if (req.BusinessName != null) v.BusinessName = req.BusinessName;
        if (req.BusinessType != null) v.BusinessType = req.BusinessType;
        if (req.City != null) v.City = req.City;
        if (req.Pincode != null) v.Pincode = req.Pincode;
        if (req.GstNumber != null) v.GstNumber = req.GstNumber;
        if (req.FssaiNumber != null) v.FssaiNumber = req.FssaiNumber;
        if (req.PanNumber != null) v.PanNumber = req.PanNumber;
        if (req.OperatingFrom != null) v.OperatingFrom = req.OperatingFrom;
        if (req.OperatingTo != null) v.OperatingTo = req.OperatingTo;
        if (req.LogoUrl != null) v.LogoUrl = req.LogoUrl;
        if (req.OwnerName != null) v.OwnerName = req.OwnerName;
        if (req.OwnerPhone != null) v.OwnerPhone = req.OwnerPhone;
        if (req.Email != null) v.Email = req.Email;
        if (req.Description != null) v.Description = req.Description;
        await _db.SaveChangesAsync();
        return Ok(v);
    }

    [HttpPut("{id}/approve"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Approve(int id)
    {
        var v = await _db.Vendors.FindAsync(id);
        if (v == null) return NotFound();
        v.Status = "APPROVED"; v.ApprovedBy = Uid; v.ApprovedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Vendor approved", vendor = v });
    }

    [HttpPut("{id}/suspend"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Suspend(int id, [FromBody] UpdateOrderStatusRequest req)
    {
        var v = await _db.Vendors.FindAsync(id);
        if (v == null) return NotFound();
        v.Status = "SUSPENDED";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Vendor suspended" });
    }

    [HttpPost("my/users"), Authorize(Roles = "Vendor,Admin,SuperAdmin")]
    public async Task<IActionResult> MapUser([FromBody] MapUserRequest req)
    {
        Console.WriteLine($"VendorId = {req.VendorId}");
        Console.WriteLine($"Phone = {req.Phone}");
        Console.WriteLine($"Role = {req.Role}");
        var mapping = await _db.VendorUserMappings
            .FirstOrDefaultAsync(m => m.UserId == Uid);

        if (mapping == null &&
            !User.IsInRole("Admin") &&
            !User.IsInRole("SuperAdmin"))
        {
            return Forbid();
        }

        int vendorId;

        if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
        {
            vendorId = req.VendorId;
        }
        else
        {
            vendorId = mapping!.VendorId;
        }

        var user = await _db.User
            .FirstOrDefaultAsync(u =>
                u.Phone == req.Phone.Replace("+91", "").Trim());

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found. They must register on the app first."
            });
        }

        var vendor = await _db.Vendors.FindAsync(vendorId);

        if (vendor == null)
        {
            return NotFound(new
            {
                message = "Vendor not found"
            });
        }

        var exists = await _db.VendorUserMappings
            .AnyAsync(m =>
                m.VendorId == vendorId &&
                m.UserId == user.Id);

        if (exists)
        {
            return Conflict(new
            {
                message = "User already mapped to this vendor"
            });
        }

        user.Role = "Vendor";

        _db.VendorUserMappings.Add(new VendorUserMapping
        {
            VendorId = vendorId,
            UserId = user.Id,
            Role = req.Role
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "User mapped as vendor team member",
            vendorId,
            userId = user.Id,
            role = req.Role
        });
    }

    [HttpDelete("my/users/{userId}"), Authorize(Roles = "Vendor,Admin,SuperAdmin")]
    public async Task<IActionResult> RemoveUser(int userId)
    {
        var mapping = await _db.VendorUserMappings.FirstOrDefaultAsync(m => m.UserId == userId);
        if (mapping == null) return NotFound();
        _db.VendorUserMappings.Remove(mapping);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Team member removed" });
    }

    [HttpGet("my/orders"), Authorize(Roles = "Vendor")]
    public async Task<IActionResult> MyOrders([FromQuery] string? status, [FromQuery] int page=1, [FromQuery] int pageSize=20)
    {
        var mapping = await _db.VendorUserMappings.FirstOrDefaultAsync(m => m.UserId == Uid);
        if (mapping == null) return NotFound();
        var q = _db.Orders.Include(o=>o.Items).Include(o=>o.User).Where(o => o.VendorId == mapping.VendorId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(o => o.Status == status);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(o => o.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}/payouts"), Authorize]
    public async Task<IActionResult> Payouts(int id, [FromQuery] int page=1, [FromQuery] int pageSize=20)
    {
        var total = await _db.VendorPayouts.CountAsync(p => p.VendorId == id);
        var items = await _db.VendorPayouts.Where(p => p.VendorId == id).OrderByDescending(p => p.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpPut("my/bank"), Authorize(Roles = "Vendor")]
    public async Task<IActionResult> Bank([FromBody] VendorBankRequest req)
    {
        // Store bank details (in production encrypt these)
        return Ok(new { message = "Bank details saved. Payouts will be processed every Monday." });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        Console.WriteLine($"DELETE HIT: {id}");

        var vendor = await _db.Vendors.FindAsync(id);

        if (vendor == null)
            return NotFound();

        _db.Vendors.Remove(vendor);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Vendor deleted successfully"
        });
    }
}

// ── VENDOR PLANS ──────────────────────────────────────────────
[ApiController]
[Route("api/vendor-plans")]
[Tags("Vendors")]
public class VendorPlansController : ControllerBase
{
    private readonly AppDbContext _db;
    public VendorPlansController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _db.VendorPlans.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id) { var p = await _db.VendorPlans.FindAsync(id); return p == null ? NotFound() : Ok(p); }

    [HttpGet("my"), Authorize(Roles = "Vendor")]
    public async Task<IActionResult> MyPlan()
    {
        var mapping = await _db.VendorUserMappings.FirstOrDefaultAsync(m => m.UserId == Uid);
        if (mapping == null) return NotFound();
        var vendor = await _db.Vendors.Include(v => v.Documents).FirstOrDefaultAsync(v => v.Id == mapping.VendorId);
        if (vendor?.VendorPlanId == null) return Ok(null);
        var plan = await _db.VendorPlans.FindAsync(vendor.VendorPlanId);
        return Ok(plan);
    }

    [HttpPost, Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreatePlanRequest req)
    {
        var plan = new VendorPlan { Name=req.Name, Code=req.Code??req.Name.ToUpper(), Price=req.Price, CommissionPct=req.CommissionPct, MaxProducts=req.MaxProducts, MaxOrders=req.MaxOrders, MaxDeliveryBoys=req.MaxDeliveryBoys, Description=req.Description, Color=req.Color, Emoji=req.Emoji, CanCreateOffers=req.CanCreateOffers, CanCreatePromoCodes=req.CanCreatePromoCodes, FeaturedListing=req.FeaturedListing, AnalyticsDashboard=req.AnalyticsDashboard, IsFeatured=req.IsFeatured };
        _db.VendorPlans.Add(plan);
        await _db.SaveChangesAsync();
        return Ok(plan);
    }

    [HttpPut("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePlanRequest req)
    {
        var plan = await _db.VendorPlans.FindAsync(id);
        if (plan == null) return NotFound();
        plan.Name=req.Name; plan.Price=req.Price; plan.CommissionPct=req.CommissionPct;
        plan.MaxProducts=req.MaxProducts; plan.MaxOrders=req.MaxOrders; plan.MaxDeliveryBoys=req.MaxDeliveryBoys;
        plan.Description=req.Description; plan.Color=req.Color; plan.Emoji=req.Emoji;
        plan.CanCreateOffers=req.CanCreateOffers; plan.CanCreatePromoCodes=req.CanCreatePromoCodes;
        plan.FeaturedListing=req.FeaturedListing; plan.AnalyticsDashboard=req.AnalyticsDashboard;
        await _db.SaveChangesAsync();
        return Ok(plan);
    }

    [HttpPost("subscribe"), Authorize(Roles = "Vendor")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribePlanRequest req)
    {
        var plan = await _db.VendorPlans.FindAsync(req.PlanId);
        if (plan == null) return NotFound(new { message = "Plan not found" });
        var mapping = await _db.VendorUserMappings.FirstOrDefaultAsync(m => m.UserId == Uid);
        if (mapping == null) return NotFound(new { message = "Not linked to vendor" });
        var vendor = await _db.Vendors.FindAsync(mapping.VendorId);
        if (vendor == null) return NotFound();
        vendor.VendorPlanId = plan.Id;
        vendor.CommissionRate = plan.CommissionPct;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Subscribed to {plan.Name} plan", plan });
    }
}

// ════════════════════════════════════════════════════════════════
// DELIVERY BOYS
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/delivery-boys")]
[Tags("Delivery")]
public class DeliveryBoyController : ControllerBase
{
    private readonly AppDbContext _db;
    public DeliveryBoyController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet, Authorize]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] int? vendorId, [FromQuery] int page=1, [FromQuery] int pageSize=50)
    {
        var q = _db.DeliveryBoys.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(d => d.Status == status);
        if (vendorId.HasValue) q = q.Where(d => d.VendorId == vendorId);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(d => d.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}"), Authorize]
    public async Task<IActionResult> Get(int id) { var d = await _db.DeliveryBoys.FindAsync(id); return d == null ? NotFound() : Ok(d); }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDeliveryBoyRequest req)
    {
        var db = new DeliveryBoy { Name=req.Name, Phone=req.Phone.Replace("+91","").Trim(), VehicleType=req.VehicleType, VehicleNumber=req.VehicleNumber, AadhaarNumber=req.AadhaarNumber, Status="PENDING" };
        _db.DeliveryBoys.Add(db);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Delivery boy registered. Pending approval.", deliveryBoy = db });
    }

    [HttpPut("{id}/approve"), Authorize(Roles = "Admin,SuperAdmin,Vendor")]
    public async Task<IActionResult> Approve(int id)
    {
        var d = await _db.DeliveryBoys.FindAsync(id);
        if (d == null) return NotFound();
        d.Status = "ACTIVE";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Approved", deliveryBoy = d });
    }
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(
    int id,
    [FromBody] UpdateDeliveryBoyRequest req)
    {
        var d = await _db.DeliveryBoys.FindAsync(id);

        if (d == null)
            return NotFound(new
            {
                message = "Delivery boy not found"
            });

        d.Name = req.Name.Trim();
        d.Phone = req.Phone.Trim();
        d.AadhaarNumber = req.AadhaarNumber.Trim();
        d.VehicleType = req.VehicleType;
        d.VehicleNumber = req.VehicleNumber;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Delivery boy updated successfully",
            deliveryBoy = d
        });
    }

    [HttpPut("{id}/toggle-online"), Authorize]
    public async Task<IActionResult> ToggleOnline(int id)
    {
        var d = await _db.DeliveryBoys.FindAsync(id);
        if (d == null) return NotFound();
        d.IsOnline = !d.IsOnline;
        await _db.SaveChangesAsync();
        return Ok(new { id = d.Id, isOnline = d.IsOnline });
    }

    [HttpPost("{id}/payout"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Payout(int id, [FromBody] WalletRequest req)
    {
        var d = await _db.DeliveryBoys.FindAsync(id);
        if (d == null) return NotFound();
        if (d.WalletBalance < req.Amount) return BadRequest(new { message = "Insufficient wallet balance" });
        d.WalletBalance -= req.Amount;
        _db.DeliveryBoyPayouts.Add(new DeliveryBoyPayout { DeliveryBoyId = id, Amount = req.Amount, Status = "PROCESSED", ProcessedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Payout processed", amount = req.Amount });
    }
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _db.DeliveryBoys.FindAsync(id);

        if (d == null)
        {
            return NotFound(new
            {
                message = "Delivery boy not found"
            });
        }

        _db.DeliveryBoys.Remove(d);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Delivery boy deleted successfully"
        });
    }
}

// ════════════════════════════════════════════════════════════════
// SERVICE PERSONS
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/service-persons")]
[Tags("Service Persons")]
public class ServicePersonController : ControllerBase
{
    private readonly AppDbContext _db;
    public ServicePersonController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet, Authorize]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? skill, [FromQuery] int page=1, [FromQuery] int pageSize=50)
    {
        var q = _db.ServicePersons.AsQueryable();
        if (!string.IsNullOrEmpty(status)) q = q.Where(s => s.Status == status);
        if (!string.IsNullOrEmpty(skill))  q = q.Where(s => s.SkillTags != null && s.SkillTags.Contains(skill));
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(s => s.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}"), Authorize]
    public async Task<IActionResult> Get(int id)
    {
        var sp = await _db.ServicePersons.Include(x=>x.Documents).FirstOrDefaultAsync(x=>x.Id==id);
        return sp == null ? NotFound() : Ok(sp);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterServicePersonRequest req)
    {
        var sp = new ServicePerson { Name=req.Name, Phone=req.Phone.Replace("+91","").Trim(), Email=req.Email, Bio=req.Bio, SkillTags=req.Skills, Experience=req.Experience, Status="PENDING" };
        _db.ServicePersons.Add(sp);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Service person registered. Pending approval.", servicePerson = sp });
    }

    [HttpPut("{id}/approve"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Approve(int id)
    {
        var sp = await _db.ServicePersons.FindAsync(id);
        if (sp == null) return NotFound();
        sp.Status = "ACTIVE";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Approved", servicePerson = sp });
    }

    [HttpPut("{id}/verify"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Verify(int id)
    {
        var sp = await _db.ServicePersons.FindAsync(id);
        if (sp == null) return NotFound();
        sp.IsVerified = true;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Service person verified", isVerified = true });
    }

    [HttpPut("{id}/payment-config"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SetPayment(int id, [FromBody] SetPaymentConfigRequest req)
    {
        var sp = await _db.ServicePersons.FindAsync(id);
        if (sp == null) return NotFound();
        sp.PaymentType = req.PaymentType;
        sp.RateAmount  = req.RateAmount;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Payment config saved", paymentType = sp.PaymentType, rateAmount = sp.RateAmount });
    }

    [HttpPost("allocate"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Allocate([FromBody] AllocateRequest req)
    {
        var booking = await _db.ServiceBookings.FindAsync(req.BookingId);
        if (booking == null) return NotFound(new { message = "Booking not found" });
        var sp = await _db.ServicePersons.FindAsync(req.ServicePersonId);
        if (sp == null || sp.Status != "ACTIVE") return NotFound(new { message = "Service person not available" });
        booking.ServicePersonId = req.ServicePersonId;
        booking.Status = "ASSIGNED";
        _db.BookingStatusLogs.Add(new BookingStatusLog { BookingId=req.BookingId, StatusFrom="CONFIRMED", StatusTo="ASSIGNED", ChangedBy=Uid });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Service person allocated", booking });
    }

    [HttpPost("{id}/payout"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Payout(int id, [FromBody] WalletRequest req)
    {
        var sp = await _db.ServicePersons.FindAsync(id);
        if (sp == null) return NotFound();
        if (sp.WalletBalance < req.Amount) return BadRequest(new { message = "Insufficient wallet balance" });
        sp.WalletBalance -= req.Amount;
        _db.ServicePersonPayouts.Add(new ServicePersonPayout { ServicePersonId = id, GrossAmount = req.Amount, PlatformFee = 0, NetAmount = req.Amount, Status = "PROCESSED", ProcessedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Payout processed", amount = req.Amount });
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
    int id,
    [FromBody] RegisterServicePersonRequest req)
    {
        var sp = await _db.ServicePersons.FindAsync(id);

        if (sp == null)
            return NotFound();

        sp.Name = req.Name;
        sp.Phone = req.Phone;
        sp.Email = req.Email;
        sp.Bio = req.Bio;
        sp.SkillTags = req.Skills;
        sp.Experience = req.Experience;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Service person updated",
            servicePerson = sp
        });
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var sp = await _db.ServicePersons.FindAsync(id);

        if (sp == null)
            return NotFound();

        _db.ServicePersons.Remove(sp);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Service person deleted"
        });
    }
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Reject(int id)
    {
        var sp = await _db.ServicePersons.FindAsync(id);

        if (sp == null)
            return NotFound();

        sp.Status = "INACTIVE"; // or "REJECTED"

        await _db.SaveChangesAsync();

        return Ok(new { message = "Service person rejected" });
    }
}

// ════════════════════════════════════════════════════════════════
// OFFERS & PROMO CODES
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/offers")]
[Tags("Marketing")]
public class OffersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OffersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var now = DateTime.UtcNow;
        var offers = await _db.Offers.Where(o => o.IsActive && (o.ValidTill == null || o.ValidTill > now)).OrderBy(o => o.SortOrder).ToListAsync();
        return Ok(offers);
    }

    [HttpPost, Authorize(Roles = "Admin,SuperAdmin,Vendor")]
    public async Task<IActionResult> Create([FromBody] CreateOfferRequest req)
    {
        var offer = new Offer { TitleEn=req.TitleEn, TitleTe=req.TitleTe??"", Type=req.Type, Value=req.Value, MinOrder=req.MinOrder, MaxDiscount=req.MaxDiscount, ValidTill=req.ValidTill, Emoji=req.Emoji, Color=req.Color, ImageUrl=req.ImageUrl };
        _db.Offers.Add(offer);
        await _db.SaveChangesAsync();
        return Ok(offer);
    }

    [HttpPut("{id}"), Authorize(Roles = "Admin,SuperAdmin,Vendor")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateOfferRequest req)
    {
        var o = await _db.Offers.FindAsync(id);
        if (o == null) return NotFound();
        o.TitleEn=req.TitleEn; o.TitleTe=req.TitleTe??""; o.Type=req.Type; o.Value=req.Value;
        o.MinOrder=req.MinOrder; o.MaxDiscount=req.MaxDiscount; o.ValidTill=req.ValidTill;
        o.Emoji=req.Emoji??o.Emoji; o.Color=req.Color??o.Color;
        await _db.SaveChangesAsync();
        return Ok(o);
    }

    [HttpPut("{id}/toggle-active"), Authorize(Roles = "Admin,SuperAdmin,Vendor")]
    public async Task<IActionResult> Toggle(int id) { var o = await _db.Offers.FindAsync(id); if (o==null) return NotFound(); o.IsActive=!o.IsActive; await _db.SaveChangesAsync(); return Ok(new { id=o.Id, isActive=o.IsActive }); }

    [HttpDelete("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id) { var o = await _db.Offers.FindAsync(id); if (o==null) return NotFound(); o.IsActive=false; await _db.SaveChangesAsync(); return Ok(new { message="Deleted" }); }
}

[ApiController]
[Route("api/promo-codes")]
[Tags("Marketing")]
public class PromoCodesController : ControllerBase
{
    private readonly AppDbContext _db;
    public PromoCodesController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? cartType)
    {
        var now = DateTime.UtcNow;
        var q = _db.Coupons.Where(c => c.IsPublic && c.IsActive && c.ValidTill > now && c.ValidFrom <= now);
        if (!string.IsNullOrEmpty(cartType) && cartType.ToUpper() != "ALL")
            q = q.Where(c => c.CartType == "ALL" || c.CartType.ToUpper() == cartType.ToUpper());
        var list = await q.OrderByDescending(c => c.DiscountValue).ToListAsync();
        return Ok(list);
    }

    [HttpPost("validate"), Authorize]
    public async Task<IActionResult> Validate([FromBody] ValidateCouponRequest req)
        => Ok(await RunChecks(req, Uid, false, null, null));

    [HttpPost("apply"), Authorize]
    public async Task<IActionResult> Apply([FromBody] ApplyCouponRequest req)
    {
        var valReq = new ValidateCouponRequest(req.Code, req.OrderAmount, req.CartType, req.DeliveryFee);
        return Ok(await RunChecks(valReq, Uid, true, req.OrderId, req.BookingId));
    }

    [HttpPut("{id}/toggle-active"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Toggle(int id) { var c = await _db.Coupons.FindAsync(id); if (c==null) return NotFound(); c.IsActive=!c.IsActive; await _db.SaveChangesAsync(); return Ok(new { id=c.Id, isActive=c.IsActive }); }

    [HttpPost, Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest req)
    {
        if (await _db.Coupons.AnyAsync(c => c.Code == req.Code.ToUpper()))
            return Conflict(new { message = $"Code '{req.Code}' already exists" });
        var coupon = new Coupon { Code=req.Code.ToUpper(), TitleEn=req.TitleEn, TitleTe=req.TitleTe??"", DescEn=req.DescEn??"", CartType=req.CartType.ToUpper(), MinOrder=req.MinOrder, DiscountType=req.DiscountType, DiscountValue=req.DiscountValue, MaxDiscount=req.MaxDiscount, ValidFrom=req.ValidFrom??DateTime.UtcNow, ValidTill=req.ValidTill??DateTime.UtcNow.AddMonths(3), UsageLimit=req.UsageLimit, PerUserLimit=req.PerUserLimit, IsFirstOrderOnly=req.IsFirstOrderOnly, IsPublic=req.IsPublic, Color=req.Color };
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();
        return Ok(coupon);
    }

    [HttpPut("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCouponRequest req)
    {
        var c = await _db.Coupons.FindAsync(id);
        if (c == null) return NotFound();
        c.TitleEn=req.TitleEn; c.TitleTe=req.TitleTe??""; c.CartType=req.CartType.ToUpper();
        c.DiscountType=req.DiscountType; c.DiscountValue=req.DiscountValue; c.MaxDiscount=req.MaxDiscount;
        c.MinOrder=req.MinOrder; c.ValidTill=req.ValidTill??c.ValidTill; c.UsageLimit=req.UsageLimit;
        c.PerUserLimit=req.PerUserLimit; c.IsFirstOrderOnly=req.IsFirstOrderOnly; c.IsPublic=req.IsPublic;
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    [HttpDelete("{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Delete(int id) { var c = await _db.Coupons.FindAsync(id); if (c==null) return NotFound(); c.IsActive=false; await _db.SaveChangesAsync(); return Ok(new { message="Deactivated" }); }

    private async Task<ValidateCouponResponse> RunChecks(ValidateCouponRequest req, int userId, bool record, int? orderId, int? bookingId)
    {
        var now  = DateTime.UtcNow;
        var code = req.Code.Trim().ToUpper();
        var coupon = await _db.Coupons.Include(c => c.Usages).FirstOrDefaultAsync(c => c.Code == code);

        if (coupon == null)                    return Fail("NOT_FOUND",       "Promo code not found.");
        if (!coupon.IsActive)                  return Fail("INACTIVE",        "This promo code is no longer active.");
        if (coupon.ValidFrom > now)            return Fail("NOT_YET_ACTIVE",  $"This code starts on {coupon.ValidFrom:dd MMM yyyy}.");
        if (coupon.ValidTill < now)            return Fail("EXPIRED",         $"This code expired on {coupon.ValidTill:dd MMM yyyy}.");
        if (coupon.UsageLimit > 0 && coupon.UsedCount >= coupon.UsageLimit) return Fail("LIMIT_REACHED", "This code has been fully claimed.");

        if (coupon.CartType != "ALL")
        {
            var ct = req.CartType.Trim().ToUpper();
            if (!coupon.CartType.Equals(ct, StringComparison.OrdinalIgnoreCase))
                return Fail("WRONG_CART_TYPE", $"This code is valid only for {coupon.CartType.ToLower()} orders.");
        }

        if (req.OrderAmount < coupon.MinOrder)
            return Fail("MIN_ORDER", $"Add Rs.{coupon.MinOrder - req.OrderAmount:F0} more to use this code. Min order: Rs.{coupon.MinOrder:F0}.");

        if (coupon.IsFirstOrderOnly)
        {
            var prev = await _db.Orders.CountAsync(o => o.UserId == userId && o.Status != "CANCELLED");
            var prevB= await _db.ServiceBookings.CountAsync(b => b.UserId == userId && b.Status == "COMPLETED");
            if (prev + prevB > 0) return Fail("FIRST_ORDER_ONLY", "This code is for first-time customers only.");
        }

        var used = coupon.Usages.Count(u => u.UserId == userId);
        if (used >= coupon.PerUserLimit)
            return Fail("ALREADY_USED", $"You have already used this code ({coupon.PerUserLimit}x per customer).");

        decimal discount = coupon.DiscountType switch
        {
            "percent"          => Math.Min(Math.Round(req.OrderAmount * coupon.DiscountValue / 100, 2), coupon.MaxDiscount > 0 ? coupon.MaxDiscount : decimal.MaxValue),
            "flat"             => coupon.DiscountValue,
            "delivery"         => req.DeliveryFee,
            "free_service_fee" => coupon.DiscountValue,
            _                  => 0m
        };
        discount = Math.Min(discount, req.OrderAmount);
        discount = Math.Round(discount, 2);

        var finalDelivery = coupon.DiscountType == "delivery" ? 0m : req.DeliveryFee;
        var finalAmount   = Math.Round(req.OrderAmount - discount, 2);

        if (record)
        {
            _db.CouponUsages.Add(new CouponUsage { CouponId=coupon.Id, UserId=userId, OrderId=orderId, BookingId=bookingId, CartType=req.CartType.ToUpper(), DiscountGiven=discount, UsedAt=now });
            coupon.UsedCount++;
            await _db.SaveChangesAsync();
        }

        var badge = coupon.DiscountType switch
        {
            "percent"  => $"{(int)coupon.DiscountValue}% OFF",
            "delivery" => "FREE DELIVERY",
            _          => $"Rs.{(int)coupon.DiscountValue} OFF"
        };

        return new ValidateCouponResponse(true, "OK", $"Code applied! You save Rs.{discount:F0}.", discount, finalAmount, finalDelivery, coupon.Code, badge, coupon.DiscountType);
    }

    private static ValidateCouponResponse Fail(string code, string msg)
        => new(false, code, msg, 0, 0, 0, null, null, null);
}

// ════════════════════════════════════════════════════════════════
// ADMIN
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/admin")]
[Tags("Admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtHelper _jwt;
    public AdminController(AppDbContext db, JwtHelper jwt) { _db = db; _jwt = jwt; }
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    // GET /api/admin/users
    [HttpGet("users"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AdminList() => Ok(await _db.AdminUsers.Include(a => a.User).ToListAsync());

    // POST /api/admin/users
    [HttpPost("users"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminRequest req)
    {
        var phone = req.Phone.Replace("+91","").Trim();
        var user = await _db.User.FirstOrDefaultAsync(u => u.Phone == phone)
                   ?? new User { Phone = phone, Name = req.Name, Email = req.Email, Role = req.Role };

        if (user.Id == 0) { _db.User.Add(user); await _db.SaveChangesAsync(); }
        else { user.Role = req.Role; }

        user.PasswordHash = JwtHelper.HashPassword("Admin@123"); // temp password

        if (!await _db.AdminUsers.AnyAsync(a => a.UserId == user.Id))
        {
            _db.AdminUsers.Add(new AdminUser { UserId = user.Id, Role = req.Role, IsSuperAdmin = req.Role == "SuperAdmin" });
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Admin created. Temp password: Admin@123 (must change)", user, hint = "Call POST /api/auth/set-password to set a proper password." });
    }

    // GET /api/admin/platform-settings
    [HttpGet("platform-settings"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Settings() => Ok(await _db.PlatformSettings.OrderBy(s => s.Group).ThenBy(s => s.Key).ToListAsync());

    // PUT /api/admin/platform-settings/{key}
    [HttpPut("platform-settings/{key}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest req)
    {
        var setting = await _db.PlatformSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null) return NotFound(new { message = $"Setting '{key}' not found" });
        setting.Value = req.Value;
        await _db.SaveChangesAsync();
        return Ok(setting);
    }

    // GET /api/admin/revenue-report
    [HttpGet("revenue-report"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RevenueReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var f = from ?? DateTime.UtcNow.AddMonths(-1);
        var t = to   ?? DateTime.UtcNow;
        var orders   = await _db.Orders.Where(o => o.CreatedAt >= f && o.CreatedAt <= t && o.Status == "DELIVERED").SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        var bookings = await _db.ServiceBookings.Where(b => b.CreatedAt >= f && b.CreatedAt <= t && b.Status == "COMPLETED").SumAsync(b => (decimal?)b.Amount) ?? 0;
        return Ok(new { from=f, to=t, groceryRevenue=orders, serviceRevenue=bookings, totalRevenue=orders+bookings });
    }

    // GET /api/users
    [HttpGet("/api/users"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UserList([FromQuery] int page=1, [FromQuery] int pageSize=50)
    {
        var total = await _db.User.CountAsync();
        var items = await _db.User.OrderByDescending(u => u.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    // PUT /api/users/{id}/block
    [HttpPut("/api/users/{id}/block"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> BlockUser(int id) { var u = await _db.User.FindAsync(id); if (u==null) return NotFound(); u.IsActive=false; await _db.SaveChangesAsync(); return Ok(new { message="User blocked" }); }

    // PUT /api/users/{id}/unblock
    [HttpPut("/api/users/{id}/unblock"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UnblockUser(int id) { var u = await _db.User.FindAsync(id); if (u==null) return NotFound(); u.IsActive=true; await _db.SaveChangesAsync(); return Ok(new { message="User unblocked" }); }
    [HttpDelete("/api/users/{id}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.User.FindAsync(id);

        if (user == null)
            return NotFound();

        _db.User.Remove(user);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "User deleted successfully"
        });
    }
    // PUT /api/admin/users/{id}
    [HttpPut("users/{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateAdmin(int id, [FromBody] CreateAdminRequest req)
    {
        var admin = await _db.AdminUsers
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (admin == null)
            return NotFound();

        admin.Role = req.Role;
        admin.IsSuperAdmin = req.Role == "SuperAdmin";

        admin.User.Name = req.Name;
        admin.User.Email = req.Email;
        admin.User.Role = req.Role;

        await _db.SaveChangesAsync();

        return Ok(admin);
    }
    // DELETE /api/admin/users/{id}
    [HttpDelete("users/{id}"), Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteAdmin(int id)
    {
        var admin = await _db.AdminUsers
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (admin == null)
            return NotFound();

        _db.AdminUsers.Remove(admin);

        await _db.SaveChangesAsync();

        return Ok(new { message = "Admin deleted" });
    }
}

// ════════════════════════════════════════════════════════════════
// TRANSLATIONS
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/translations")]
[Tags("Translations")]
public class TranslationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public TranslationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? group, [FromQuery] string lang = "en")
    {
        var q = _db.Translations.AsQueryable();
        if (!string.IsNullOrEmpty(group)) q = q.Where(t => t.Group == group);
        var list = await q.OrderBy(t => t.Group).ThenBy(t => t.Key).ToListAsync();
        return Ok(list);
    }

    [HttpPost, Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateTranslationRequest req)
    {
        if (await _db.Translations.AnyAsync(t => t.Key == req.Key)) return Conflict(new { message = "Key already exists" });
        var trans = new Translation { Key=req.Key, Group=req.Group, En=req.En, Te=req.Te };
        _db.Translations.Add(trans);
        await _db.SaveChangesAsync();
        return Ok(trans);
    }

    [HttpPut("{key}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Update(string key, [FromBody] UpdateTranslationRequest req)
    {
        var t = await _db.Translations.FirstOrDefaultAsync(x => x.Key == key);
        if (t == null) return NotFound();
        if (req.En != null) t.En = req.En;
        if (req.Te != null) t.Te = req.Te;
        await _db.SaveChangesAsync();
        return Ok(t);
    }
}

// ════════════════════════════════════════════════════════════════
// DASHBOARD
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/dashboard")]
[Tags("Dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("admin"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AdminDashboard()
    {
        var today = DateTime.UtcNow.Date;
        return Ok(new
        {
            totalUsers         = await _db.User.CountAsync(u => u.Role == "Customer"),
            totalVendors       = await _db.Vendors.CountAsync(),
            totalDeliveryBoys  = await _db.DeliveryBoys.CountAsync(),
            totalServicePersons= await _db.ServicePersons.CountAsync(),
            totalOrders        = await _db.Orders.CountAsync(),
            totalBookings      = await _db.ServiceBookings.CountAsync(),
            ordersToday        = await _db.Orders.CountAsync(o => o.CreatedAt.Date == today),
            activeBookings     = await _db.ServiceBookings.CountAsync(b => b.Status == "ASSIGNED" || b.Status == "IN_PROGRESS"),
            pendingVendors     = await _db.Vendors.CountAsync(v => v.Status == "PENDING"),
            pendingDeliveryBoys= await _db.DeliveryBoys.CountAsync(d => d.Status == "PENDING"),
            pendingServicePersons = await _db.ServicePersons.CountAsync(s => s.Status == "PENDING"),
            totalRevenue       = await _db.Orders.Where(o => o.Status == "DELIVERED").SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            unallocatedBookings= await _db.ServiceBookings.CountAsync(b => b.Status == "CONFIRMED" && b.ServicePersonId == null),
        });
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> VendorDashboard()
    {
        var uid = int.Parse(User.FindFirst("uid")?.Value ?? "0");
        var mapping = await _db.VendorUserMappings.FirstOrDefaultAsync(m => m.UserId == uid);
        if (mapping == null) return Ok(new { });
        var v = await _db.Vendors.FindAsync(mapping.VendorId);
        return Ok(new {
            businessName   = v?.BusinessName,
            status         = v?.Status,
            totalRevenue   = v?.TotalRevenue ?? 0,
            totalOrders    = v?.TotalOrders ?? 0,
            rating         = v?.Rating ?? 0,
            planName       = v?.VendorPlanId != null ? (await _db.VendorPlans.FindAsync(v.VendorPlanId))?.Name : "FREE",
            commissionRate = v?.CommissionRate ?? 18,
        });
    }
}

// ════════════════════════════════════════════════════════════════
// REVIEWS
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/reviews")]
[Tags("Reviews")]
public class ReviewsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ReviewsController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpPost, Authorize]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest req)
    {
        var review = new Review { UserId=Uid, Rating=req.Rating, Comment=req.Comment, OrderId=req.OrderId, BookingId=req.BookingId, VendorId=req.VendorId, DeliveryBoyId=req.DeliveryBoyId, ServicePersonId=req.ServicePersonId };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return Ok(review);
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> List([FromQuery] int? vendorId, [FromQuery] int? spId, [FromQuery] int? deliveryBoyId)
    {
        var q = _db.Reviews.Include(r => r.User).AsQueryable();
        if (vendorId.HasValue)      q = q.Where(r => r.VendorId == vendorId);
        if (spId.HasValue)          q = q.Where(r => r.ServicePersonId == spId);
        if (deliveryBoyId.HasValue) q = q.Where(r => r.DeliveryBoyId == deliveryBoyId);
        return Ok(await q.OrderByDescending(r => r.CreatedAt).Take(50).ToListAsync());
    }
}

// ════════════════════════════════════════════════════════════════
// LOGS (audit)
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/logs")]
[Tags("Logs")]
public class LogsController : ControllerBase
{
    private readonly AppDbContext _db;
    public LogsController(AppDbContext db) => _db = db;

    [HttpGet("booking-status"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> BookingLogs([FromQuery] int? bookingId, [FromQuery] int page=1, [FromQuery] int pageSize=50)
    {
        var q = _db.BookingStatusLogs.AsQueryable();
        if (bookingId.HasValue) q = q.Where(l => l.BookingId == bookingId);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(l => l.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }
}
