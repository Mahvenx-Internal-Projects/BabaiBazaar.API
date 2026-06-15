using Microsoft.EntityFrameworkCore;
using BabaiBazaar.API.Models;

namespace BabaiBazaar.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Core ─────────────────────────────────────────────────
    public DbSet<User>                User                { get; set; }
    public DbSet<OtpRecord>           OtpRecords          { get; set; }
    public DbSet<Address>             Addresses           { get; set; }
    public DbSet<Category>            Categories          { get; set; }
    public DbSet<SubCategory>         SubCategories       { get; set; }
    public DbSet<Product>             Products            { get; set; }
    public DbSet<Service>             Services            { get; set; }
    public DbSet<ServiceInclude>      ServiceIncludes     { get; set; }
    public DbSet<ServiceInstruction>  ServiceInstructions { get; set; }
    public DbSet<CartItem>            CartItems           { get; set; }
    public DbSet<Coupon>              Coupons             { get; set; }
    public DbSet<CouponUsage>         CouponUsages        { get; set; }
    public DbSet<Order>               Orders              { get; set; }
    public DbSet<OrderItem>           OrderItems          { get; set; }
    public DbSet<ServiceBooking>      ServiceBookings     { get; set; }
    public DbSet<Banner>              Banners             { get; set; }
    public DbSet<Review>              Reviews             { get; set; }
    public DbSet<WalletTransaction>   WalletTransactions  { get; set; }
    public DbSet<Translation>         Translations        { get; set; }
    public DbSet<Pincode>             Pincodes            { get; set; }

    // ── Vendor ───────────────────────────────────────────────
    public DbSet<Vendor>              Vendors             { get; set; }
    public DbSet<VendorDocument>      VendorDocuments     { get; set; }
    public DbSet<VendorUserMapping>   VendorUserMappings  { get; set; }
    public DbSet<VendorPlan>          VendorPlans         { get; set; }
    public DbSet<VendorPlanPayment>   VendorPlanPayments  { get; set; }
    public DbSet<VendorPayout>        VendorPayouts       { get; set; }
    public DbSet<Offer>               Offers              { get; set; }

    // ── Delivery ─────────────────────────────────────────────
    public DbSet<DeliveryBoy>         DeliveryBoys        { get; set; }
    public DbSet<DeliveryBoyPayout>   DeliveryBoyPayouts  { get; set; }

    // ── Service Person ────────────────────────────────────────
    public DbSet<ServicePerson>       ServicePersons      { get; set; }
    public DbSet<ServicePersonDocument> ServicePersonDocuments { get; set; }
    public DbSet<ServicePersonPayout> ServicePersonPayouts{ get; set; }

    // ── Admin ─────────────────────────────────────────────────
    public DbSet<AdminUser>           AdminUsers          { get; set; }
    public DbSet<PlatformSetting>     PlatformSettings    { get; set; }
    public DbSet<BookingStatusLog>    BookingStatusLogs   { get; set; }

    // ── Onboarding & CRM ─────────────────────────────────────
    public DbSet<OnboardingAgreement> OnboardingAgreements{ get; set; }
    public DbSet<CrmTicket>           CrmTickets          { get; set; }
    public DbSet<CrmTicketNote>       CrmTicketNotes      { get; set; }
    public DbSet<StaffMember>         StaffMembers        { get; set; }
    public DbSet<DailyTarget>         DailyTargets        { get; set; }

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        // ── Unique indexes ────────────────────────────────────
        m.Entity<User>().HasIndex(u => u.Phone).IsUnique();
        m.Entity<User>().HasIndex(u => u.Username).IsUnique();
        m.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
        m.Entity<Translation>().HasIndex(t => t.Key).IsUnique();
        m.Entity<Pincode>().HasIndex(p => p.Pincode_).IsUnique();

        // ── CouponUsage ───────────────────────────────────────
        m.Entity<CouponUsage>()
            .HasOne(u => u.Coupon).WithMany(c => c.Usages).HasForeignKey(u => u.CouponId);
        m.Entity<CouponUsage>()
            .HasOne(u => u.User).WithMany().HasForeignKey(u => u.UserId);
        m.Entity<CouponUsage>()
            .HasIndex(u => new { u.CouponId, u.UserId });

        // ── Vendor ────────────────────────────────────────────
        m.Entity<Vendor>()
            .HasOne<VendorPlan>().WithMany(p => p.Vendors).HasForeignKey(v => v.VendorPlanId).IsRequired(false);
        m.Entity<VendorDocument>()
            .HasOne(d => d.Vendor).WithMany(v => v.Documents).HasForeignKey(d => d.VendorId);
        m.Entity<VendorUserMapping>()
            .HasOne(x => x.Vendor).WithMany(v => v.Users).HasForeignKey(x => x.VendorId);
        m.Entity<VendorUserMapping>()
            .HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        m.Entity<VendorPayout>()
            .HasOne(p => p.Vendor).WithMany(v => v.Payouts).HasForeignKey(p => p.VendorId);

        // ── Delivery boy ──────────────────────────────────────
        m.Entity<DeliveryBoyPayout>()
            .HasOne(p => p.DeliveryBoy).WithMany(d => d.Payouts).HasForeignKey(p => p.DeliveryBoyId);

        // ── Service person ────────────────────────────────────
        m.Entity<ServicePersonDocument>()
            .HasOne(d => d.ServicePerson).WithMany(s => s.Documents).HasForeignKey(d => d.ServicePersonId);
        m.Entity<ServicePersonPayout>()
            .HasOne(p => p.ServicePerson).WithMany(s => s.Payouts).HasForeignKey(p => p.ServicePersonId);

        // ── Service booking ───────────────────────────────────
        m.Entity<ServiceBooking>()
            .HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId);
        m.Entity<ServiceBooking>()
            .HasOne(b => b.Service).WithMany(s => s.Bookings).HasForeignKey(b => b.ServiceId);

        // ── Admin user ────────────────────────────────────────
        m.Entity<AdminUser>()
            .HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId);

        // ── CRM ───────────────────────────────────────────────
        m.Entity<CrmTicketNote>()
            .HasOne(n => n.Ticket).WithMany(t => t.Notes).HasForeignKey(n => n.TicketId);

        // ── CrmTicket optional FK (no nav props needed) ───────
        m.Entity<CrmTicket>().Property(t => t.VendorId).IsRequired(false);
        m.Entity<CrmTicket>().Property(t => t.ServicePersonId).IsRequired(false);

        // ── OnboardingAgreement optional FKs ─────────────────
        m.Entity<OnboardingAgreement>().Property(o => o.VendorId).IsRequired(false);
        m.Entity<OnboardingAgreement>().Property(o => o.ServicePersonId).IsRequired(false);

        // ── Staff member ──────────────────────────────────────
        m.Entity<StaffMember>()
            .HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<DailyTarget>()
            .HasOne(d => d.StaffMember).WithMany(s => s.Targets).HasForeignKey(d => d.StaffMemberId);

        // ── Wallet FK ─────────────────────────────────────────
        m.Entity<WalletTransaction>()
            .HasOne(w => w.User).WithMany().HasForeignKey(w => w.UserId);

        // ── Category / SubCategory ────────────────────────────
        m.Entity<SubCategory>()
            .HasOne(s => s.Category).WithMany(c => c.SubCategories).HasForeignKey(s => s.CategoryId);
        m.Entity<Product>()
            .HasOne(p => p.SubCategory).WithMany(s => s.Products).HasForeignKey(p => p.SubCategoryId).IsRequired(false);

        // ── Service Includes/Instructions ─────────────────────
        m.Entity<ServiceInclude>()
            .HasOne(i => i.Service).WithMany(s => s.Includes).HasForeignKey(i => i.ServiceId);
        m.Entity<ServiceInstruction>()
            .HasOne(i => i.Service).WithMany(s => s.Instructions).HasForeignKey(i => i.ServiceId);

        // ── Cart ──────────────────────────────────────────────
        m.Entity<CartItem>()
            .HasOne(c => c.User).WithMany(u => u.CartItems).HasForeignKey(c => c.UserId);
        m.Entity<CartItem>()
            .HasOne(c => c.Product).WithMany().HasForeignKey(c => c.ProductId);

        // ── Order items ───────────────────────────────────────
        m.Entity<OrderItem>()
            .HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId);
        m.Entity<OrderItem>()
            .HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId);
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(ct);
    }

    private void SetTimestamps()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
        }
    }
}
