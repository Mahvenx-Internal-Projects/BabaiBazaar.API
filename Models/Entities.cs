using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BabaiBazaar.API.Models;


// ── BASE ──────────────────────────────────────────────────────
public abstract class BaseEntity
{
    public int      Id        { get; set; }
    public bool     IsActive  { get; set; } = true;
    public int      SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ── USER ──────────────────────────────────────────────────────
public class User : BaseEntity
{
    [MaxLength(15)]  public string  Phone          { get; set; } = "";
    [MaxLength(100)] public string  Name           { get; set; } = "";
    [MaxLength(200)] public string? Email          { get; set; }
    [MaxLength(200)] public string? GoogleId       { get; set; }
    [MaxLength(500)] public string? ProfileImage   { get; set; }
    [MaxLength(5)]   public string  Language       { get; set; } = "en";
    [MaxLength(20)]  public string  Role           { get; set; } = "Customer"; // Customer|Vendor|Admin|SuperAdmin|Marketing|Telecaller|FieldAgent
    [MaxLength(100)] public string? Username       { get; set; }  // Portal login username
    [MaxLength(255)] public string? PasswordHash   { get; set; }  // PBKDF2 hashed password
    public bool   PhoneVerified   { get; set; } = false;
    public decimal WalletBalance  { get; set; } = 0;
    public int    LoyaltyPoints   { get; set; } = 0;
    public DateTime? LastLogin    { get; set; }

    // Navigation
    public ICollection<Address>   Addresses { get; set; } = new List<Address>();
    public ICollection<CartItem>  CartItems { get; set; } = new List<CartItem>();
}

// ── OTP ───────────────────────────────────────────────────────
public class OtpRecord : BaseEntity
{
    [MaxLength(15)] public string   Phone     { get; set; } = "";
    [MaxLength(6)]  public string   Code      { get; set; } = "";
    public bool                     IsUsed    { get; set; } = false;
    public DateTime                 ExpiresAt { get; set; }
}

// ── ADDRESS ───────────────────────────────────────────────────
public class Address : BaseEntity
{
    public int     UserId      { get; set; }
    [MaxLength(20)]  public string  Type       { get; set; } = "Home"; // Home|Work|Other
    [MaxLength(100)] public string  Label      { get; set; } = "";
    [MaxLength(500)] public string  FullAddress{ get; set; } = "";
    [MaxLength(100)] public string? Area       { get; set; }
    [MaxLength(100)] public string? City       { get; set; }
    [MaxLength(10)]  public string? Pincode    { get; set; }
    [Column(TypeName="decimal(9,6)")] public decimal? Latitude  { get; set; }
    [Column(TypeName="decimal(9,6)")] public decimal? Longitude { get; set; }
    public bool   IsDefault   { get; set; } = false;
    public User   User        { get; set; } = null!;
}

// ── CATEGORY ──────────────────────────────────────────────────
public class Category : BaseEntity
{
    [MaxLength(100)] public string  NameEn     { get; set; } = "";
    [MaxLength(100)] public string  NameTe     { get; set; } = "";
    [MaxLength(200)] public string? Slug       { get; set; }
    [MaxLength(10)]  public string  Emoji      { get; set; } = "📦";
    [MaxLength(30)]  public string  Type       { get; set; } = "grocery"; // grocery|food|service|pharma|beauty
    [MaxLength(20)]  public string  ColorBg    { get; set; } = "#E6F9EE";
    [MaxLength(20)]  public string  ColorText  { get; set; } = "#1E9E4F";
    [MaxLength(500)] public string? ImageUrl   { get; set; }

    public ICollection<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
}

// ── SUB-CATEGORY ──────────────────────────────────────────────
public class SubCategory : BaseEntity
{
    public int     CategoryId  { get; set; }
    [MaxLength(100)] public string  NameEn   { get; set; } = "";
    [MaxLength(100)] public string  NameTe   { get; set; } = "";
    [MaxLength(10)]  public string  Emoji    { get; set; } = "📁";
    [MaxLength(500)] public string? ImageUrl { get; set; }

