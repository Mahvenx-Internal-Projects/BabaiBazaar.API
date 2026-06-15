using Microsoft.EntityFrameworkCore;
using BabaiBazaar.API.Models;
using System.Security.Cryptography;
using System.Text;

namespace BabaiBazaar.API.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        await SeedPlatformSettings(db);
        await SeedVendorPlans(db);
        await SeedSuperAdmin(db);
        await SeedCategories(db);
        await SeedServices(db);
        await SeedCoupons(db);
        await SeedTranslations(db);
        await SeedPincodes(db);
        await SeedBanners(db);
    }

    private static async Task SeedPlatformSettings(AppDbContext db)
    {
        if (await db.PlatformSettings.AnyAsync()) return;
        db.PlatformSettings.AddRange(
            new PlatformSetting { Key="delivery_fee_base",    Value="30",   Group="orders",    Description="Base delivery fee in rupees", Type="number" },
            new PlatformSetting { Key="delivery_fee_per_km",  Value="5",    Group="orders",    Description="Extra per km beyond 3km", Type="number" },
            new PlatformSetting { Key="free_delivery_above",  Value="299",  Group="orders",    Description="Free delivery on orders above this amount", Type="number" },
            new PlatformSetting { Key="max_delivery_radius",  Value="10",   Group="orders",    Description="Max delivery radius in km", Type="number" },
            new PlatformSetting { Key="service_platform_fee", Value="20",   Group="services",  Description="Platform fee % on service bookings", Type="number" },
            new PlatformSetting { Key="travel_allowance_min", Value="30",   Group="services",  Description="Min travel allowance per job in Rs", Type="number" },
            new PlatformSetting { Key="travel_allowance_max", Value="80",   Group="services",  Description="Max travel allowance per job in Rs", Type="number" },
            new PlatformSetting { Key="sp_payout_pct",        Value="80",   Group="services",  Description="% of booking value paid to service person", Type="number" },
            new PlatformSetting { Key="app_maintenance",      Value="false",Group="system",    Description="Show maintenance mode on app", Type="toggle" },
            new PlatformSetting { Key="orders_enabled",       Value="true", Group="system",    Description="Enable order placement", Type="toggle" },
            new PlatformSetting { Key="services_enabled",     Value="true", Group="system",    Description="Enable service bookings", Type="toggle" },
            new PlatformSetting { Key="otp_expiry_minutes",   Value="10",   Group="auth",      Description="OTP expiry in minutes", Type="number" },
            new PlatformSetting { Key="payout_min_balance",   Value="500",  Group="payouts",   Description="Minimum wallet balance for payout in Rs", Type="number" },
            new PlatformSetting { Key="loyalty_points_per_order", Value="10", Group="loyalty", Description="Loyalty points per Rs 100 spent", Type="number" }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedVendorPlans(AppDbContext db)
    {
        if (await db.VendorPlans.AnyAsync()) return;
        db.VendorPlans.AddRange(
            new VendorPlan { Name="FREE",       Code="FREE",       Price=0,    CommissionPct=18, MaxProducts=50,      MaxOrders=100,  MaxDeliveryBoys=1, CanCreateOffers=false, CanCreatePromoCodes=false, FeaturedListing=false, AnalyticsDashboard=false, Color="#9090A8", Emoji="📦", Description="Get started for free. Zero monthly fee.", SortOrder=1 },
            new VendorPlan { Name="BASIC",      Code="BASIC",      Price=499,  CommissionPct=15, MaxProducts=200,     MaxOrders=500,  MaxDeliveryBoys=3, CanCreateOffers=true,  CanCreatePromoCodes=false, FeaturedListing=false, AnalyticsDashboard=false, Color="#1A56DB", Emoji="🚀", Description="For growing stores. Lower commission.", SortOrder=2 },
            new VendorPlan { Name="PRO",        Code="PRO",        Price=999,  CommissionPct=10, MaxProducts=1000,    MaxOrders=2000, MaxDeliveryBoys=10, CanCreateOffers=true, CanCreatePromoCodes=true,  FeaturedListing=false, AnalyticsDashboard=true,  Color="#E8420A", Emoji="⭐", Description="Best for established vendors.", SortOrder=3, IsFeatured=true },
            new VendorPlan { Name="ENTERPRISE", Code="ENTERPRISE", Price=2499, CommissionPct=8,  MaxProducts=99999,  MaxOrders=99999,MaxDeliveryBoys=99999,CanCreateOffers=true,CanCreatePromoCodes=true,  FeaturedListing=true,  AnalyticsDashboard=true,  Color="#534AB7", Emoji="🏆", Description="Unlimited everything. Priority support.", SortOrder=4 }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedSuperAdmin(AppDbContext db)
    {
        if (await db.User.AnyAsync(u => u.Role == "SuperAdmin")) return;

        var admin = new User
        {
            Phone         = "9441363687",
            Name          = "Super Admin — Babai Bazaar",
            Role          = "SuperAdmin",
            PhoneVerified = true,
            Username      = "superadmin",
            PasswordHash  = HashPassword("Admin@123"),  // CHANGE THIS
            Language      = "en",
        };
        db.User.Add(admin);
        await db.SaveChangesAsync();

        db.AdminUsers.Add(new AdminUser
        {
            UserId      = admin.Id,
            Role        = "SuperAdmin",
            IsSuperAdmin = true,
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedCategories(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var cats = new[]
        {
            new Category { NameEn="Vegetables & Fruits", NameTe="కూరగాయలు మరియు పండ్లు", Emoji="🥬", Type="grocery", ColorBg="#E6F9EE", ColorText="#1E9E4F", SortOrder=1 },
            new Category { NameEn="Dairy & Eggs",        NameTe="పాల ఉత్పత్తులు మరియు గుడ్లు", Emoji="🥛", Type="grocery", ColorBg="#EEF4FF", ColorText="#1A56DB", SortOrder=2 },
            new Category { NameEn="Staples & Grains",    NameTe="నిత్యావసరాలు",               Emoji="🌾", Type="grocery", ColorBg="#FAEEDA", ColorText="#854F0B", SortOrder=3 },
            new Category { NameEn="Snacks & Beverages",  NameTe="స్నాక్స్ మరియు పానీయాలు",  Emoji="🍿", Type="grocery", ColorBg="#FFF0EB", ColorText="#E8420A", SortOrder=4 },
            new Category { NameEn="Cleaning",            NameTe="శుభ్రపరచడం",                  Emoji="🧹", Type="service", ColorBg="#F5F3FF", ColorText="#534AB7", SortOrder=5 },
            new Category { NameEn="Electrician",         NameTe="విద్యుత్ పని",                Emoji="⚡", Type="service", ColorBg="#FFF8E1", ColorText="#854F0B", SortOrder=6 },
            new Category { NameEn="Plumbing",            NameTe="పైప్ లైన్ పని",              Emoji="🚰", Type="service", ColorBg="#EEF4FF", ColorText="#1A56DB", SortOrder=7 },
            new Category { NameEn="Beauty & Wellness",   NameTe="అందం మరియు ఆరోగ్యం",        Emoji="💆", Type="service", ColorBg="#FFF0F8", ColorText="#BE185D", SortOrder=8 },
        };
        db.Categories.AddRange(cats);
        await db.SaveChangesAsync();

        // Sub-categories for Vegetables
        var vegCat = cats[0];
        db.SubCategories.AddRange(
            new SubCategory { CategoryId=vegCat.Id, NameEn="Leafy Vegetables", NameTe="ఆకు కూరలు", Emoji="🥬", SortOrder=1 },
            new SubCategory { CategoryId=vegCat.Id, NameEn="Root Vegetables",  NameTe="దుంప కూరలు", Emoji="🥕", SortOrder=2 },
            new SubCategory { CategoryId=vegCat.Id, NameEn="Fruits",           NameTe="పండ్లు",     Emoji="🍎", SortOrder=3 },
            new SubCategory { CategoryId=vegCat.Id, NameEn="Exotic Vegetables",NameTe="ఎగ్జోటిక్ కూరలు", Emoji="🫑", SortOrder=4 }
        );

        var dairyCat = cats[1];
        db.SubCategories.AddRange(
            new SubCategory { CategoryId=dairyCat.Id, NameEn="Milk",    NameTe="పాలు",    Emoji="🥛", SortOrder=1 },
            new SubCategory { CategoryId=dairyCat.Id, NameEn="Curd",    NameTe="పెరుగు", Emoji="🍶", SortOrder=2 },
            new SubCategory { CategoryId=dairyCat.Id, NameEn="Butter & Ghee", NameTe="నెయ్యి", Emoji="🧈", SortOrder=3 },
            new SubCategory { CategoryId=dairyCat.Id, NameEn="Eggs",    NameTe="గుడ్లు", Emoji="🥚", SortOrder=4 }
        );
        await db.SaveChangesAsync();

        // Seed sample products
        var leafy = await db.SubCategories.FirstAsync(s => s.NameEn == "Leafy Vegetables");
        db.Products.AddRange(
            new Product { NameEn="Farm Fresh Spinach", NameTe="తాజా పాలకూర", SubCategoryId=leafy.Id, Emoji="🥬", Price=22, Mrp=30, Stock=100, Unit="500g", TagEn="Fresh", TagTe="తాజా" },
            new Product { NameEn="Fenugreek Leaves",   NameTe="మెంతి ఆకులు",  SubCategoryId=leafy.Id, Emoji="🌿", Price=15, Mrp=20, Stock=80,  Unit="250g", TagEn="Organic", TagTe="సేంద్రీయ" },
            new Product { NameEn="Coriander Bunch",    NameTe="కొత్తిమీర",    SubCategoryId=leafy.Id, Emoji="🌱", Price=10, Mrp=15, Stock=60,  Unit="bunch" }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedServices(AppDbContext db)
    {
        if (await db.Services.AnyAsync()) return;
        db.Services.AddRange(
            new Service { NameEn="Full Home Deep Clean",   NameTe="పూర్తి ఇల్లు శుభ్రపరచడం",  Emoji="🏠", Category="Cleaning",    Price=999,  Mrp=1399, DurationLabel="4–5 hrs",  Description="Living room, all bedrooms, bathrooms, kitchen exterior, mopping and dusting." },
            new Service { NameEn="Bathroom Deep Clean",    NameTe="బాత్రూమ్ శుభ్రపరచడం",      Emoji="🚿", Category="Cleaning",    Price=299,  Mrp=499,  DurationLabel="1–2 hrs",  Description="Complete bathroom scrubbing, tiles, toilet, taps, floor." },
            new Service { NameEn="Kitchen Deep Clean",     NameTe="వంటగది శుభ్రపరచడం",         Emoji="🍳", Category="Cleaning",    Price=499,  Mrp=699,  DurationLabel="2–3 hrs",  Description="Kitchen cabinets, chimney, tiles, sink, counter tops." },
            new Service { NameEn="Fan & Light Fitting",    NameTe="ఫాన్ & లైట్ అమర్చడం",     Emoji="💡", Category="Electrician", Price=199,  Mrp=299,  DurationLabel="30–45 min",Description="Installation of ceiling fan or light fixture." },
            new Service { NameEn="MCB / Fuse Box Repair",  NameTe="MCB రిపేర్",                Emoji="⚡", Category="Electrician", Price=299,  Mrp=449,  DurationLabel="1 hr",     Description="MCB reset, fuse repair, switchboard work." },
            new Service { NameEn="Tap / Faucet Repair",    NameTe="నల్లా రిపేర్",              Emoji="🚰", Category="Plumbing",    Price=149,  Mrp=249,  DurationLabel="30 min",   Description="Leaky tap repair, washer replacement, faucet fitting." },
            new Service { NameEn="Drainage Unclogging",    NameTe="డ్రైనేజ్ పరిష్కారం",        Emoji="🔧", Category="Plumbing",    Price=299,  Mrp=449,  DurationLabel="1–2 hrs",  Description="Unclogging bathroom, kitchen drains using professional tools." },
            new Service { NameEn="Ladies Haircut at Home", NameTe="ఇంట్లో హెయిర్‌కట్",        Emoji="✂️", Category="Beauty",      Price=349,  Mrp=499,  DurationLabel="45 min",   Description="Professional haircut by trained female beautician." }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedCoupons(AppDbContext db)
    {
        if (await db.Coupons.AnyAsync()) return;
        db.Coupons.AddRange(
            new Coupon { Code="BABAI1",   TitleEn="Welcome 50% Off",     TitleTe="స్వాగత 50% తగ్గింపు",    CartType="ALL",     MinOrder=199, DiscountType="percent", DiscountValue=50, MaxDiscount=150, ValidFrom=DateTime.UtcNow, ValidTill=DateTime.UtcNow.AddYears(5), UsageLimit=0, PerUserLimit=1, IsFirstOrderOnly=true,  IsPublic=true, Color="#E8420A", DescEn="50% off on your first order. Max Rs.150." },
            new Coupon { Code="FARM40",   TitleEn="Fresh Farms 40% Off", TitleTe="తాజా వ్యవసాయం 40% తగ్గింపు", CartType="GROCERY", MinOrder=149, DiscountType="percent", DiscountValue=40, MaxDiscount=80,  ValidFrom=DateTime.UtcNow, ValidTill=DateTime.UtcNow.AddMonths(6), UsageLimit=500, PerUserLimit=2, IsFirstOrderOnly=false, IsPublic=true, Color="#1E9E4F", DescEn="40% off on grocery orders above Rs.149." },
            new Coupon { Code="HOME200",  TitleEn="Service Rs.200 Off",  TitleTe="సర్వీస్ Rs.200 తగ్గింపు", CartType="SERVICE", MinOrder=999, DiscountType="flat",    DiscountValue=200, MaxDiscount=200, ValidFrom=DateTime.UtcNow, ValidTill=DateTime.UtcNow.AddMonths(3), UsageLimit=200, PerUserLimit=1, IsFirstOrderOnly=false, IsPublic=true, Color="#534AB7", DescEn="Rs.200 off on service bookings above Rs.999." },
            new Coupon { Code="FREEDEL",  TitleEn="Free Delivery",       TitleTe="ఉచిత డెలివరీ",           CartType="GROCERY", MinOrder=299, DiscountType="delivery",DiscountValue=0,   MaxDiscount=0,   ValidFrom=DateTime.UtcNow, ValidTill=DateTime.UtcNow.AddMonths(12),UsageLimit=0,   PerUserLimit=3, IsFirstOrderOnly=false, IsPublic=true, Color="#F59E0B", DescEn="Free delivery on grocery orders above Rs.299." },
            new Coupon { Code="CLEANFEE", TitleEn="Cleaning Service Fee Waived",TitleTe="క్లీనింగ్ ఫీజు మాఫీ",CartType="SERVICE",MinOrder=499, DiscountType="free_service_fee",DiscountValue=100,MaxDiscount=100,ValidFrom=DateTime.UtcNow,ValidTill=DateTime.UtcNow.AddMonths(1),UsageLimit=100,PerUserLimit=1,IsFirstOrderOnly=false,IsPublic=true, Color="#1A56DB",DescEn="Service fee waived on cleaning bookings." }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedTranslations(AppDbContext db)
    {
        if (await db.Translations.AnyAsync()) return;
        var trans = new[] {
            ("common.save",           "common", "Save",              "సేవ్ చేయి"),
            ("common.cancel",         "common", "Cancel",            "రద్దు చేయి"),
            ("common.search",         "common", "Search",            "వెతుకు"),
            ("common.loading",        "common", "Loading...",        "లోడ్ అవుతోంది..."),
            ("common.error",          "common", "Something went wrong","ఏదో తప్పు జరిగింది"),
            ("auth.login",            "auth",   "Login",             "లాగిన్"),
            ("auth.send_otp",         "auth",   "Send OTP",          "OTP పంపు"),
            ("auth.verify_otp",       "auth",   "Verify OTP",        "OTP ధృవీకరించు"),
            ("auth.enter_phone",      "auth",   "Enter mobile number","మొబైల్ నంబర్ నమోదు చేయండి"),
            ("home.welcome",          "home",   "Welcome to Babai Bazaar","Babai Bazaar కి స్వాగతం"),
            ("home.groceries",        "home",   "Groceries",         "కిరాణా సామాగ్రి"),
            ("home.services",         "home",   "Home Services",     "ఇంటి సేవలు"),
            ("cart.empty",            "cart",   "Your cart is empty","మీ కార్ట్ ఖాళీగా ఉంది"),
            ("cart.add_items",        "cart",   "Add items to cart", "కార్ట్ కి చేర్చండి"),
            ("cart.checkout",         "cart",   "Checkout",          "చెక్అవుట్"),
            ("order.placed",          "status", "Order placed!",     "ఆర్డర్ పెట్టారు!"),
            ("order.confirmed",       "status", "Order confirmed",   "ఆర్డర్ నిర్ధారించబడింది"),
            ("order.out_delivery",    "status", "Out for delivery",  "డెలివరీకి బయలుదేరింది"),
            ("order.delivered",       "status", "Delivered!",        "డెలివరీ అయింది!"),
            ("booking.otp_required",  "booking","Share OTP with professional","OTP ని ప్రొఫెషనల్ తో పంచుకోండి"),
            ("booking.started",       "booking","Service started",   "సేవ మొదలైంది"),
            ("booking.completed",     "booking","Service completed!" ,"సేవ పూర్తయింది!"),
            ("payment.cod",           "payment","Cash on Delivery",  "నగదు అందిన తర్వాత"),
            ("payment.online",        "payment","Pay Online",        "ఆన్‌లైన్ చెల్లించండి"),
            ("payment.success",       "payment","Payment successful!","చెల్లింపు విజయవంతమైంది!"),
        };
        db.Translations.AddRange(trans.Select(t => new Translation { Key=t.Item1, Group=t.Item2, En=t.Item3, Te=t.Item4 }));
        await db.SaveChangesAsync();
    }

    private static async Task SeedPincodes(AppDbContext db)
    {
        if (await db.Pincodes.AnyAsync()) return;
        var pins = new[] {
            ("500072","KPHB",            "Hyderabad",15),
            ("500085","Kukatpally",      "Hyderabad",18),
            ("500046","Jubilee Hills",   "Hyderabad",20),
            ("500034","Banjara Hills",   "Hyderabad",20),
            ("500018","Madhapur",        "Hyderabad",22),
            ("500081","Gachibowli",      "Hyderabad",25),
            ("500050","Ameerpet",        "Hyderabad",18),
            ("500038","Begumpet",        "Hyderabad",20),
            ("500036","Punjagutta",      "Hyderabad",18),
            ("500016","Secunderabad",    "Hyderabad",25),
            ("500011","Abids",           "Hyderabad",25),
            ("500001","Nampally",        "Hyderabad",28),
            ("500007","Himayatnagar",    "Hyderabad",22),
            ("500029","Mehdipatnam",     "Hyderabad",25),
            ("500068","Kondapur",        "Hyderabad",22),
        };
        db.Pincodes.AddRange(pins.Select(p => new Pincode { Pincode_=p.Item1, Area=p.Item2, City=p.Item3, State="Telangana", DeliveryEta=p.Item4 }));
        await db.SaveChangesAsync();
    }

    private static async Task SeedBanners(AppDbContext db)
    {
        if (await db.Banners.AnyAsync()) return;
        db.Banners.AddRange(
            new Banner { TitleEn="Welcome to Babai Bazaar",   TitleTe="Babai Bazaar కి స్వాగతం",    ImageUrl="/banners/welcome.jpg",    Type="HOME",    Color="#E8420A", ValidTill=DateTime.UtcNow.AddMonths(12) },
            new Banner { TitleEn="Fresh Vegetables Daily",    TitleTe="రోజూ తాజా కూరగాయలు",        ImageUrl="/banners/veggies.jpg",    Type="GROCERY", Color="#1E9E4F", ValidTill=DateTime.UtcNow.AddMonths(6)  },
            new Banner { TitleEn="Book Home Cleaning Today",  TitleTe="ఇంటి శుభ్రత ఇప్పుడే",       ImageUrl="/banners/cleaning.jpg",   Type="SERVICE", Color="#534AB7", ValidTill=DateTime.UtcNow.AddMonths(6)  }
        );
        await db.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt,
            iterations: 100_000, HashAlgorithmName.SHA256, outputLength: 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }
}
