using System.ComponentModel.DataAnnotations;

namespace BabaiBazaar.API.DTOs;

// ── AUTH ──────────────────────────────────────────────────────
public record SendOtpRequest([Required] string Phone);
public record VerifyOtpRequest([Required] string Phone, [Required] string Otp, string? Name);
public record GoogleAuthRequest([Required] string IdToken);
public record PortalLoginRequest([Required] string Username, [Required] string Password);
public record SetPasswordRequest(int UserId, [Required] string Username, [Required] string Password);
public record ChangePasswordRequest([Required] string CurrentPassword, [Required] string NewPassword);

// ── CATEGORY ──────────────────────────────────────────────────
public record CreateCategoryRequest(
    [Required] string NameEn, string? NameTe, string? Emoji, string? Type,
    string? ColorBg, string? ColorText, string? ImageUrl, string? Slug, int SortOrder = 0);

public record CreateSubCategoryRequest(
    [Required] string NameEn, string? NameTe, string? Emoji, string? ImageUrl, int SortOrder = 0);

// ── PRODUCT ───────────────────────────────────────────────────
public record CreateProductRequest(
    [Required] string NameEn, string? NameTe, string? Emoji,
    int? SubCategoryId, int? VendorId,
    [Required][Range(0.01, 99999)] decimal Price, decimal? Mrp,
    int Stock = 100, string? Unit = null, string? TagEn = null, string? TagTe = null,
    string? ImageUrl = null, string? Description = null, bool IsActive = true);

// ── SERVICE ───────────────────────────────────────────────────
public record CreateServiceRequest(
    [Required] string NameEn, string? NameTe, string? Emoji, string? Category,
    [Required] decimal Price, decimal? Mrp, string? DurationLabel,
    string? ImageUrl, string? Description, bool IsActive = true);

// ── CART ──────────────────────────────────────────────────────
public record AddToCartRequest([Required] int ProductId, int Qty = 1);
public record UpdateCartRequest([Required] int CartItemId, int Qty = 1);

// ── ORDER ─────────────────────────────────────────────────────
public record PlaceOrderRequest(
    [Required] int AddressId, [Required] string PaymentMethod,
    string? CouponCode = null, decimal DeliveryFee = 30m);

public record UpdateOrderStatusRequest([Required] string Status, string? Reason = null);

// ── SERVICE BOOKING ───────────────────────────────────────────
public record CreateBookingRequest(
    [Required] int ServiceId, [Required] string Address,
    DateTime? ScheduledDate = null, string? TimeSlot = null,
    string? CouponCode = null);

public record UpdateBookingRequest(string? Status = null, int? ServicePersonId = null, string? Reason = null);
public record VerifyOtpBookingRequest([Required] string Otp);

// ── COUPON ────────────────────────────────────────────────────
public record ValidateCouponRequest(
    [Required] string Code, decimal OrderAmount,
    string CartType = "GROCERY", decimal DeliveryFee = 40m);

public record ValidateCouponResponse(
    bool IsValid, string ErrorCode, string Message,
    decimal DiscountAmount, decimal FinalAmount, decimal FinalDeliveryFee,
    string? AppliedCode, string? BadgeText, string? DiscountType);

public record ApplyCouponRequest(
    [Required] string Code, decimal OrderAmount, string CartType = "GROCERY",
    decimal DeliveryFee = 40m, int? OrderId = null, int? BookingId = null);

public record CreateCouponRequest(
    [Required] string Code, [Required] string TitleEn, string? TitleTe,
    string? DescEn, string? DescTe, string CartType = "ALL",
    decimal MinOrder = 0, string DiscountType = "percent",
    decimal DiscountValue = 0, decimal MaxDiscount = 0,
    DateTime? ValidFrom = null, DateTime? ValidTill = null,
    int UsageLimit = 9999, int PerUserLimit = 1,
    bool IsFirstOrderOnly = false, bool IsPublic = true, string? Color = null);

// ── VENDOR ────────────────────────────────────────────────────
public record RegisterVendorRequest(
    [Required] string BusinessName, string? BusinessType,
    [Required] string Phone, string? Email, string? City, string? Pincode,
    string? GstNumber, string? FssaiNumber, string? PanNumber,
    string? OwnerName = null, string? Description = null);

public record UpdateVendorRequest(
    string? BusinessName, string? BusinessType, string? City, string? Pincode,
    string? GstNumber, string? FssaiNumber, string? PanNumber,
    string? OperatingFrom, string? OperatingTo, string? LogoUrl,
    string? OwnerName, string? OwnerPhone, string? Email, string? Description);

public record MapUserRequest(
    int VendorId,
    [Required] string Phone,
    string Role = "Manager"
);

public record VendorBankRequest(
    string? AccountHolderName, string? AccountNumber, string? IfscCode,
    string? BankName, string? Branch, string? UpiId);

// ── DELIVERY BOY ──────────────────────────────────────────────
public record RegisterDeliveryBoyRequest(
    [Required(ErrorMessage = "Name is required")]
    string Name,

    [Required(ErrorMessage = "Phone is required")]
    string Phone,

    [Required(ErrorMessage = "Aadhaar Number is required")]
    [RegularExpression(@"^\d{12}$",
        ErrorMessage = "Aadhaar Number must be exactly 12 digits")]
    string AadhaarNumber,

    string VehicleType = "Bike",

    string? VehicleNumber = null
);