    public Category             Category { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

// ── PRODUCT ───────────────────────────────────────────────────
public class Product : BaseEntity
{
    public int?    VendorId       { get; set; }    // null = platform product
    public int?    SubCategoryId  { get; set; }
    [MaxLength(200)] public string  NameEn       { get; set; } = "";
    [MaxLength(200)] public string  NameTe       { get; set; } = "";
    [MaxLength(10)]  public string  Emoji        { get; set; } = "📦";
    [MaxLength(50)]  public string? Unit         { get; set; }  // kg|litre|pcs
    [Column(TypeName="decimal(10,2)")] public decimal Price     { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal Mrp       { get; set; }
    public int     Stock          { get; set; } = 100;
    [MaxLength(100)] public string? TagEn        { get; set; }  // "Organic"|"Fresh"
    [MaxLength(100)] public string? TagTe        { get; set; }
    [MaxLength(500)] public string? ImageUrl     { get; set; }
    [MaxLength(1000)] public string? Description { get; set; }

    public SubCategory? SubCategory { get; set; }
}

// ── SERVICE ───────────────────────────────────────────────────
public class Service : BaseEntity
{
    [MaxLength(200)] public string  NameEn       { get; set; } = "";
    [MaxLength(200)] public string  NameTe       { get; set; } = "";
    [MaxLength(10)]  public string  Emoji        { get; set; } = "🔧";
    [MaxLength(100)] public string  Category     { get; set; } = "";  // Cleaning|Plumbing|Electrician
    [Column(TypeName="decimal(10,2)")] public decimal Price     { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal Mrp       { get; set; }
    [MaxLength(50)]  public string? DurationLabel{ get; set; }  // "2-3 hrs"
    [MaxLength(500)] public string? ImageUrl     { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }

    public ICollection<ServiceInclude>     Includes     { get; set; } = new List<ServiceInclude>();
    public ICollection<ServiceInstruction> Instructions { get; set; } = new List<ServiceInstruction>();
    public ICollection<ServiceBooking>     Bookings     { get; set; } = new List<ServiceBooking>();
}

public class ServiceInclude : BaseEntity
{
    public int    ServiceId { get; set; }
    [MaxLength(200)] public string TextEn { get; set; } = "";
    [MaxLength(200)] public string TextTe { get; set; } = "";
    public Service Service { get; set; } = null!;
}

public class ServiceInstruction : BaseEntity
{
    public int    ServiceId { get; set; }
    [MaxLength(500)] public string TextEn { get; set; } = "";
    [MaxLength(500)] public string TextTe { get; set; } = "";
    public Service Service { get; set; } = null!;
}

// ── CART ──────────────────────────────────────────────────────
public class CartItem : BaseEntity
{
    public int     UserId    { get; set; }
    public int     ProductId { get; set; }
    public int     Qty       { get; set; } = 1;
    public User    User      { get; set; } = null!;
    public Product Product   { get; set; } = null!;
}

// ── COUPON ────────────────────────────────────────────────────
public class Coupon : BaseEntity
{
    [MaxLength(30)]  public string  Code           { get; set; } = "";
    [MaxLength(100)] public string  TitleEn        { get; set; } = "";
    [MaxLength(100)] public string  TitleTe        { get; set; } = "";
    [MaxLength(300)] public string  DescEn         { get; set; } = "";
    [MaxLength(300)] public string  DescTe         { get; set; } = "";
    [MaxLength(20)]  public string  CartType       { get; set; } = "ALL"; // ALL|GROCERY|SERVICE
    [Column(TypeName="decimal(10,2)")] public decimal MinOrder      { get; set; } = 0;
    [MaxLength(20)]  public string  DiscountType   { get; set; } = "percent"; // percent|flat|delivery|free_service_fee
    [Column(TypeName="decimal(10,2)")] public decimal DiscountValue { get; set; } = 0;
    [Column(TypeName="decimal(10,2)")] public decimal MaxDiscount   { get; set; } = 0;
    public DateTime ValidFrom     { get; set; } = DateTime.UtcNow;
    public DateTime ValidTill     { get; set; } = DateTime.UtcNow.AddMonths(3);
    public int      UsageLimit    { get; set; } = 1000;
    public int      PerUserLimit  { get; set; } = 1;
    public int      UsedCount     { get; set; } = 0;
    public bool     IsFirstOrderOnly { get; set; } = false;
    public bool     IsPublic      { get; set; } = true;
    [MaxLength(20)] public string? Color           { get; set; }

    public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
}

public class CouponUsage : BaseEntity
{
    public int     CouponId     { get; set; }
    public int     UserId       { get; set; }
    public int?    OrderId      { get; set; }
    public int?    BookingId    { get; set; }
    [MaxLength(20)] public string CartType { get; set; } = "GROCERY";
    [Column(TypeName="decimal(10,2)")] public decimal DiscountGiven { get; set; }
    public DateTime UsedAt      { get; set; } = DateTime.UtcNow;
    public Coupon   Coupon      { get; set; } = null!;
    public User     User        { get; set; } = null!;
}

// ── ORDER ─────────────────────────────────────────────────────
public class Order : BaseEntity
{
    public int     UserId           { get; set; }
    public int?    VendorId         { get; set; }
    public int?    DeliveryBoyId    { get; set; }
    public int?    AddressId        { get; set; }
    public int?    CouponId         { get; set; }
    [MaxLength(20)] public string  Status         { get; set; } = "PENDING_PAYMENT";
    // PENDING_PAYMENT|CONFIRMED|ASSIGNED_TO_DELIVERY|OUT_FOR_DELIVERY|DELIVERED|CANCELLED
    [Column(TypeName="decimal(10,2)")] public decimal SubTotal      { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal DeliveryFee   { get; set; } = 0;
    [Column(TypeName="decimal(10,2)")] public decimal Discount      { get; set; } = 0;
    [Column(TypeName="decimal(10,2)")] public decimal TotalAmount   { get; set; }
    [MaxLength(20)] public string  PaymentMethod  { get; set; } = "COD"; // COD|ONLINE|WALLET
    [MaxLength(20)] public string  PaymentStatus  { get; set; } = "PENDING"; // PENDING|PAID|REFUNDED
    [MaxLength(100)] public string? RazorpayOrderId { get; set; }
    [MaxLength(100)] public string? RazorpayPaymentId { get; set; }
    [MaxLength(500)] public string? DeliveryAddress  { get; set; }
    [MaxLength(500)] public string? CancelReason     { get; set; }
    public DateTime? DeliveredAt  { get; set; }

    public User                     User         { get; set; } = null!;
    public ICollection<OrderItem>   Items        { get; set; } = new List<OrderItem>();
}

public class OrderItem : BaseEntity
{
    public int     OrderId   { get; set; }
    public int     ProductId { get; set; }
    [MaxLength(200)] public string ProductName { get; set; } = "";
    [Column(TypeName="decimal(10,2)")] public decimal UnitPrice { get; set; }
    public int     Qty       { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal Total     { get; set; }
    public Order   Order     { get; set; } = null!;
    public Product Product   { get; set; } = null!;
}

// ── SERVICE BOOKING ───────────────────────────────────────────
public class ServiceBooking : BaseEntity
{
    public int     UserId           { get; set; }
    public int     ServiceId        { get; set; }
    public int?    ServicePersonId  { get; set; }
    public int?    CouponId         { get; set; }
    [MaxLength(20)] public string  Status         { get; set; } = "CONFIRMED";
    // CONFIRMED|ASSIGNED|IN_PROGRESS|COMPLETED|CANCELLED|RESCHEDULED
    [Column(TypeName="decimal(10,2)")] public decimal Amount      { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal Discount    { get; set; } = 0;
    [MaxLength(500)] public string? Address       { get; set; }
    public DateTime? ScheduledDate  { get; set; }
    [MaxLength(50)] public string?  TimeSlot      { get; set; }
    [MaxLength(6)]  public string?  Otp           { get; set; }
    [MaxLength(500)] public string? CancelReason  { get; set; }
    public DateTime? CompletedAt    { get; set; }

    public User          User          { get; set; } = null!;
    public Service       Service       { get; set; } = null!;
}

// ── BANNER ────────────────────────────────────────────────────
public class Banner : BaseEntity
{
    [MaxLength(200)] public string  TitleEn  { get; set; } = "";
    [MaxLength(200)] public string  TitleTe  { get; set; } = "";
    [MaxLength(500)] public string  ImageUrl { get; set; } = "";
    [MaxLength(500)] public string? LinkUrl  { get; set; }
    [MaxLength(20)]  public string  Type     { get; set; } = "HOME"; // HOME|GROCERY|SERVICE
    [MaxLength(20)]  public string? Color    { get; set; }
    public DateTime? ValidTill { get; set; }
}

// ── REVIEW ────────────────────────────────────────────────────
public class Review : BaseEntity
{
    public int     UserId     { get; set; }
    public int?    OrderId    { get; set; }
    public int?    BookingId  { get; set; }
    public int?    VendorId   { get; set; }
    public int?    DeliveryBoyId { get; set; }
    public int?    ServicePersonId { get; set; }
    public int     Rating     { get; set; } = 5; // 1-5
    [MaxLength(1000)] public string? Comment { get; set; }
    public User    User       { get; set; } = null!;
}

// ── WALLET TRANSACTION ────────────────────────────────────────
public class WalletTransaction : BaseEntity
{
    public int     UserId      { get; set; }
    [MaxLength(20)]  public string  Type       { get; set; } = "CREDIT"; // CREDIT|DEBIT
    [Column(TypeName="decimal(10,2)")] public decimal Amount    { get; set; }
    [MaxLength(200)] public string  Description{ get; set; } = "";
    [MaxLength(20)]  public string? RefType    { get; set; } // ORDER|BOOKING|PAYOUT|BONUS
    public int?    RefId       { get; set; }
    public User    User        { get; set; } = null!;
}

// ── TRANSLATION ───────────────────────────────────────────────
public class Translation : BaseEntity
{
    [MaxLength(100)] public string Key   { get; set; } = "";
    [MaxLength(50)]  public string Group { get; set; } = "common";
    [MaxLength(500)] public string En    { get; set; } = "";
    [MaxLength(500)] public string Te    { get; set; } = "";
}

// ── PINCODE ───────────────────────────────────────────────────
public class Pincode : BaseEntity
{
    [MaxLength(10)]  public string Pincode_    { get; set; } = "";
    [MaxLength(100)] public string Area        { get; set; } = "";
    [MaxLength(100)] public string City        { get; set; } = "";
    [MaxLength(100)] public string State       { get; set; } = "Telangana";
    public int DeliveryEta { get; set; } = 20; // minutes
}
