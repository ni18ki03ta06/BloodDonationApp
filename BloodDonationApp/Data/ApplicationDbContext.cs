using Microsoft.EntityFrameworkCore;
using BloodDonationApp.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BloodDonationApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Donor> Donors { get; set; }
        public DbSet<BloodRequest> BloodRequests { get; set; }
        public DbSet<DonationRecord> DonationRecords { get; set; }
        public DbSet<BloodInventory> BloodInventory { get; set; }
        public DbSet<BloodCamp> BloodCamps { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<CampRegistration> CampRegistrations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<QrScanLog> QrScanLogs { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<DonorBadge> DonorBadges { get; set; }
        public DbSet<Reward> Rewards { get; set; }
        public DbSet<RedeemedReward> RedeemedRewards { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DonationRecord>()
                .HasOne(dr => dr.Donor)
                .WithMany(d => d.DonationRecords)
                .HasForeignKey(dr => dr.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(t => t.Donor)
                .WithMany(d => d.PasswordResetTokens)
                .HasForeignKey(t => t.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampRegistration>()
                .HasOne(cr => cr.Donor)
                .WithMany(d => d.CampRegistrations)
                .HasForeignKey(cr => cr.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CampRegistration>()
                .HasOne(cr => cr.Camp)
                .WithMany()
                .HasForeignKey(cr => cr.CampId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BloodRequest>()
                .HasOne(br => br.FulfilledByDonor)
                .WithMany()
                .HasForeignKey(br => br.FulfilledBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Donor)
                .WithMany()
                .HasForeignKey(f => f.DonorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Donor)
                .WithMany(d => d.Notifications)
                .HasForeignKey(n => n.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QrScanLog>()
                .HasOne(log => log.Donor)
                .WithMany()
                .HasForeignKey(log => log.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Donor)
                .WithMany()
                .HasForeignKey(a => a.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DonorBadge>()
                .HasOne(db => db.Donor)
                .WithMany(d => d.DonorBadges)
                .HasForeignKey(db => db.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DonorBadge>()
                .HasOne(db => db.Badge)
                .WithMany()
                .HasForeignKey(db => db.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RedeemedReward>()
                .HasOne(rr => rr.Donor)
                .WithMany(d => d.RedeemedRewards)
                .HasForeignKey(rr => rr.DonorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RedeemedReward>()
                .HasOne(rr => rr.Reward)
                .WithMany()
                .HasForeignKey(rr => rr.RewardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BloodRequest>()
                .Property(e => e.UrgencyLevel)
                .HasConversion<string>();
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Donor && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var donor = (Donor)entry.Entity;
                var now = DateTime.Now;
                if (entry.State == EntityState.Added)
                {
                    donor.CreatedAt = now;
                }
                donor.UpdatedAt = now;
            }
        }
    }
}
