using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BabaiBazaar.API.Data;
using BabaiBazaar.API.DTOs;
using BabaiBazaar.API.Models;

namespace BabaiBazaar.API.Controllers;

// ════════════════════════════════════════════════════════════════
// ONBOARDING — Vendor & SP document upload, agreements, checklist
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/onboarding")]
[Tags("Onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    public OnboardingController(AppDbContext db, IConfiguration cfg) { _db = db; _cfg = cfg; }
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    // ── VENDOR DOCUMENTS ─────────────────────────────────────
    [HttpGet("vendor/{vendorId}/documents"), Authorize]
    public async Task<IActionResult> VendorDocs(int vendorId)
        => Ok(await _db.VendorDocuments.Where(d => d.VendorId == vendorId).OrderByDescending(d => d.CreatedAt).ToListAsync());

    [HttpPost("vendor/{vendorId}/documents"), Authorize]
    public async Task<IActionResult> UploadVendorDoc(int vendorId, [FromBody] UploadDocRequest req)
    {
        var vendor = await _db.Vendors.FindAsync(vendorId);
        if (vendor == null) return NotFound(new { message = "Vendor not found" });

        var existing = await _db.VendorDocuments.Where(d => d.VendorId == vendorId && d.DocType == req.DocType).ToListAsync();
        _db.VendorDocuments.RemoveRange(existing);

        _db.VendorDocuments.Add(new VendorDocument { VendorId=vendorId, DocType=req.DocType, FileUrl=req.FileUrl, Status="PENDING", Remarks=req.Remarks??"" });
        await _db.SaveChangesAsync();
        return Ok(new { message = $"{req.DocType} uploaded. Pending admin review." });
    }

    [HttpPut("vendor/documents/{docId}/status"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateVendorDocStatus(int docId, [FromBody] UpdateDocStatusRequest req)
    {
        var doc = await _db.VendorDocuments.FindAsync(docId);
        if (doc == null) return NotFound();
        doc.Status = req.Status;
        if (req.Remarks != null) doc.Remarks = req.Remarks;
        await _db.SaveChangesAsync();

        // Auto-approve vendor when all required docs approved
        if (req.Status == "APPROVED")
        {
            var vendorDocs = await _db.VendorDocuments.Where(d => d.VendorId == doc.VendorId).ToListAsync();
            var required   = new[] { "GSTIN", "PAN", "AADHAAR", "BANK_PROOF", "TNC_SIGNED" };
            bool allOk     = required.All(r => vendorDocs.Any(d => d.DocType == r && d.Status == "APPROVED"));
            if (allOk)
            {
                var vendor = await _db.Vendors.FindAsync(doc.VendorId);
                if (vendor != null && vendor.Status == "PENDING")
                { vendor.Status = "APPROVED"; vendor.ApprovedAt = DateTime.UtcNow; vendor.ApprovedBy = Uid; }
                await _db.SaveChangesAsync();
            }
        }
        return Ok(new { message = $"Document {req.Status}", doc });
    }

    [HttpGet("vendor/{vendorId}/checklist"), Authorize]
    public async Task<IActionResult> VendorChecklist(int vendorId)
    {
        var docs   = await _db.VendorDocuments.Where(d => d.VendorId == vendorId).ToListAsync();
        var vendor = await _db.Vendors.FindAsync(vendorId);

        var required = new[]
        {
            new { DocType="GSTIN",      Label="GST Certificate",     Required=true,  Description="GST registration certificate of the business" },
            new { DocType="FSSAI",      Label="FSSAI License",       Required=true,  Description="Food Safety license (for grocery/food vendors)" },
            new { DocType="PAN",        Label="PAN Card",            Required=true,  Description="PAN card of proprietor/company" },
            new { DocType="AADHAAR",    Label="Aadhaar Card",        Required=true,  Description="Aadhaar of business owner" },
            new { DocType="SHOP_ACT",   Label="Shop Act License",    Required=false, Description="Shop and Establishment Act license" },
            new { DocType="BANK_PROOF", Label="Bank Account Proof",  Required=true,  Description="Cancelled cheque or bank passbook front page" },
            new { DocType="LOGO",       Label="Business Logo",       Required=false, Description="Store logo for the app (PNG/JPG, 512x512)" },
            new { DocType="TNC_SIGNED", Label="Terms and Conditions",Required=true,  Description="Signed vendor agreement with Babai Bazaar" },
        };

        var checklist = required.Select(r =>
        {
            var doc = docs.FirstOrDefault(d => d.DocType == r.DocType);
            return new { r.DocType, r.Label, r.Required, r.Description, Status=doc?.Status??"NOT_UPLOADED", FileUrl=doc?.FileUrl, Remarks=doc?.Remarks, DocId=doc?.Id };
        });

        var approved = required.Count(r => { var d = docs.FirstOrDefault(x => x.DocType == r.DocType); return d?.Status == "APPROVED"; });
        return Ok(new { vendorId, vendorName=vendor?.BusinessName, vendorStatus=vendor?.Status, totalRequired=required.Count(r=>r.Required), approved, isComplete=approved==required.Count(r=>r.Required), checklist });
    }

    // ── SERVICE PERSON DOCUMENTS ──────────────────────────────
    [HttpGet("sp/{spId}/documents"), Authorize]
    public async Task<IActionResult> SpDocs(int spId)
        => Ok(await _db.ServicePersonDocuments.Where(d => d.ServicePersonId == spId).OrderByDescending(d => d.CreatedAt).ToListAsync());

    [HttpPost("sp/{spId}/documents"), Authorize]
    public async Task<IActionResult> UploadSpDoc(int spId, [FromBody] UploadDocRequest req)
    {
        var sp = await _db.ServicePersons.FindAsync(spId);
        if (sp == null) return NotFound();
        var existing = await _db.ServicePersonDocuments.Where(d => d.ServicePersonId == spId && d.DocType == req.DocType).ToListAsync();
        _db.ServicePersonDocuments.RemoveRange(existing);
        _db.ServicePersonDocuments.Add(new ServicePersonDocument { ServicePersonId=spId, DocType=req.DocType, FileUrl=req.FileUrl, Status="PENDING", Remarks=req.Remarks??"" });
        await _db.SaveChangesAsync();
        return Ok(new { message = $"{req.DocType} uploaded." });
    }

    [HttpPut("sp/documents/{docId}/status"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateSpDocStatus(int docId, [FromBody] UpdateDocStatusRequest req)
    {
        var doc = await _db.ServicePersonDocuments.FindAsync(docId);
        if (doc == null) return NotFound();
        doc.Status = req.Status;
        if (req.Remarks != null) doc.Remarks = req.Remarks;
        await _db.SaveChangesAsync();

        if (req.Status == "APPROVED")
        {
            var spDocs   = await _db.ServicePersonDocuments.Where(d => d.ServicePersonId == doc.ServicePersonId).ToListAsync();
            var required = new[] { "AADHAAR", "PAN", "POLICE_CLEARANCE", "PHOTO", "BANK_PROOF", "TNC_SIGNED" };
            bool allOk   = required.All(r => spDocs.Any(d => d.DocType == r && d.Status == "APPROVED"));
            if (allOk)
            {
                var sp = await _db.ServicePersons.FindAsync(doc.ServicePersonId);
                if (sp != null) { sp.Status = "ACTIVE"; sp.IsVerified = true; }
                await _db.SaveChangesAsync();
            }
        }
        return Ok(new { message = $"Document {req.Status}", doc });
    }

    [HttpGet("sp/{spId}/checklist"), Authorize]
    public async Task<IActionResult> SpChecklist(int spId)
    {
        var docs = await _db.ServicePersonDocuments.Where(d => d.ServicePersonId == spId).ToListAsync();
        var sp   = await _db.ServicePersons.FindAsync(spId);

        var required = new[]
        {
            new { DocType="AADHAAR",          Label="Aadhaar Card (Front and Back)", Required=true,  Description="Clear photo of both sides of Aadhaar" },
            new { DocType="PAN",              Label="PAN Card",                      Required=true,  Description="PAN card of the service professional" },
            new { DocType="POLICE_CLEARANCE", Label="Police Clearance Certificate",  Required=true,  Description="Police verification certificate — apply at Mee Seva. Takes 7-15 days. Cost Rs.150-300." },
            new { DocType="SKILL_CERT",       Label="Skill Certificate",             Required=false, Description="Any relevant skill or ITI training certificate" },
            new { DocType="PHOTO",            Label="Passport Photo",                Required=true,  Description="Recent passport-size photograph on white background" },
            new { DocType="BANK_PROOF",       Label="Bank Account Proof",            Required=true,  Description="Passbook front page or cancelled cheque" },
            new { DocType="TNC_SIGNED",       Label="Service Professional Agreement",Required=true,  Description="Signed Babai Bazaar service professional agreement" },
            new { DocType="ADDRESS_PROOF",    Label="Address Proof",                 Required=false, Description="Utility bill or rental agreement" },
        };

        var checklist = required.Select(r => { var doc = docs.FirstOrDefault(d => d.DocType == r.DocType); return new { r.DocType, r.Label, r.Required, r.Description, Status=doc?.Status??"NOT_UPLOADED", FileUrl=doc?.FileUrl, Remarks=doc?.Remarks, DocId=doc?.Id }; });
        var approved  = required.Count(r => { var d = docs.FirstOrDefault(x => x.DocType == r.DocType); return d?.Status == "APPROVED"; });
        return Ok(new { spId, name=sp?.Name, status=sp?.Status, isVerified=sp?.IsVerified, totalRequired=required.Count(r=>r.Required), approved, isComplete=approved==required.Count(r=>r.Required), checklist });
    }

    // ── TEMPLATES (T&C PDF downloads) ────────────────────────
    [HttpGet("agreements/templates")]
    public IActionResult Templates()
    {
        var baseUrl = _cfg["AppSettings:BaseUrl"] ?? "http://204.168.159.160:8085";
        return Ok(new[]
        {
            new { type="VENDOR_TNC",     label="Vendor Terms and Conditions",     url=$"{baseUrl}/templates/vendor-tnc.pdf",    description="Standard vendor onboarding agreement. Print, sign, upload scan." },
            new { type="SP_TNC",         label="Service Person Agreement",        url=$"{baseUrl}/templates/sp-agreement.pdf",  description="Service professional onboarding agreement." },
            new { type="PRIVACY_POLICY", label="Privacy Policy",                  url=$"{baseUrl}/templates/privacy-policy.pdf",description="Babai Bazaar data privacy policy." },
            new { type="NDA",            label="Non-Disclosure Agreement",        url=$"{baseUrl}/templates/nda.pdf",           description="Confidentiality agreement for premium vendors." },
        });
    }

    // ── AGREEMENTS ────────────────────────────────────────────
    [HttpPost("vendor/{vendorId}/agreement"), Authorize]
    public async Task<IActionResult> RecordVendorAgreement(int vendorId, [FromBody] RecordAgreementRequest req)
    {
        _db.OnboardingAgreements.Add(new OnboardingAgreement { PartyType="VENDOR", VendorId=vendorId, AgreementType=req.AgreementType??"VENDOR_TNC", DocumentUrl=req.DocumentUrl??"", SignatureUrl=req.SignatureUrl??"", Notes=req.Notes, SignedByName=req.SignedByName, SignedAt=DateTime.UtcNow, IpAddress=HttpContext.Connection.RemoteIpAddress?.ToString()??"", CollectedByUserId=Uid, CollectionMethod=req.CollectionMethod??"PORTAL", Status="SIGNED" });

        var docCheck = await _db.VendorDocuments.FirstOrDefaultAsync(d => d.VendorId == vendorId && d.DocType == "TNC_SIGNED");
        if (docCheck == null)
            _db.VendorDocuments.Add(new VendorDocument { VendorId=vendorId, DocType="TNC_SIGNED", FileUrl=req.DocumentUrl??"", Status="PENDING", Remarks=$"Signed by {req.SignedByName} via {req.CollectionMethod}" });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Agreement recorded" });
    }

    [HttpGet("vendor/{vendorId}/agreements"), Authorize]
    public async Task<IActionResult> VendorAgreements(int vendorId)
        => Ok(await _db.OnboardingAgreements.Where(a => a.VendorId == vendorId).OrderByDescending(a => a.SignedAt).ToListAsync());

    [HttpPost("sp/{spId}/agreement"), Authorize]
    public async Task<IActionResult> RecordSpAgreement(int spId, [FromBody] RecordAgreementRequest req)
    {
        _db.OnboardingAgreements.Add(new OnboardingAgreement { PartyType="SERVICE_PERSON", ServicePersonId=spId, AgreementType=req.AgreementType??"SP_TNC", DocumentUrl=req.DocumentUrl??"", SignatureUrl=req.SignatureUrl??"", Notes=req.Notes, SignedByName=req.SignedByName, SignedAt=DateTime.UtcNow, IpAddress=HttpContext.Connection.RemoteIpAddress?.ToString()??"", CollectedByUserId=Uid, CollectionMethod=req.CollectionMethod??"PORTAL", Status="SIGNED" });

        var docCheck = await _db.ServicePersonDocuments.FirstOrDefaultAsync(d => d.ServicePersonId == spId && d.DocType == "TNC_SIGNED");
        if (docCheck == null)
            _db.ServicePersonDocuments.Add(new ServicePersonDocument { ServicePersonId=spId, DocType="TNC_SIGNED", FileUrl=req.DocumentUrl??"", Status="PENDING", Remarks=$"Signed by {req.SignedByName} via {req.CollectionMethod}" });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Agreement recorded" });
    }

    [HttpGet("sp/{spId}/agreements"), Authorize]
    public async Task<IActionResult> SpAgreements(int spId)
        => Ok(await _db.OnboardingAgreements.Where(a => a.ServicePersonId == spId).OrderByDescending(a => a.SignedAt).ToListAsync());
}

// ════════════════════════════════════════════════════════════════
// CRM — Tickets, Staff, Daily Targets
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/crm")]
[Tags("CRM")]
public class CrmController : ControllerBase
{
    private readonly AppDbContext _db;
    public CrmController(AppDbContext db) => _db = db;
    private int Uid  => int.Parse(User.FindFirst("uid")?.Value ?? "0");
    private string UName => User.FindFirst("name")?.Value ?? "System";

    // ── TICKETS ──────────────────────────────────────────────
    [HttpGet("tickets"), Authorize]
    public async Task<IActionResult> ListTickets([FromQuery] string? status, [FromQuery] string? type, [FromQuery] int? assignedTo, [FromQuery] int page=1, [FromQuery] int pageSize=20)
    {
        var q = _db.CrmTickets.Include(t => t.Notes).AsQueryable();
        if (!string.IsNullOrEmpty(status))    q = q.Where(t => t.Status    == status);
        if (!string.IsNullOrEmpty(type))      q = q.Where(t => t.TicketType == type);
        if (assignedTo.HasValue)              q = q.Where(t => t.AssignedToUserId == assignedTo);

        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        if (role is "Marketing" or "Telecaller" or "FieldAgent")
            q = q.Where(t => t.AssignedToUserId == Uid || t.CreatedByUserId == Uid);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("tickets/{id}"), Authorize]
    public async Task<IActionResult> GetTicket(int id)
    {
        var t = await _db.CrmTickets.Include(x => x.Notes).FirstOrDefaultAsync(x => x.Id == id);
        return t == null ? NotFound() : Ok(t);
    }

    [HttpGet("tickets/my"), Authorize]
    public async Task<IActionResult> MyTickets([FromQuery] string? status)
    {
        var q = _db.CrmTickets.Where(t => t.AssignedToUserId == Uid);
        if (!string.IsNullOrEmpty(status)) q = q.Where(t => t.Status == status);
        return Ok(await q.OrderByDescending(t => t.CreatedAt).Take(50).ToListAsync());
    }

    [HttpPost("tickets"), Authorize]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req)
    {
        var ticket = new CrmTicket { TargetType=req.TargetType, VendorId=req.VendorId, ServicePersonId=req.ServicePersonId, LeadName=req.LeadName, LeadPhone=req.LeadPhone, TicketType=req.TicketType, Subject=req.Subject, Description=req.Description, Priority=req.Priority, Status="OPEN", AssignedToUserId=req.AssignedToUserId, AssignedToName=req.AssignedToName, CreatedByUserId=Uid, DueDate=req.DueDate, CallScheduled=req.CallScheduled };
        _db.CrmTickets.Add(ticket);
        await _db.SaveChangesAsync();
        if (!string.IsNullOrEmpty(req.Description))
        {
            _db.CrmTicketNotes.Add(new CrmTicketNote { TicketId=ticket.Id, CreatedByUserId=Uid, CreatedByName=UName, Note=req.Description, NoteType="NOTE" });
            await _db.SaveChangesAsync();
        }
        return Ok(new { message = "Ticket created", ticketId=ticket.Id, ticket });
    }

    [HttpPut("tickets/{id}"), Authorize]
    public async Task<IActionResult> UpdateTicket(int id, [FromBody] UpdateTicketRequest req)
    {
        var ticket = await _db.CrmTickets.FindAsync(id);
        if (ticket == null) return NotFound();
        var oldStatus = ticket.Status;
        if (req.Status != null) ticket.Status = req.Status;
        if (req.Priority != null) ticket.Priority = req.Priority;
        if (req.AssignedToUserId != null) ticket.AssignedToUserId = req.AssignedToUserId;
        if (req.AssignedToName != null)   ticket.AssignedToName = req.AssignedToName;
        if (req.Resolution != null)       ticket.Resolution = req.Resolution;
        if (req.DueDate != null)          ticket.DueDate = req.DueDate;
        if (req.CallScheduled != null)    ticket.CallScheduled = req.CallScheduled;
        if (req.CallStatus != null)       ticket.CallStatus = req.CallStatus;
        if (req.Status is "RESOLVED" or "CLOSED") ticket.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        if (oldStatus != ticket.Status)
        {
            _db.CrmTicketNotes.Add(new CrmTicketNote { TicketId=id, CreatedByUserId=Uid, CreatedByName=UName, Note=$"Status changed from {oldStatus} to {ticket.Status}" + (req.Resolution != null ? $". Resolution: {req.Resolution}" : ""), NoteType="STATUS_CHANGE" });
            await _db.SaveChangesAsync();
        }
        return Ok(ticket);
    }

    [HttpPost("tickets/{id}/notes"), Authorize]
    public async Task<IActionResult> AddNote(int id, [FromBody] AddNoteRequest req)
    {
        var ticket = await _db.CrmTickets.FindAsync(id);
        if (ticket == null) return NotFound();
        var note = new CrmTicketNote { TicketId=id, CreatedByUserId=Uid, CreatedByName=UName, Note=req.Note, NoteType=req.NoteType };
        _db.CrmTicketNotes.Add(note);
        if (req.NoteType == "CALL" && req.CallStatus != null)
        {
            ticket.CallStatus = req.CallStatus;
            if (req.CallStatus == "COMPLETED") ticket.Status = "IN_PROGRESS";
        }
        await _db.SaveChangesAsync();
        return Ok(note);
    }

    [HttpPost("tickets/{id}/schedule-call"), Authorize]
    public async Task<IActionResult> ScheduleCall(int id, [FromBody] ScheduleCallRequest req)
    {
        var ticket = await _db.CrmTickets.FindAsync(id);
        if (ticket == null) return NotFound();
        ticket.CallScheduled = req.CallDateTime;
        ticket.CallStatus    = "SCHEDULED";
       // _db.CrmTicketNotes.Add(new CrmTicketNote { TicketId=id, CreatedByUserId=Uid, CreatedByName=UName, Note=$"Call scheduled for {req.CallDateTime:dd MMM yyyy HH:mm}. Reason: {req.Reason??\"Follow-up\"}", NoteType="CALL" });
       // await _db.SaveChangesAsync();
        return Ok(new { message = "Call scheduled", callDateTime=req.CallDateTime });
    }

    // ── STAFF ────────────────────────────────────────────────
    [HttpGet("staff"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ListStaff([FromQuery] string? role)
    {
        var q = _db.StaffMembers.Include(s => s.User).AsQueryable();
        if (!string.IsNullOrEmpty(role)) q = q.Where(s => s.Role == role);
        return Ok(await q.OrderBy(s => s.Name).ToListAsync());
    }

    [HttpPost("staff"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AddStaff([FromBody] AddStaffRequest req)
    {
        var phone = req.Phone.Replace("+91","").Trim();
        var user = await _db.User.FirstOrDefaultAsync(u => u.Phone == phone);
        if (user == null)
        {
            user = new User { Phone=phone, Name=req.Name, Email=req.Email, Role=req.Role };
            _db.User.Add(user);
            await _db.SaveChangesAsync();
        }
        else user.Role = req.Role;

        if (!await _db.AdminUsers.AnyAsync(a => a.UserId == user.Id))
            _db.AdminUsers.Add(new AdminUser { UserId=user.Id, Role=req.Role });

        var staff = new StaffMember { UserId=user.Id, Name=req.Name, Phone=phone, Email=req.Email, Role=req.Role, Team=req.Team, Territory=req.Territory, Status="ACTIVE" };
        _db.StaffMembers.Add(staff);
        await _db.SaveChangesAsync();
        return Ok(new { message = $"{req.Role} added", staff, userId=user.Id });
    }

    [HttpPut("staff/{id}"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateStaff(int id, [FromBody] AddStaffRequest req)
    {
        var staff = await _db.StaffMembers.FindAsync(id);
        if (staff == null) return NotFound();
        staff.Name=req.Name; staff.Team=req.Team; staff.Territory=req.Territory;
        staff.Email=req.Email??staff.Email; staff.Status=req.Status??staff.Status;
        await _db.SaveChangesAsync();
        return Ok(staff);
    }

    // ── TARGETS ──────────────────────────────────────────────
    [HttpGet("staff/{staffId}/targets"), Authorize]
    public async Task<IActionResult> GetTargets(int staffId, [FromQuery] DateTime? date)
    {
        var d = (date ?? DateTime.Today).Date;
        return Ok(await _db.DailyTargets.Where(t => t.StaffMemberId == staffId && t.TargetDate.Date == d).ToListAsync());
    }

    [HttpGet("staff/{staffId}/targets/summary"), Authorize]
    public async Task<IActionResult> TargetSummary(int staffId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var f = from ?? DateTime.Today.AddDays(-30);
        var t = to   ?? DateTime.Today;
        var targets = await _db.DailyTargets.Where(x => x.StaffMemberId == staffId && x.TargetDate >= f && x.TargetDate <= t).ToListAsync();
        var summary = targets.GroupBy(x => x.TargetType).Select(g => new { type=g.Key, totalTarget=g.Sum(x=>x.TargetCount), totalAchieved=g.Sum(x=>x.AchievedCount), days=g.Count(), hitRate=g.Sum(x=>x.TargetCount)>0?Math.Round((double)g.Sum(x=>x.AchievedCount)/g.Sum(x=>x.TargetCount)*100,1):0.0 });
        return Ok(new { staffId, from=f, to=t, summary, raw=targets });
    }

    [HttpPost("staff/{staffId}/targets"), Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SetTarget(int staffId, [FromBody] SetTargetRequest req)
    {
        var existing = await _db.DailyTargets.FirstOrDefaultAsync(t => t.StaffMemberId == staffId && t.TargetDate.Date == req.TargetDate.Date && t.TargetType == req.TargetType);
        if (existing != null) { existing.TargetCount=req.TargetCount; existing.Notes=req.Notes; }
        else _db.DailyTargets.Add(new DailyTarget { StaffMemberId=staffId, TargetDate=req.TargetDate, TargetType=req.TargetType, TargetCount=req.TargetCount, Notes=req.Notes });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Target set" });
    }

    [HttpPut("staff/{staffId}/targets/{targetId}/achieved"), Authorize]
    public async Task<IActionResult> UpdateAchieved(int staffId, int targetId, [FromBody] UpdateAchievedRequest req)
    {
        var target = await _db.DailyTargets.FindAsync(targetId);
        if (target == null || target.StaffMemberId != staffId) return NotFound();
        target.AchievedCount = req.AchievedCount;
        await _db.SaveChangesAsync();
        return Ok(target);
    }

    // ── CRM DASHBOARD ─────────────────────────────────────────
    [HttpGet("dashboard"), Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var today = DateTime.Today;
        return Ok(new
        {
            openTickets    = await _db.CrmTickets.CountAsync(t => t.Status == "OPEN"),
            inProgress     = await _db.CrmTickets.CountAsync(t => t.Status == "IN_PROGRESS"),
            resolvedToday  = await _db.CrmTickets.CountAsync(t => t.Status == "RESOLVED" && t.ResolvedAt.HasValue && t.ResolvedAt.Value.Date == today),
            callsToday     = await _db.CrmTickets.CountAsync(t => t.CallScheduled.HasValue && t.CallScheduled.Value.Date == today),
            staffCount     = await _db.StaffMembers.CountAsync(s => s.Status == "ACTIVE"),
            myOpenTickets  = await _db.CrmTickets.CountAsync(t => t.AssignedToUserId == Uid && t.Status == "OPEN"),
        });
    }
}

// ════════════════════════════════════════════════════════════════
// ADDRESSES
// ════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/addresses")]
[Tags("User")]
public class AddressesController : ControllerBase
{
    private readonly AppDbContext _db;
    public AddressesController(AppDbContext db) => _db = db;
    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    [HttpGet, Authorize]
    public async Task<IActionResult> List() => Ok(await _db.Addresses.Where(a => a.UserId == Uid && a.IsActive).OrderByDescending(a => a.IsDefault).ToListAsync());

    [HttpPost, Authorize]
    public async Task<IActionResult> Add([FromBody] Address req)
    {
        req.UserId = Uid;
        if (req.IsDefault)
        {
            var existing = await _db.Addresses.Where(a => a.UserId == Uid && a.IsDefault).ToListAsync();
            existing.ForEach(a => a.IsDefault = false);
        }
        _db.Addresses.Add(req);
        await _db.SaveChangesAsync();
        return Ok(req);
    }

    [HttpPut("{id}"), Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] Address req)
    {
        var addr = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == Uid);
        if (addr == null) return NotFound();
        addr.Label=req.Label; addr.FullAddress=req.FullAddress; addr.Area=req.Area; addr.City=req.City; addr.Pincode=req.Pincode; addr.Type=req.Type; addr.IsDefault=req.IsDefault;
        await _db.SaveChangesAsync();
        return Ok(addr);
    }

    [HttpDelete("{id}"), Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var addr = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == Uid);
        if (addr == null) return NotFound();
        addr.IsActive = false;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Address removed" });
    }
}
