using System;
using System.Reflection;
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

    public virtual DbSet<Favorite> Favorites { get; set; }

    public virtual DbSet<Level> Levels { get; set; }

    public virtual DbSet<Record> Records { get; set; }

    public virtual DbSet<Upvote> Upvotes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vote> Votes { get; set; }

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

        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("favorites_pkey");

            entity.ToTable("favorites");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
            entity.Property(e => e.DateUpdated).HasColumnName("date_updated");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.User).HasColumnName("user");

            entity.HasOne(d => d.LevelNavigation).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.Level)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("favorites_level_foreign");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.Favorites)
                .HasForeignKey(d => d.User)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("favorites_user_foreign");
        });

        modelBuilder.Entity<Level>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("levels_pkey");

            entity.ToTable("levels");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Author)
                .HasMaxLength(255)
                .HasColumnName("author");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
            entity.Property(e => e.DateUpdated).HasColumnName("date_updated");
            entity.Property(e => e.IsValid)
                .HasDefaultValueSql("true")
                .HasColumnName("is_valid");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Points).HasColumnName("points");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.ThumbnailUrl)
                .HasMaxLength(2048)
                .HasColumnName("thumbnail_url");
            entity.Property(e => e.TimeAuthor).HasColumnName("time_author");
            entity.Property(e => e.TimeBronze).HasColumnName("time_bronze");
            entity.Property(e => e.TimeGold).HasColumnName("time_gold");
            entity.Property(e => e.TimeSilver).HasColumnName("time_silver");
            entity.Property(e => e.Uid)
                .HasMaxLength(255)
                .HasDefaultValueSql("NULL::character varying")
                .HasColumnName("uid");
            entity.Property(e => e.Wid)
                .HasMaxLength(255)
                .HasColumnName("wid");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Levels)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("levels_created_by_foreign");
        });

        modelBuilder.Entity<Record>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("records_pkey");

            entity.ToTable("records");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
            entity.Property(e => e.DateUpdated).HasColumnName("date_updated");
            entity.Property(e => e.GameVersion)
                .HasMaxLength(255)
                .HasColumnName("game_version");
            entity.Property(e => e.GhostUrl)
                .HasMaxLength(255)
                .HasColumnName("ghost_url");
            entity.Property(e => e.IsBest).HasColumnName("is_best");
            entity.Property(e => e.IsValid).HasColumnName("is_valid");
            entity.Property(e => e.IsWr).HasColumnName("is_wr");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.ScreenshotUrl)
                .HasMaxLength(255)
                .HasColumnName("screenshot_url");
            entity.Property(e => e.Splits)
                .HasMaxLength(1024)
                .HasColumnName("splits");
            entity.Property(e => e.Time).HasColumnName("time");
            entity.Property(e => e.User).HasColumnName("user");

            entity.HasOne(d => d.LevelNavigation).WithMany(p => p.Records)
                .HasForeignKey(d => d.Level)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("records_level_foreign");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.Records)
                .HasForeignKey(d => d.User)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("records_user_foreign");
        });

        modelBuilder.Entity<Upvote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("upvotes_pkey");

            entity.ToTable("upvotes");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
            entity.Property(e => e.DateUpdated).HasColumnName("date_updated");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.User).HasColumnName("user");

            entity.HasOne(d => d.LevelNavigation).WithMany(p => p.Upvotes)
                .HasForeignKey(d => d.Level)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("upvotes_level_foreign");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.Upvotes)
                .HasForeignKey(d => d.User)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("upvotes_user_foreign");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.Id).HasColumnName("id");
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

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("votes_pkey");

            entity.ToTable("votes");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Category).HasColumnName("category");
            entity.Property(e => e.DateCreated).HasColumnName("date_created");
            entity.Property(e => e.DateUpdated).HasColumnName("date_updated");
            entity.Property(e => e.Level).HasColumnName("level");
            entity.Property(e => e.Score).HasColumnName("score");
            entity.Property(e => e.User).HasColumnName("user");

            entity.HasOne(d => d.LevelNavigation).WithMany(p => p.Votes)
                .HasForeignKey(d => d.Level)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("votes_level_foreign");

            entity.HasOne(d => d.UserNavigation).WithMany(p => p.Votes)
                .HasForeignKey(d => d.User)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("votes_user_foreign");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    private static readonly MethodInfo DateTruncMethod =
        typeof(GTRContext).GetRuntimeMethod(nameof(DateTrunc), new[] { typeof(string), typeof(DateTime) })!;

    public static DateTime DateTrunc(string field, DateTime source)
        => throw new NotSupportedException();
}
