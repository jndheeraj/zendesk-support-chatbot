using KyrisCBL.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KyrisCBL.Data;

public class AppDbContext : IdentityDbContext<ChatUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<ChatUser> Users { get; set; } = null!;
    public DbSet<ChatArchive> ChatArchives => Set<ChatArchive>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Map User table to your custom schema/name
        builder.Entity<ChatUser>().ToTable("user", "user");

        // Map default Identity columns to your DB column names
        builder.Entity<ChatUser>(b =>
        {
            b.Property(u => u.Id).HasColumnName("user_id");
            b.Property(u => u.UserName).HasColumnName("UserName");
            b.Property(u => u.NormalizedUserName).HasColumnName("NormalizedUserName");
            b.Property(u => u.Email).HasColumnName("email_address").IsRequired();
            b.Property(u => u.NormalizedEmail).HasColumnName("NormalizedEmail");
            b.Property(u => u.EmailConfirmed).HasColumnName("EmailConfirmed");
            b.Property(u => u.PasswordHash).HasColumnName("PasswordHash");
            b.Property(u => u.SecurityStamp).HasColumnName("SecurityStamp");
            b.Property(u => u.ConcurrencyStamp).HasColumnName("ConcurrencyStamp");
            b.Property(u => u.PhoneNumber).HasColumnName("phone_number");
            b.Property(u => u.PhoneNumberConfirmed).HasColumnName("PhoneNumberConfirmed");
            b.Property(u => u.TwoFactorEnabled).HasColumnName("TwoFactorEnabled");
            b.Property(u => u.LockoutEnd).HasColumnName("LockoutEnd");
            b.Property(u => u.LockoutEnabled).HasColumnName("LockoutEnabled");
            b.Property(u => u.AccessFailedCount).HasColumnName("AccessFailedCount");

            // Custom profile columns
            b.Property(u => u.FirstName).HasColumnName("first_name");
            b.Property(u => u.LastName).HasColumnName("last_name");
        });

        // Map Identity tables to custom schema
        builder.Entity<IdentityRole<int>>()
            .ToTable("role", "user");
        builder.Entity<IdentityUserRole<int>>()
            .ToTable("user_roles", "user");
        builder.Entity<IdentityUserClaim<int>>()
            .ToTable("user_claims", "user");
        builder.Entity<IdentityUserLogin<int>>()
            .ToTable("user_logins", "user");
        builder.Entity<IdentityRoleClaim<int>>()
            .ToTable("role_claims", "user");
        builder.Entity<IdentityUserToken<int>>()
            .ToTable("user_tokens", "user");

        // ----- ChatArchive mapping (schema: cb, table: chat_archives) -----
        builder.Entity<ChatArchive>(e =>
        {
            e.ToTable("chat_archives", "cb");

            e.HasKey(x => x.Id);

            e.Property(x => x.Tenant).HasMaxLength(64).IsRequired();
            e.Property(x => x.UserKey).HasMaxLength(320).IsRequired();
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.Channel).HasMaxLength(64).HasDefaultValue("web-widget").IsRequired();
            e.Property(x => x.Summary).HasMaxLength(512).HasDefaultValue("").IsRequired();

            // NVARCHAR(MAX) for the JSON payload
            e.Property(x => x.Json).HasColumnType("nvarchar(max)").IsRequired();

            // Default CreatedUtc from SQL Server
            e.Property(x => x.CreatedUtc).HasDefaultValueSql("SYSUTCDATETIME()");

            // Index to match your DDL
            e.HasIndex(x => new { x.Tenant, x.UserKey, x.CreatedUtc });

            // Matches your CHECK constraint (SQL Server)
            e.HasCheckConstraint("CK_ChatArchives_Json_IsJson", "ISJSON([Json]) > 0");
        });
    }
}
