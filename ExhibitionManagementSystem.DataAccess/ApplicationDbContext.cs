using ExhibitionManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;
using System.Linq.Expressions;
using System.Text;

namespace ExhibitionManagementSystem.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }






        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantSubscription> TenantSubscriptions { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Venue> Venues { get; set; }
        public DbSet<Hall> Halls { get; set; }
        public DbSet<Booth> Booths { get; set; }
        public DbSet<BoothMerge> BoothMerges { get; set; }
        public DbSet<BoothMergeItem> BoothMergeItems { get; set; }
        public DbSet<Exhibition> Exhibitions { get; set; }
        public DbSet<ExhibitionSchedule> ExhibitionSchedules { get; set; }
        public DbSet<ScheduleRegistration> ScheduleRegistrations { get; set; }
        public DbSet<Exhibitor> Exhibitors { get; set; }
        public DbSet<BoothReservation> BoothReservations { get; set; }
        public DbSet<BoothStaff> BoothStaffs { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<BoothPriceRule> BoothPriceRules { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServicePriceRule> ServicePriceRules { get; set; }
        public DbSet<PricingPackage> PricingPackages { get; set; }
        public DbSet<PackageService> PackageServices { get; set; }
        public DbSet<ReservationService> ReservationServices { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<FinancialReport> FinancialReports { get; set; }
        public DbSet<Visitor> Visitors { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketScan> TicketScans { get; set; }
        public DbSet<VisitorRating> VisitorRatings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. تحويل الـ Enums إلى نصوص (nvarchar) كما هو موضح في التصميم
            builder.Entity<Exhibition>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<BoothPriceRule>().Property(e => e.BoothType).HasConversion<string>().HasMaxLength(50);
            builder.Entity<BoothPriceRule>().Property(e => e.ExhibitorCategory).HasConversion<string>().HasMaxLength(20);
            builder.Entity<BoothReservation>().Property(e => e.ExhibitorCategory).HasConversion<string>().HasMaxLength(20);
            builder.Entity<BoothReservation>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<BoothReservation>().Property(e => e.BoothTypeSelected).HasConversion<string>().HasMaxLength(50);
            builder.Entity<Invoice>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<Payment>().Property(e => e.Method).HasConversion<string>().HasMaxLength(50);
            builder.Entity<Payment>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<TicketScan>().Property(e => e.Direction).HasConversion<string>().HasMaxLength(10);
            builder.Entity<Booth>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<Booth>().Property(e => e.ShapeType).HasConversion<string>().HasMaxLength(20);
            builder.Entity<Booth>().Property(e => e.ShapeType).HasDefaultValue(BoothShapeType.Rect);
            builder.Entity<ExhibitionSchedule>().Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
            builder.Entity<Exhibitor>().Property(e => e.ExhibitorCategory).HasConversion<string>().HasMaxLength(20);

            // New Enums Conversions
            builder.Entity<Ticket>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<ScheduleRegistration>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<TenantSubscription>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<ServicePriceRule>().Property(e => e.ExhibitorCategory).HasConversion<string>().HasMaxLength(20);

            // 2. القيود والفهارس الفريدة (Unique Constraints) المذكورة في التوثيق
            builder.Entity<Tenant>().HasIndex(t => t.Subdomain).IsUnique();
            builder.Entity<ExchangeRate>().HasIndex(e => new { e.FromCurrency, e.ToCurrency, e.RateDate }).IsUnique();
            builder.Entity<ScheduleRegistration>().HasIndex(s => new { s.ScheduleID, s.VisitorID }).IsUnique();
            builder.Entity<Invoice>().HasIndex(i => new { i.TenantID, i.InvoiceNumber }).IsUnique();
            builder.Entity<Ticket>().HasIndex(t => t.QRCode).IsUnique();

            // قيد التقييم من 1 إلى 5
            builder.Entity<VisitorRating>().ToTable(t => t.HasCheckConstraint("CK_VISITOR_RATINGS_Score", "Score >= 1 AND Score <= 5"));

            // 3. معالجة العلاقات الدائرية (Circular References) لمنع أخطاء الحذف التعاقبي
            builder.Entity<BoothMerge>()
                .HasOne(bm => bm.Reservation)
                .WithMany()
                .HasForeignKey(bm => bm.ReservationID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BoothReservation>()
                .HasOne(br => br.BoothMerge)
                .WithMany()
                .HasForeignKey(br => br.MergeID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booth>()
                .HasOne(b => b.BoothMerge)
                .WithMany()
                .HasForeignKey(b => b.MergeID)
                .OnDelete(DeleteBehavior.Restrict);

            // إيقاف الحذف التعاقبي (Cascade Delete) في بعض الجداول المهمة مالياً
            builder.Entity<Invoice>()
                .HasOne(i => i.Reservation)
                .WithOne()
                .HasForeignKey<Invoice>(i => i.ReservationID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany()
                .HasForeignKey(p => p.InvoiceID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PricingPackage>()
              .HasOne(p => p.Tenant)
              .WithMany()
              .HasForeignKey(p => p.TenantID)
              .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PricingPackage>()
                .HasOne(p => p.Currency)
                .WithMany()
                .HasForeignKey(p => p.CurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Tenant>()
                .HasOne(t => t.Currency)
                .WithMany()
                .HasForeignKey(t => t.BaseCurrency)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExchangeRate>()
                .HasOne(e => e.FromCurrencyNav)
                .WithMany()
                .HasForeignKey(e => e.FromCurrency)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExchangeRate>()
                .HasOne(e => e.ToCurrencyNav)
                .WithMany()
                .HasForeignKey(e => e.ToCurrency)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Exhibition>()
                .HasOne(e => e.Venue)
                .WithMany()
                .HasForeignKey(e => e.VenueID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Exhibition>()
                .HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExhibitionSchedule>()
                .HasOne(s => s.Exhibition)
                .WithMany()
                .HasForeignKey(s => s.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExhibitionSchedule>()
                .HasOne(s => s.Hall)
                .WithMany()
                .HasForeignKey(s => s.HallID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BoothReservation>()
                .HasOne(br => br.Exhibitor)
                .WithMany()
                .HasForeignKey(br => br.ExhibitorID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BoothReservation>()
                .HasOne(br => br.Exhibition)
                .WithMany()
                .HasForeignKey(br => br.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>()
                .HasOne(t => t.Exhibition)
                .WithMany()
                .HasForeignKey(t => t.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>()
                .HasOne(t => t.Visitor)
                .WithMany()
                .HasForeignKey(t => t.VisitorID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<VisitorRating>()
                .HasOne(vr => vr.Exhibition)
                .WithMany()
                .HasForeignKey(vr => vr.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<VisitorRating>()
                .HasOne(vr => vr.Visitor)
                .WithMany()
                .HasForeignKey(vr => vr.VisitorID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<VisitorRating>()
                .HasOne(vr => vr.Exhibitor)
                .WithMany()
                .HasForeignKey(vr => vr.ExhibitorID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Tenant)
                .WithMany()
                .HasForeignKey(u => u.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationRole>()
                .HasOne(r => r.Tenant)
                .WithMany()
                .HasForeignKey(r => r.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            // User relationship configurations
            builder.Entity<Exhibitor>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Visitor>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check Constraint on BoothReservation
            builder.Entity<BoothReservation>().ToTable(t =>
                t.HasCheckConstraint(
                    "CK_BoothReservation_BoothOrMerge",
                    "NOT (BoothID IS NOT NULL AND MergeID IS NOT NULL)"
                )
            );

            // Indexes
            builder.Entity<Exhibition>().HasIndex(e => new { e.TenantID, e.Status });
            builder.Entity<BoothReservation>().HasIndex(br => new { br.ExhibitionID, br.IsDeleted, br.Status });
            builder.Entity<BoothReservation>().HasIndex(br => br.ExhibitorID);
            builder.Entity<Booth>().HasIndex(b => new { b.HallID, b.IsDeleted, b.Status });
            builder.Entity<Ticket>().HasIndex(t => new { t.ExhibitionID, t.Status });
            builder.Entity<TicketScan>().HasIndex(ts => ts.TicketID);
            builder.Entity<AuditLog>().HasIndex(a => new { a.TenantID, a.ActionAt });
            builder.Entity<Invoice>().HasIndex(i => i.ReservationID);
            builder.Entity<Payment>().HasIndex(p => p.InvoiceID);
            builder.Entity<Exhibitor>().HasIndex(e => new { e.TenantID, e.IsDeleted });
            builder.Entity<Visitor>().HasIndex(v => new { v.TenantID, v.IsDeleted });

            // Global Cascade Delete Disable for Custom Models to prevent SQL Server multiple cascade path errors
            var cascadeFKs = builder.Model.GetEntityTypes()
                .Where(t => t.ClrType.Namespace == "ExhibitionManagementSystem.Models")
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in cascadeFKs)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // Global Soft Delete Filter
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var body = Expression.Equal(
                        Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)),
                        Expression.Constant(false)
                    );
                    builder.Entity(entityType.ClrType).HasQueryFilter(
                        Expression.Lambda(body, parameter)
                    );
                }
            }
        }










    }






}
