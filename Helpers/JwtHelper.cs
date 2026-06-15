using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BabaiBazaar.API.Models;

namespace BabaiBazaar.API.Helpers;

// ── JWT ───────────────────────────────────────────────────────
public class JwtHelper
{
    private readonly IConfiguration _cfg;
    public JwtHelper(IConfiguration cfg) => _cfg = cfg;

    public string GenerateToken(User user)
    {
        var secret = _cfg["Jwt:Secret"]!;
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("uid",               user.Id.ToString()),
            new Claim("phone",             user.Phone ?? ""),
            new Claim("name",              user.Name  ?? ""),
            new Claim("lang",              user.Language ?? "en"),
            new Claim("role",              user.Role),
            new Claim(ClaimTypes.Role,     user.Role),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        var days  = double.Parse(_cfg["Jwt:ExpiryDays"] ?? "30");
        var token = new JwtSecurityToken(
            _cfg["Jwt:Issuer"], _cfg["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddDays(days),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), salt,
            iterations: 100_000, HashAlgorithmName.SHA256, outputLength: 32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string stored)
    {
        try
        {
            var parts    = stored.Split(':');
            if (parts.Length != 2) return false;
            var salt     = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual   = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password), salt,
                iterations: 100_000, HashAlgorithmName.SHA256, outputLength: 32);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }
}

// ── UPLOAD ────────────────────────────────────────────────────
public class UploadHelper
{
    private readonly IConfiguration _cfg;
    private readonly IWebHostEnvironment _env;

    public UploadHelper(IConfiguration cfg, IWebHostEnvironment env)
    { _cfg = cfg; _env = env; }

    public async Task<string> SaveLocalAsync(IFormFile file, string folder)
    {
        var uploads = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", folder);
        Directory.CreateDirectory(uploads);
        var ext  = Path.GetExtension(file.FileName).ToLowerInvariant();
        var name = $"{Guid.NewGuid()}{ext}";
        var path = Path.Combine(uploads, name);
        await using var fs = File.Create(path);
        await file.CopyToAsync(fs);
        var baseUrl = _cfg["AppSettings:BaseUrl"] ?? "";
        return $"{baseUrl}/uploads/{folder}/{name}";
    }

    // AWS S3 upload — wire up AWSSDK.S3 if needed
    public async Task<string> SaveS3Async(IFormFile file, string folder)
    {
        // Fallback to local in development
        if (_cfg["AppSettings:DevMode"] == "true")
            return await SaveLocalAsync(file, folder);

        // Production: upload to S3
        var bucketName  = _cfg["AWS:BucketName"]!;
        var region      = _cfg["AWS:Region"]!;
        var ext  = Path.GetExtension(file.FileName).ToLowerInvariant();
        var key  = $"{folder}/{Guid.NewGuid()}{ext}";

        // var s3 = new AmazonS3Client(...);
        // await s3.PutObjectAsync(new PutObjectRequest { BucketName=bucketName, Key=key, InputStream=file.OpenReadStream(), ContentType=file.ContentType });

        return $"https://{bucketName}.s3.{region}.amazonaws.com/{key}";
    }
}
