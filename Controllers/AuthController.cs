using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using BabaiBazaar.API.Data;
using BabaiBazaar.API.DTOs;
using BabaiBazaar.API.Helpers;
using BabaiBazaar.API.Models;

namespace BabaiBazaar.API.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly JwtHelper _jwt;

    public AuthController(AppDbContext db, IConfiguration cfg, JwtHelper jwt)
    { _db = db; _cfg = cfg; _jwt = jwt; }

    private int Uid => int.Parse(User.FindFirst("uid")?.Value ?? "0");

    // ── OTP LOGIN (Mobile app) ────────────────────────────────

    // POST /api/auth/send-otp
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Phone))
        {
            return BadRequest(new { message = "Phone number is required" });
        }

        var phone = req.Phone.Replace("+91", "").Trim();

        // Validate Indian mobile number
        if (!Regex.IsMatch(phone, @"^[6-9]\d{9}$"))
        {
            return BadRequest(new
            {
                message = "Please enter a valid 10-digit mobile number"
            });
        }

        var otp = _cfg["AppSettings:DevMode"] == "true"
            ? "1234"
            : new Random().Next(1000, 9999).ToString();

        _db.OtpRecords.Add(new OtpRecord
        {
            Phone = phone,
            Code = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "OTP sent successfully",
            otp = _cfg["AppSettings:DevMode"] == "true" ? otp : null
        });
    }

    // POST /api/auth/verify-otp
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest req)
    {
        var phone = req.Phone.Replace("+91", "").Trim();

        var record = await _db.OtpRecords
            .Where(o => o.Phone == phone && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (record == null || record.Code != req.Otp)
            return Unauthorized(new { message = "Invalid or expired OTP" });

        record.IsUsed = true;

        var user = await _db.User.FirstOrDefaultAsync(u => u.Phone == phone);
        bool isNew = user == null;
        if (isNew)
        {
            user = new User { Phone = phone, PhoneVerified = true, Name = req.Name ?? "" };
            _db.User.Add(user);
        }
        else
        {
            user!.PhoneVerified = true;
            user.LastLogin = DateTime.UtcNow;
        }

        await SyncUserRole(user!);
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user!);
        return Ok(new { token, user = MapUser(user!), isNew });
    }

    // ── USERNAME + PASSWORD LOGIN (Portal) ────────────────────

    // POST /api/auth/portal-login
    [HttpPost("portal-login")]
    public async Task<IActionResult> PortalLogin([FromBody] PortalLoginRequest req)
    {
        var username = req.Username.Trim().ToLower();
        var user = await _db.User.FirstOrDefaultAsync(u =>
            (u.Username != null && u.Username.ToLower() == username) ||
            (u.Email    != null && u.Email.ToLower()    == username) ||
            u.Phone == username
        );

        if (user == null)
            return Unauthorized(new { message = "Invalid username or password" });

        if (string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized(new { message = "Portal credentials not set for this account. Contact admin.", hint = "POST /api/auth/set-password" });

        if (!JwtHelper.VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password" });

        if (user.Role == "Customer")
            return StatusCode(403, new { message = "Access denied. Only Admin/Vendor accounts can use the portal." });

        await SyncUserRole(user);
        user.LastLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user);
        return Ok(new { token, user = MapUser(user), message = $"Welcome back, {user.Name}!" });
    }

    // POST /api/auth/set-password   [Admin only]
    [HttpPost("set-password")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest req)
    {
        if (req.Password.Length < 8)
            return BadRequest(new { message = "Password must be at least 8 characters" });

        var existing = await _db.User.FirstOrDefaultAsync(u =>
            u.Username != null &&
            u.Username.ToLower() == req.Username.Trim().ToLower() &&
            u.Id != req.UserId);
        if (existing != null)
            return Conflict(new { message = $"Username '{req.Username}' is already taken" });

        var user = await _db.User.FindAsync(req.UserId);
        if (user == null) return NotFound(new { message = "User not found" });

        user.Username     = req.Username.Trim().ToLower();
        user.PasswordHash = JwtHelper.HashPassword(req.Password);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Credentials set", username = user.Username, userId = user.Id, role = user.Role });
    }

    // POST /api/auth/change-password
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var user = await _db.User.FindAsync(Uid);
        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(user.PasswordHash) && !JwtHelper.VerifyPassword(req.CurrentPassword, user.PasswordHash))
            return Unauthorized(new { message = "Current password is incorrect" });

        if (req.NewPassword.Length < 8)
            return BadRequest(new { message = "Password must be at least 8 characters" });

        user.PasswordHash = JwtHelper.HashPassword(req.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password changed successfully" });
    }

    // POST /api/auth/google
    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth([FromBody] GoogleAuthRequest req)
    {
        try
        {
            var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(req.IdToken,
                new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
                { Audience = new[] { _cfg["Google:ClientId"] } });

            var user = await _db.User
                .FirstOrDefaultAsync(u => u.GoogleId == payload.Subject || u.Email == payload.Email);
            bool isNew = user == null;
            if (isNew)
            {
                user = new User { GoogleId = payload.Subject, Email = payload.Email, Name = payload.Name, ProfileImage = payload.Picture, PhoneVerified = false };
                _db.User.Add(user);
            }
            else { user!.LastLogin = DateTime.UtcNow; }

            await SyncUserRole(user!);
            await _db.SaveChangesAsync();

            return Ok(new { token = _jwt.GenerateToken(user!), user = MapUser(user!), isNew });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = "Invalid Google token", detail = ex.Message });
        }
    }

    // GET /api/auth/me
    [HttpGet("me"), Authorize]
    public async Task<IActionResult> Me()
    {
        var user = await _db.User.FindAsync(Uid);
        if (user == null) return NotFound();
        return Ok(MapUser(user));
    }

    // GET /api/auth/whoami  (debug)
    [HttpGet("whoami"), Authorize]
    public IActionResult WhoAmI() => Ok(new
    {
        uid    = User.FindFirst("uid")?.Value,
        role   = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
        name   = User.FindFirst("name")?.Value,
        isAdmin      = User.IsInRole("Admin"),
        isSuperAdmin = User.IsInRole("SuperAdmin"),
        isVendor     = User.IsInRole("Vendor"),
        claims = User.Claims.Select(c => new { c.Type, c.Value })
    });

    // ── HELPERS ──────────────────────────────────────────────
    private async Task SyncUserRole(User user)
    {
        var adminEntry = await _db.AdminUsers.FirstOrDefaultAsync(a => a.UserId == user.Id);
        if (adminEntry != null)
        {
            user.Role = adminEntry.IsSuperAdmin ? "SuperAdmin" : (adminEntry.Role ?? "Admin");
            return;
        }
        var vendorEntry = await _db.Vendors.FirstOrDefaultAsync(v => v.UserId == user.Id);
        if (vendorEntry != null && user.Role == "Customer") user.Role = "Vendor";
    }

    private static object MapUser(User u) => new
    {
        u.Id, u.Name, u.Phone, u.Email, u.ProfileImage,
        u.Language, u.Role, u.PhoneVerified, u.WalletBalance, u.LoyaltyPoints,
        u.Username, HasPassword = !string.IsNullOrEmpty(u.PasswordHash)
    };
}
