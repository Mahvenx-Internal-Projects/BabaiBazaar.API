using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BabaiBazaar.API.Models;

// ── VENDOR ────────────────────────────────────────────────────
public class Vendor : BaseEntity
{
    public int?    UserId          { get; set; }
    public int?    VendorPlanId    { get; set; }
    [MaxLength(200)] public string  BusinessName   { get; set; } = "";
    [MaxLength(50)]  public string  BusinessType   { get; set; } = "Grocery";
    [MaxLength(15)]  public string  Phone          { get; set; } = "";
    [MaxLength(200)] public string? Email          { get; set; }
    [MaxLength(20)]  public string  Status         { get; set; } = "PENDING"; // PENDING|APPROVED|SUSPENDED
    [MaxLength(500)] public string? LogoUrl        { get; set; }
    [MaxLength(100)] public string? City           { get; set; }
    [MaxLength(10)]  public string? Pincode        { get; set; }
    [MaxLength(500)] public string? Address        { get; set; }
    [MaxLength(30)]  public string? GstNumber      { get; set; }
    [MaxLength(30)]  public string? FssaiNumber    { get; set; }
    [MaxLength(30)]  public string? PanNumber      { get; set; }
    [MaxLength(20)]  public string  OperatingFrom  { get; set; } = "08:00";
    [MaxLength(20)]  public string  OperatingTo    { get; set; } = "22:00";
    public bool   IsOpen           { get; set; } = true;
    [Column(TypeName="decimal(5,2)")] public decimal CommissionRate { get; set; } = 15;
    [Column(TypeName="decimal(12,2)")] public decimal TotalRevenue { get; set; } = 0;
    public int    TotalOrders      { get; set; } = 0;
    [Column(TypeName="decimal(3,2)")] public decimal Rating       { get; set; } = 0;
    public int?   ApprovedBy       { get; set; }
    public DateTime? ApprovedAt    { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }
    public string? OwnerName       { get; set; }
    public string? OwnerPhone      { get; set; }

    public ICollection<VendorDocument>   Documents { get; set; } = new List<VendorDocument>();
    public ICollection<VendorUserMapping> Users    { get; set; } = new List<VendorUserMapping>();
    public ICollection<VendorPayout>     Payouts  { get; set; } = new List<VendorPayout>();
}

public class VendorDocument : BaseEntity
{
    public int     VendorId { get; set; }
    [MaxLength(30)]  public string DocType  { get; set; } = ""; // GSTIN|FSSAI|PAN|AADHAAR|SHOP_ACT|BANK_PROOF|LOGO|TNC_SIGNED
    [MaxLength(500)] public string FileUrl  { get; set; } = "";
    [MaxLength(20)]  public string Status   { get; set; } = "PENDING"; // PENDING|APPROVED|REJECTED
    [MaxLength(500)] public string Remarks  { get; set; } = "";
    public Vendor  Vendor { get; set; } = null!;
}

public class VendorUserMapping : BaseEntity
{
    public int     VendorId { get; set; }
    public int     UserId   { get; set; }
    [MaxLength(50)] public string Role { get; set; } = "Manager"; // Owner|Manager|Operator|DeliverySupervisor
    public Vendor  Vendor { get; set; } = null!;
    public User    User   { get; set; } = null!;
}

