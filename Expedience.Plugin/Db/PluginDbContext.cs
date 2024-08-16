using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Expedience.Db.Models;

namespace Expedience.Db
{
    public class PluginDbContext : DbContext
    {
        public PluginDbContext()
        {
        }

        public PluginDbContext(DbContextOptions<PluginDbContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbFolderLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Expedience");
            Directory.CreateDirectory(dbFolderLocation);
            var dbPath = Path.Combine(dbFolderLocation, "expedience.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        public DbSet<LocalRecord> LocalRecords { get; set; }
        public DbSet<LocalUser> LocalUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LocalRecord>(cfg =>
            {
                cfg.ToTable(nameof(LocalRecord));
                cfg.HasKey(e => e.Id);

                cfg.Property(e => e.Id)
                .HasColumnType("INTEGER")
                .IsRequired()
                .ValueGeneratedOnAdd()
                .HasAnnotation("Sqlite:Autoincrement", true);

                cfg.HasIndex(e => e.CompletionDate);
            });

            modelBuilder.Entity<LocalUser>(cfg =>
            {
                cfg.ToTable(nameof(LocalUser));
                cfg.HasKey(e => new { e.WorldId, e.UserHash });
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
