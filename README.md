# Babai Bazaar API — Setup Guide
**Mahvenx IT Solutions Pvt. Ltd. | babaibazaar.com**

---

## 1. Prerequisites

| Tool | Version | Download |
|------|---------|---------|
| .NET SDK | 8.0 | https://dotnet.microsoft.com/download/dotnet/8 |
| MySQL | 8.0+ | https://dev.mysql.com/downloads/ |
| Visual Studio | 2022+ | https://visualstudio.microsoft.com |

---

## 2. Project Structure

```
BabaiBazaar/
└── BabaiBazaar.API/
    ├── Controllers/
    │   ├── AuthController.cs          ← OTP + username/password login
    │   ├── CategoryProductController.cs ← Categories, Products, Services, Cart, Orders, Upload
    │   ├── CoreControllers.cs         ← Vendors, Plans, DeliveryBoys, ServicePersons, Offers, Promos, Admin, Dashboard
    │   └── OnboardingCrmController.cs ← Onboarding docs + CRM tickets + Staff targets
    ├── Data/
    │   ├── AppDbContext.cs            ← EF Core context with all relationships
    │   └── DataSeeder.cs             ← Seeds SuperAdmin, plans, categories, services, coupons
    ├── DTOs/
    │   └── Dtos.cs                   ← All request/response records
    ├── Helpers/
    │   └── JwtHelper.cs              ← JWT generation + PBKDF2 password hashing + UploadHelper
    ├── Models/
    │   ├── Entities.cs               ← User, Category, Product, Service, Order, Booking, Cart, Coupon...
    │   └── ExtendedEntities.cs       ← Vendor, DeliveryBoy, ServicePerson, CRM, Onboarding, Staff...
    ├── appsettings.json              ← Connection string, JWT, Twilio, Razorpay, AWS config
    ├── BabaiBazaar.API.csproj        ← NuGet packages
    └── Program.cs                    ← Startup — JWT auth, CORS, Swagger, EF, Serilog
```

---

## 3. Configuration (appsettings.json)

Update these values before running:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=BabaiBazor;User=root;Password=YOUR_PASSWORD;"
  },
  "Jwt": {
    "Secret": "BabaiBazaarSuperSecretKey2025MahvenxITSolutions!@#$"
  },
  "AppSettings": {
    "DevMode": "true",
    "BaseUrl": "http://204.168.159.160:8085"
  }
}
```

---

## 4. First-time Setup

### Step 1 — Create MySQL database
```sql
CREATE DATABASE BabaiBazor CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### Step 2 — Add Entity Framework migrations
```bash
cd BabaiBazaar/BabaiBazaar.API
dotnet ef migrations add InitialCreate
dotnet ef database update
```

> **If you get "No project was found" error:**
> You MUST be inside the `BabaiBazaar.API` folder, not the solution root.

### Step 3 — Restore packages and run
```bash
dotnet restore
dotnet run
```

### Step 4 — Open Swagger
```
http://localhost:8085/swagger
```

---

## 5. Deployment on Linux Server (your server at 204.168.159.160)

### Publish
```bash
dotnet publish -c Release -o /var/www/Babaibazor/publish
```

### Copy to server (from Windows/Mac)
```bash
scp -r ./publish/* root@204.168.159.160:/var/www/Babaibazor/publish/
```

### Create systemd service
```bash
cat > /etc/systemd/system/babai.service << 'EOF'
[Unit]
Description=Babai Bazaar API
After=network.target

[Service]
WorkingDirectory=/var/www/Babaibazor/publish
ExecStart=/usr/bin/dotnet BabaiBazaar.API.dll
Restart=always
RestartSec=10
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:8085

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable babai
systemctl start babai
systemctl status babai
```

### Check logs
```bash
journalctl -u babai -f
```

### Kill & restart (if port 8085 busy)
```bash
systemctl stop babai
fuser -k 8085/tcp
systemctl start babai
```

---

## 6. Default Login Credentials (seeded)

| Role | Username | Password | Portal URL |
|------|----------|----------|-----------|
| Super Admin | `superadmin` | `Admin@123` | `/superadmin/login` |
| Admin | `admin` | `Admin@123` | `/login` |

> **IMPORTANT: Change these passwords after first login!**
> Call: `POST /api/auth/change-password` with the new password.

---

## 7. Key API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|---------|-------------|
| POST | `/api/auth/send-otp` | Send OTP to mobile (customer) |
| POST | `/api/auth/verify-otp` | Verify OTP and get JWT |
| POST | `/api/auth/portal-login` | Username+password login (admin/vendor portal) |
| POST | `/api/auth/set-password` | Set portal credentials (admin only) |
| POST | `/api/auth/change-password` | Change password (authenticated) |
| GET | `/api/auth/me` | Get current user profile |

### Catalogue
| Method | Endpoint | Description |
|--------|---------|-------------|
| GET | `/api/categories?type=grocery` | List categories |
| GET | `/api/categories/{id}/sub-categories` | List sub-categories |
| GET | `/api/products?subCategoryId=1` | List products |
| GET | `/api/services?category=Cleaning` | List services |
| GET | `/api/banners?type=HOME` | List active banners |
| GET | `/api/pincode/check?pincode=500072` | Check delivery availability |