public class VendorPlan : BaseEntity
{
    [MaxLength(50)]  public string  Name                { get; set; } = "";
    [MaxLength(20)]  public string  Code                { get; set; } = "";
    [MaxLength(300)] public string? Description         { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal Price { get; set; } = 0;
    [Column(TypeName="decimal(5,2)")] public decimal CommissionPct { get; set; } = 15;
    public int    MaxProducts       { get; set; } = 100;
    public int    MaxOrders         { get; set; } = 500;
    public int    MaxDeliveryBoys   { get; set; } = 3;
    public bool   CanCreateOffers   { get; set; } = false;
    public bool   CanCreatePromoCodes { get; set; } = false;
    public bool   FeaturedListing   { get; set; } = false;
    public bool   AnalyticsDashboard{ get; set; } = false;
    [MaxLength(20)] public string? Color { get; set; }
    [MaxLength(10)] public string? Emoji { get; set; }
    public bool   IsFeatured        { get; set; } = false;
    public ICollection<Vendor> Vendors { get; set; } = new List<Vendor>();
}

public class VendorPlanPayment : BaseEntity
{
    public int     VendorId    { get; set; }
    public int     PlanId      { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal Amount { get; set; }
    [MaxLength(100)] public string? RazorpayOrderId   { get; set; }
    [MaxLength(100)] public string? RazorpayPaymentId { get; set; }
    [MaxLength(20)]  public string  Status            { get; set; } = "PENDING";
    public DateTime ValidFrom   { get; set; } = DateTime.UtcNow;
    public DateTime ValidTill   { get; set; } = DateTime.UtcNow.AddMonths(1);
}

public class VendorPayout : BaseEntity
{
    public int     VendorId      { get; set; }
    [Column(TypeName="decimal(12,2)")] public decimal GrossAmount { get; set; }
    [Column(TypeName="decimal(12,2)")] public decimal Commission  { get; set; }
    [Column(TypeName="decimal(12,2)")] public decimal NetAmount   { get; set; }
    [MaxLength(20)] public string Status       { get; set; } = "PENDING"; // PENDING|PROCESSED|FAILED
    [MaxLength(100)] public string? UtrNumber  { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Vendor  Vendor        { get; set; } = null!;
}

// ── DELIVERY BOY ──────────────────────────────────────────────
public class DeliveryBoy : BaseEntity
{
    public int?    UserId       { get; set; }
    public int?    VendorId     { get; set; }  // null = platform delivery boy
    [MaxLength(100)] public string  Name          { get; set; } = "";
    [MaxLength(15)]  public string  Phone         { get; set; } = "";
    [MaxLength(20)]  public string  Status        { get; set; } = "PENDING"; // PENDING|ACTIVE|INACTIVE|SUSPENDED
    [MaxLength(30)]  public string  VehicleType   { get; set; } = "Bike";
    [MaxLength(20)]  public string? VehicleNumber { get; set; }
    [Required]
    [MaxLength(12)]  public string? AadhaarNumber { get; set; }
    public bool   IsOnline       { get; set; } = false;
    [Column(TypeName="decimal(3,2)")] public decimal Rating     { get; set; } = 0;
    public int    TotalDeliveries { get; set; } = 0;
    [Column(TypeName="decimal(12,2)")] public decimal WalletBalance { get; set; } = 0;

