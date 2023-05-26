using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TNRD.Zeepkist.GTR.Auth.Database.Models;

namespace TNRD.Zeepkist.GTR.Auth.Database;

public partial class GTRContext : DbContext
{
    public GTRContext()
    {
    }

    public GTRContext(DbContextOptions<GTRContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Models.Auth> Auths { get; set; }

    public virtual DbSet<User> Users { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("fuzzystrmatch")
            .HasPostgresExtension("postgis")
            .HasPostgresExtension("tiger", "postgis_tiger_geocoder")
            .HasPostgresExtension("topology", "postgis_topology");

        modelBuilder.HasDbFunction(DateTruncMethod).HasName("date_trunc");

        modelBuilder.Entity<Models.Auth>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("auth_pkey");

            entity.ToTable("auth");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccessToken)
                .HasMaxLength(255)
                .HasColumnName("access_token");
            entity.Property(e => e.AccessTokenExpiry)
                .HasMaxLength(255)
                .HasColumnName("access_token_expiry");
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
            entity.Property(e => e.DateUpdated).HasColumnName("date_updated");
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(255)
                .HasColumnName("refresh_token");
            entity.Property(e => e.RefreshTokenExpiry)
                .HasMaxLength(255)
                .HasColumnName("refresh_token_expiry");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.User).HasColumnName("user");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.Auths)
                .HasForeignKey(d => d.User)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("auth_user_foreign");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Banned)
                .HasDefaultValueSql("false")
                .HasColumnName("banned");
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
            entity.Property(e => e.DateUpdated).HasColumnName("date_updated");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.SteamId)
                .HasMaxLength(255)
                .HasColumnName("steam_id");
            entity.Property(e => e.SteamName)
                .HasMaxLength(255)
                .HasColumnName("steam_name");
            entity.Property(e => e.DiscordId)
                .HasMaxLength(255)
                .HasColumnName("discord_id");
            entity.Property(e => e.WorldRecords).HasColumnName("world_records");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    private void UpdateDateUpdated()
    {
        IEnumerable<object> entries = ChangeTracker.Entries()
            .Where(x => x.State == EntityState.Modified)
            .Select(x => x.Entity);

        DateTime stamp = DateTime.UtcNow;

        foreach (object entry in entries)
        {
            if (entry is IModel model)
            {
                model.DateUpdated = stamp;
            }
        }
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        UpdateDateUpdated();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateDateUpdated();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        UpdateDateUpdated();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = new()
    )
    {
        UpdateDateUpdated();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    private static readonly MethodInfo DateTruncMethod =
        typeof(GTRContext).GetRuntimeMethod(nameof(DateTrunc), new[] { typeof(string), typeof(DateTime) })!;

    public static DateTime DateTrunc(string field, DateTime source)
        => throw new NotSupportedException();
}