### Cart & Orders
| Method | Endpoint | Description |
|--------|---------|-------------|
| GET | `/api/cart` | Get user cart |
| POST | `/api/cart` | Add to cart |
| POST | `/api/orders` | Place order |
| GET | `/api/orders` | List orders |
| PUT | `/api/orders/{id}/status` | Update order status |

### Service Bookings
| Method | Endpoint | Description |
|--------|---------|-------------|
| POST | `/api/services/bookings` | Create booking |
| GET | `/api/services/bookings` | List bookings |
| PUT | `/api/services/bookings/{id}/status` | Update booking status |
| POST | `/api/services/bookings/{id}/verify-otp` | Verify OTP to start service |

### Promo Codes
| Method | Endpoint | Description |
|--------|---------|-------------|
| GET | `/api/promo-codes` | List active promos |
| POST | `/api/promo-codes/validate` | Validate coupon (no recording) |
| POST | `/api/promo-codes/apply` | Apply coupon (records usage) |

### Vendors
| Method | Endpoint | Description |
|--------|---------|-------------|
| POST | `/api/vendors/register` | Register vendor |
| GET | `/api/vendors` | List all vendors (admin) |
| PUT | `/api/vendors/{id}/approve` | Approve vendor |
| GET | `/api/vendors/my` | Vendor's own profile |

### Onboarding
| Method | Endpoint | Description |
|--------|---------|-------------|
| GET | `/api/onboarding/vendor/{id}/checklist` | Vendor doc checklist |
| POST | `/api/onboarding/vendor/{id}/documents` | Upload vendor document |
| PUT | `/api/onboarding/vendor/documents/{id}/status` | Approve/reject doc |
| GET | `/api/onboarding/sp/{id}/checklist` | SP doc checklist |
| POST | `/api/onboarding/sp/{id}/documents` | Upload SP document |
| GET | `/api/onboarding/agreements/templates` | T&C PDF template URLs |
| POST | `/api/onboarding/vendor/{id}/agreement` | Record signed agreement |

### CRM
| Method | Endpoint | Description |
|--------|---------|-------------|
| GET | `/api/crm/tickets` | List all tickets |
| POST | `/api/crm/tickets` | Create ticket |
| POST | `/api/crm/tickets/{id}/notes` | Add note to ticket |
| POST | `/api/crm/tickets/{id}/schedule-call` | Schedule call |
| GET | `/api/crm/staff` | List marketing/telecaller staff |
| POST | `/api/crm/staff` | Add staff member |
| POST | `/api/crm/staff/{id}/targets` | Set daily target |
| GET | `/api/crm/dashboard` | CRM overview stats |

### Dashboard
| Method | Endpoint | Description |
|--------|---------|-------------|
| GET | `/api/dashboard/admin` | Admin stats (orders, vendors, revenue) |
| GET | `/api/dashboard` | Vendor dashboard |

### Upload
| Method | Endpoint | Description |
|--------|---------|-------------|
| POST | `/api/upload/image?folder=products` | Upload image file |
| POST | `/api/upload/document?folder=vendor-docs` | Upload document |

---

## 8. Role System

| Role | Portal | Can Access |
|------|--------|-----------|
| Customer | Mobile app | Cart, orders, bookings, reviews |
| Vendor | Vendor portal `/vendor/login` | Products, orders, delivery boys, payouts |
| Admin | Admin portal `/login` | All admin features |
| SuperAdmin | `/superadmin/login` | Everything including admin users |
| Marketing | Admin portal | Onboarding, CRM, tickets |
| Telecaller | Admin portal | CRM tickets, my work dashboard |
| FieldAgent | Admin portal | Onboarding, CRM |

---

## 9. Database Migration Commands

```bash
# Always run from BabaiBazaar.API folder
cd BabaiBazaar/BabaiBazaar.API

# New migration (after entity changes)
dotnet ef migrations add YourMigrationName

# Apply to database
dotnet ef database update

# Rollback one migration
dotnet ef database update PreviousMigrationName

# Drop database (CAUTION — destroys all data)
dotnet ef database drop

# List all migrations
dotnet ef migrations list
```

---

## 10. T&C PDF Templates

Place the signed agreement PDFs here on the server:
```
/var/www/Babaibazor/publish/wwwroot/templates/vendor-tnc.pdf
/var/www/Babaibazor/publish/wwwroot/templates/sp-agreement.pdf
/var/www/Babaibazor/publish/wwwroot/templates/privacy-policy.pdf
```

These are accessible at:
```
http://204.168.159.160:8085/templates/vendor-tnc.pdf
```

---

## 11. Environment Variables (Production)

Set these on the Linux server instead of hardcoding in appsettings:
```bash
export ConnectionStrings__DefaultConnection="Server=localhost;Database=BabaiBazor;User=root;Password=YourSecurePassword;"
export Jwt__Secret="YourVeryLongSecretKey2025!"
export AppSettings__DevMode="false"
export AppSettings__BaseUrl="https://babaibazaar.com"
```

---

## 12. Support

- **Phone / WhatsApp:** 9441363687
- **Office:** Plot 52, 53, 1st Floor, B Block, KPHB 5th Phase, Hyderabad
- **Website:** babaibazaar.com
- **Company:** Mahvenx IT Solutions Pvt. Ltd.