public record UpdateDeliveryBoyRequest(
    [Required] string Name,
    [Required] string Phone,
    [Required] string AadhaarNumber,
    string VehicleType = "Bike",
    string? VehicleNumber = null
);

// ── SERVICE PERSON ────────────────────────────────────────────
public record RegisterServicePersonRequest(
    [Required] string Name, [Required] string Phone,
    [Required]string Email = null, string? Bio = null, string? Skills = null, string? Experience = null);

public record SetPaymentConfigRequest(
    [Required] string PaymentType, decimal RateAmount = 0);

public record AllocateRequest(int BookingId, int ServicePersonId);

// ── VENDOR PLAN ───────────────────────────────────────────────
public record CreatePlanRequest(
    [Required] string Name, string? Code, decimal Price, decimal CommissionPct,
    int MaxProducts, int MaxOrders, int MaxDeliveryBoys,
    string? Description, string? Color, string? Emoji,
    bool CanCreateOffers = false, bool CanCreatePromoCodes = false,
    bool FeaturedListing = false, bool AnalyticsDashboard = false, bool IsFeatured = false);

public record SubscribePlanRequest([Required] int PlanId, string? RazorpayPaymentId = null);

// ── OFFER ─────────────────────────────────────────────────────
public record CreateOfferRequest(
    [Required] string TitleEn, string? TitleTe, string Type = "PERCENT",
    decimal Value = 0, decimal MinOrder = 0, decimal MaxDiscount = 0,
    DateTime? ValidTill = null, string? Emoji = null, string? Color = null, string? ImageUrl = null);

// ── PINCODE ───────────────────────────────────────────────────
public record CreatePincodeRequest(
    [Required] string Pincode, string? Area, [Required] string City,
    string State = "Telangana", int DeliveryEta = 20);

// ── TRANSLATION ───────────────────────────────────────────────
public record CreateTranslationRequest(
    [Required] string Key, string Group = "common",
    [Required] string En = "", string Te = "");

public record UpdateTranslationRequest(string? En = null, string? Te = null);

// ── UPLOAD ────────────────────────────────────────────────────
public record UploadResponse(string Url, string? Key = null);

// ── ADMIN ─────────────────────────────────────────────────────
public record CreateAdminRequest(
    [Required] string Name, [Required] string Phone,
    string? Email = null, string Role = "Support");

public record UpdateSettingRequest([Required] string Value);

// ── REVIEW ────────────────────────────────────────────────────
public record CreateReviewRequest(
    [Required][Range(1,5)] int Rating, string? Comment,
    int? OrderId = null, int? BookingId = null,
    int? VendorId = null, int? DeliveryBoyId = null, int? ServicePersonId = null);

// ── WALLET ────────────────────────────────────────────────────
public record WalletRequest([Required] decimal Amount, string? Reason = null);

// ── ONBOARDING ───────────────────────────────────────────────
public record UploadDocRequest([Required] string DocType, [Required] string FileUrl, string? Remarks = null);
public record UpdateDocStatusRequest([Required] string Status, string? Remarks = null);
public record RecordAgreementRequest(
    string? AgreementType, string? DocumentUrl, string? SignatureUrl,
    string? Notes, string? SignedByName, string? CollectionMethod);

// ── CRM ───────────────────────────────────────────────────────
public record CreateTicketRequest(
    [Required] string TargetType, int? VendorId, int? ServicePersonId,
    string? LeadName, string? LeadPhone,
    [Required] string TicketType, [Required] string Subject, [Required] string Description,
    string Priority = "MEDIUM", int? AssignedToUserId = null, string? AssignedToName = null,
    DateTime? DueDate = null, DateTime? CallScheduled = null);

public record UpdateTicketRequest(
    string? Status, string? Priority, int? AssignedToUserId, string? AssignedToName,
    string? Resolution, DateTime? DueDate, DateTime? CallScheduled, string? CallStatus);

public record AddNoteRequest([Required] string Note, string NoteType = "NOTE", string? CallStatus = null);
public record ScheduleCallRequest([Required] DateTime CallDateTime, string? Reason = null);

public record AddStaffRequest(
    [Required] string Name, [Required] string Phone, string? Email,
    [Required] string Role, string? Team, string? Territory, string? Status);

public record SetTargetRequest(
    [Required] DateTime TargetDate, [Required] string TargetType,
    int TargetCount = 1, string? Notes = null);

public record UpdateAchievedRequest(int AchievedCount);

// ── NOTIFICATION / PAYOUT ─────────────────────────────────────
public record SendNotificationRequest(
    [Required] string Title, [Required] string Body,
    string? Topic = null, string? UserId = null);

public record ProcessPayoutRequest([Required] string EntityType, [Required] int EntityId);

//── ADDRESS ─────────────────────────────────────
public record CreateAddressRequest(
    [Required] string Type,
    string? Label,
    [Required] string FullAddress,
    string? Area,
    [Required] string City,
    [Required] string Pincode,
    decimal? Latitude = null,
    decimal? Longitude = null,
    bool IsDefault = false,
    bool IsActive = true,
    int SortOrder = 0,
    int UserId=0
);
public record UpdateAddressRequest(
      [Required] string Type,
    string? Label,
    [Required] string FullAddress,
    string? Area,
    [Required] string City,
    [Required] string Pincode,
    decimal? Latitude = null,
    decimal? Longitude = null,
    bool IsDefault = false,
    bool IsActive = true,
    int SortOrder = 0,
    int UserId = 0
    );