    public ICollection<DeliveryBoyPayout> Payouts { get; set; } = new List<DeliveryBoyPayout>();
}

public class DeliveryBoyPayout : BaseEntity
{
    public int     DeliveryBoyId { get; set; }
    [Column(TypeName="decimal(12,2)")] public decimal Amount { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "PENDING";
    [MaxLength(100)] public string? UtrNumber { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DeliveryBoy DeliveryBoy { get; set; } = null!;
}

// ── SERVICE PERSON ────────────────────────────────────────────
public class ServicePerson : BaseEntity
{
    public int?    UserId        { get; set; }
    [MaxLength(100)] public string  Name          { get; set; } = "";
    [MaxLength(15)]  public string  Phone         { get; set; } = "";
    [MaxLength(200)] public string? Email         { get; set; }
    [MaxLength(20)]  public string  Status        { get; set; } = "PENDING"; // PENDING|ACTIVE|INACTIVE|SUSPENDED
    [MaxLength(500)] public string? Bio           { get; set; }
    [MaxLength(500)] public string? SkillTags     { get; set; }  // "Cleaning,Plumbing,Electrician"
    [MaxLength(50)]  public string? Experience    { get; set; }
    public bool   IsVerified     { get; set; } = false;
    public bool   IsAvailable    { get; set; } = true;
    [Column(TypeName="decimal(3,2)")] public decimal Rating         { get; set; } = 0;
    public int    TotalJobs      { get; set; } = 0;
    [Column(TypeName="decimal(12,2)")] public decimal WalletBalance { get; set; } = 0;
    [MaxLength(20)] public string? PaymentType   { get; set; } = "PER_JOB"; // PER_JOB|HOURLY|WEEKLY|MONTHLY
    [Column(TypeName="decimal(10,2)")] public decimal RateAmount    { get; set; } = 0;

    public ICollection<ServicePersonDocument>  Documents { get; set; } = new List<ServicePersonDocument>();
    public ICollection<ServicePersonPayout>    Payouts   { get; set; } = new List<ServicePersonPayout>();
}

public class ServicePersonDocument : BaseEntity
{
    public int     ServicePersonId { get; set; }
    [MaxLength(30)]  public string DocType { get; set; } = "";
    // AADHAAR|PAN|POLICE_CLEARANCE|SKILL_CERT|PHOTO|BANK_PROOF|TNC_SIGNED|ADDRESS_PROOF
    [MaxLength(500)] public string FileUrl { get; set; } = "";
    [MaxLength(20)]  public string Status  { get; set; } = "PENDING";
    [MaxLength(500)] public string Remarks { get; set; } = "";
    public ServicePerson ServicePerson { get; set; } = null!;
}

public class ServicePersonPayout : BaseEntity
{
    public int     ServicePersonId { get; set; }
    [Column(TypeName="decimal(12,2)")] public decimal GrossAmount  { get; set; }
    [Column(TypeName="decimal(12,2)")] public decimal PlatformFee  { get; set; }
    [Column(TypeName="decimal(12,2)")] public decimal TravelAllow  { get; set; } = 0;
    [Column(TypeName="decimal(12,2)")] public decimal NetAmount    { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "PENDING";
    [MaxLength(100)] public string? UtrNumber { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public ServicePerson ServicePerson { get; set; } = null!;
}

// ── OFFERS ────────────────────────────────────────────────────
public class Offer : BaseEntity
{
    public int?    VendorId      { get; set; }
    [MaxLength(200)] public string  TitleEn      { get; set; } = "";
    [MaxLength(200)] public string  TitleTe      { get; set; } = "";
    [MaxLength(20)]  public string  Type         { get; set; } = "PERCENT"; // PERCENT|FLAT|FREE_DELIVERY|BOGO
    [Column(TypeName="decimal(10,2)")] public decimal Value       { get; set; } = 0;
    [Column(TypeName="decimal(10,2)")] public decimal MinOrder    { get; set; } = 0;
    [Column(TypeName="decimal(10,2)")] public decimal MaxDiscount { get; set; } = 0;
    public DateTime? ValidTill   { get; set; }
    [MaxLength(10)]  public string? Emoji        { get; set; }
    [MaxLength(20)]  public string? Color        { get; set; }
    [MaxLength(500)] public string? ImageUrl     { get; set; }
}

// ── ADMIN USERS ───────────────────────────────────────────────
public class AdminUser : BaseEntity
{
    public int     UserId        { get; set; }
    [MaxLength(50)]  public string  Role        { get; set; } = "Support"; // SuperAdmin|Admin|Manager|Support|Analyst|Marketing|Telecaller|FieldAgent
    [MaxLength(200)] public string? Permissions { get; set; }  // JSON array
    public bool   IsSuperAdmin   { get; set; } = false;
    public DateTime? LastLogin   { get; set; }
    public User   User           { get; set; } = null!;
}

// ── PLATFORM SETTING ──────────────────────────────────────────
public class PlatformSetting : BaseEntity
{
    [MaxLength(100)]  public string  Key         { get; set; } = "";
    [MaxLength(2000)] public string  Value       { get; set; } = "";
    [MaxLength(50)]   public string  Group       { get; set; } = "general";
    [MaxLength(300)]  public string? Description { get; set; }
    [MaxLength(20)]   public string? Type        { get; set; } = "text"; // text|toggle|number
}

// ── ONBOARDING AGREEMENT ──────────────────────────────────────
public class OnboardingAgreement : BaseEntity
{
    [MaxLength(20)] public string  PartyType        { get; set; } = ""; // VENDOR|SERVICE_PERSON
    public int?    VendorId         { get; set; }
    public int?    ServicePersonId  { get; set; }
    [MaxLength(50)]  public string  AgreementType   { get; set; } = ""; // VENDOR_TNC|SP_TNC|PRIVACY_POLICY|NDA
    [MaxLength(500)] public string  DocumentUrl     { get; set; } = "";
    [MaxLength(500)] public string  SignatureUrl    { get; set; } = "";
    [MaxLength(1000)] public string? Notes          { get; set; }
    [MaxLength(100)] public string? SignedByName    { get; set; }
    public DateTime SignedAt         { get; set; } = DateTime.UtcNow;
    [MaxLength(45)]  public string  IpAddress       { get; set; } = "";
    public int?    CollectedByUserId { get; set; }
    [MaxLength(20)] public string  CollectionMethod { get; set; } = "PORTAL"; // PORTAL|FIELD|TELECALLER
    [MaxLength(20)] public string  Status           { get; set; } = "SIGNED"; // SIGNED|PENDING|EXPIRED
}

// ── CRM TICKET ────────────────────────────────────────────────
public class CrmTicket : BaseEntity
{
    [MaxLength(20)]   public string  TargetType       { get; set; } = ""; // VENDOR|SERVICE_PERSON|LEAD
    public int?       VendorId        { get; set; }
    public int?       ServicePersonId { get; set; }
    [MaxLength(100)]  public string?  LeadName        { get; set; }
    [MaxLength(15)]   public string?  LeadPhone       { get; set; }
    [MaxLength(50)]   public string   TicketType      { get; set; } = ""; // ONBOARDING|FOLLOW_UP|COMPLAINT|DOCUMENT|CALL_BACK
    [MaxLength(200)]  public string   Subject         { get; set; } = "";
    [MaxLength(2000)] public string   Description     { get; set; } = "";
    [MaxLength(20)]   public string   Priority        { get; set; } = "MEDIUM"; // LOW|MEDIUM|HIGH|URGENT
    [MaxLength(20)]   public string   Status          { get; set; } = "OPEN"; // OPEN|IN_PROGRESS|RESOLVED|CLOSED
    public int?       AssignedToUserId { get; set; }
    [MaxLength(100)]  public string?  AssignedToName  { get; set; }
    public int        CreatedByUserId  { get; set; }
    [MaxLength(2000)] public string?  Resolution      { get; set; }
    public DateTime?  ResolvedAt       { get; set; }
    public DateTime?  DueDate          { get; set; }
    public DateTime?  CallScheduled    { get; set; }
    [MaxLength(20)]   public string?  CallStatus      { get; set; } // SCHEDULED|COMPLETED|MISSED|RESCHEDULED

    public ICollection<CrmTicketNote> Notes { get; set; } = new List<CrmTicketNote>();
}

public class CrmTicketNote : BaseEntity
{
    public int     TicketId         { get; set; }
    public int     CreatedByUserId  { get; set; }
    [MaxLength(100)] public string  CreatedByName { get; set; } = "";
    [MaxLength(2000)] public string Note          { get; set; } = "";
    [MaxLength(20)]  public string  NoteType      { get; set; } = "NOTE"; // NOTE|CALL|EMAIL|VISIT|STATUS_CHANGE
    public CrmTicket Ticket         { get; set; } = null!;
}

// ── STAFF MEMBER ──────────────────────────────────────────────
public class StaffMember : BaseEntity
{
    public int     UserId       { get; set; }
    [MaxLength(100)] public string  Name      { get; set; } = "";
    [MaxLength(15)]  public string  Phone     { get; set; } = "";
    [MaxLength(200)] public string? Email     { get; set; }
    [MaxLength(30)]  public string  Role      { get; set; } = ""; // MARKETING|TELECALLER|FIELD_AGENT
    [MaxLength(100)] public string? Team      { get; set; }
    [MaxLength(100)] public string? Territory { get; set; }
    [MaxLength(20)]  public string  Status    { get; set; } = "ACTIVE";
    public User    User          { get; set; } = null!;
    public ICollection<DailyTarget> Targets { get; set; } = new List<DailyTarget>();
}

public class DailyTarget : BaseEntity
{
    public int      StaffMemberId  { get; set; }
    public DateTime TargetDate     { get; set; }
    [MaxLength(30)] public string  TargetType    { get; set; } = ""; // VENDOR_ONBOARD|SP_ONBOARD|CALLS|VISITS|FOLLOW_UPS
    public int      TargetCount    { get; set; } = 0;
    public int      AchievedCount  { get; set; } = 0;
    [MaxLength(500)] public string? Notes        { get; set; }
    public StaffMember StaffMember { get; set; } = null!;
}

// ── BOOKING STATUS LOG ────────────────────────────────────────
public class BookingStatusLog : BaseEntity
{
    public int     BookingId    { get; set; }
    [MaxLength(20)] public string StatusFrom { get; set; } = "";
    [MaxLength(20)] public string StatusTo   { get; set; } = "";
    public int     ChangedBy    { get; set; }
    [MaxLength(500)] public string? Note    { get; set; }
}
