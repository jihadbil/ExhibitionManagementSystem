using ExhibitionManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;
using ExhibitionManagementSystem.DataAccess.MultiTenancy;
using System.Linq.Expressions;
using System.Text;

namespace ExhibitionManagementSystem.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        private readonly ITenantContext _tenantContext;
        private int? _provisioningTenantId;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenantContext? tenantContext = null) : base(options)
        {
            _tenantContext = tenantContext ?? new EmptyTenantContext();
        }

        public int CurrentTenantId => _tenantContext.TenantId ?? _provisioningTenantId ?? 0;
        public bool CanBypassTenantIsolation => _tenantContext.CanBypassTenantIsolation;

        /// <summary>
        /// Allows the same scoped context that has just created a tenant to finish
        /// provisioning its first administrator and tenant-owned audit record.
        /// This is deliberately tenant-specific and cannot be used from an existing
        /// authenticated tenant request or nested to switch tenant identity.
        /// </summary>
        public IDisposable BeginTenantProvisioningScope(int tenantId)
        {
            if (tenantId <= 0)
                throw new ArgumentOutOfRangeException(nameof(tenantId));
            if (_tenantContext.TenantId.HasValue || _provisioningTenantId.HasValue)
                throw new InvalidOperationException("A tenant context is already active.");

            var tenantIsTracked = ChangeTracker.Entries<Tenant>().Any(entry =>
                entry.Entity.TenantID == tenantId &&
                entry.State != EntityState.Detached &&
                entry.State != EntityState.Deleted);
            if (!tenantIsTracked)
                throw new InvalidOperationException("The provisioned tenant must be tracked by this context.");

            _provisioningTenantId = tenantId;
            return new TenantProvisioningScope(this);
        }






        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantSubscription> TenantSubscriptions { get; set; }
        public DbSet<TenantInvitation> TenantInvitations { get; set; }
        public DbSet<RefreshSession> RefreshSessions { get; set; }
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }
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
        public DbSet<InvoiceSequence> InvoiceSequences { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<CreditNote> CreditNotes { get; set; }

        public DbSet<FinancialReport> FinancialReports { get; set; }
        public DbSet<Visitor> Visitors { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketScan> TicketScans { get; set; }
        public DbSet<VisitorRating> VisitorRatings { get; set; }
        public DbSet<Expense> Expenses { get; set; }

        // New Badge and Sponsorship DbSets
        public DbSet<BadgeTemplate> BadgeTemplates { get; set; }
        public DbSet<Sponsor> Sponsors { get; set; }
        public DbSet<SponsorshipPackage> SponsorshipPackages { get; set; }
        public DbSet<SponsorshipContract> SponsorshipContracts { get; set; }
        public DbSet<AdvertisingSpace> AdvertisingSpaces { get; set; }
        public DbSet<SponsorAdAssignment> SponsorAdAssignments { get; set; }

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
            builder.Entity<CreditNote>().Property(e => e.Type).HasConversion<string>().HasMaxLength(20);
            builder.Entity<TicketScan>().Property(e => e.Direction).HasConversion<string>().HasMaxLength(10);
            builder.Entity<Booth>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<Booth>().Property(e => e.ShapeType).HasConversion<string>().HasMaxLength(20);
            builder.Entity<Booth>().Property(e => e.ShapeType).HasDefaultValue(BoothShapeType.Rect);
            builder.Entity<Booth>().Property(e => e.BoothType).HasConversion<string>().HasMaxLength(50);
            builder.Entity<Booth>().Property(e => e.BoothType).HasDefaultValue(BoothType.Standard);
            builder.Entity<Venue>().Property(e => e.SitePlanVersion).IsConcurrencyToken();
            builder.Entity<ExhibitionSchedule>().Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
            builder.Entity<Exhibitor>().Property(e => e.ExhibitorCategory).HasConversion<string>().HasMaxLength(20);

            // New Enums Conversions
            builder.Entity<Ticket>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<ScheduleRegistration>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<TenantSubscription>().Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
            builder.Entity<ServicePriceRule>().Property(e => e.ExhibitorCategory).HasConversion<string>().HasMaxLength(20);
            
            // Badge & Sponsorship Enums Conversions
            builder.Entity<BadgeTemplate>().Property(e => e.TargetParticipantType).HasConversion<string>().HasMaxLength(30);
            builder.Entity<BadgeTemplate>().Property(e => e.Orientation).HasConversion<string>().HasMaxLength(20);
            builder.Entity<SponsorshipPackage>().Property(e => e.Level).HasConversion<string>().HasMaxLength(30);
            builder.Entity<AdvertisingSpace>().Property(e => e.SpaceType).HasConversion<string>().HasMaxLength(50);
            builder.Entity<SponsorshipContract>().Property(e => e.Status).HasConversion<string>().HasMaxLength(30);

            // 2. القيود والفهارس الفريدة (Unique Constraints) المذكورة في التوثيق
            builder.Entity<Tenant>().HasIndex(t => t.Subdomain).IsUnique();
            builder.Entity<ExchangeRate>().HasIndex(e => new { e.FromCurrency, e.ToCurrency, e.RateDate }).IsUnique();
            builder.Entity<ScheduleRegistration>().HasIndex(s => new { s.ScheduleID, s.VisitorID }).IsUnique();
            builder.Entity<Invoice>().HasIndex(i => new { i.TenantID, i.InvoiceNumber }).IsUnique();
            builder.Entity<InvoiceSequence>().HasKey(sequence => new { sequence.TenantID, sequence.Year });
            builder.Entity<Ticket>().HasIndex(t => t.QRCode).IsUnique();
            builder.Entity<RefreshSession>().HasIndex(session => session.TokenHash).IsUnique();
            builder.Entity<RefreshSession>().HasIndex(session => new { session.UserId, session.RevokedAt, session.ExpiresAt });
            builder.Entity<IdempotencyRecord>()
                .HasIndex(record => new { record.TenantID, record.Operation, record.Key })
                .IsUnique();
            builder.Entity<IdempotencyRecord>()
                .HasIndex(record => record.ExpiresAt);
            builder.Entity<CreditNote>()
                .HasIndex(note => new { note.TenantID, note.CreditNoteNumber })
                .IsUnique();
            builder.Entity<CreditNote>()
                .HasIndex(note => new { note.TenantID, note.InvoiceID });
            builder.Entity<TenantInvitation>()
                .HasIndex(invitation => invitation.TokenHash)
                .IsUnique();
            builder.Entity<TenantInvitation>()
                .HasIndex(invitation => invitation.NormalizedEmail)
                .IsUnique()
                .HasFilter("[AcceptedAt] IS NULL AND [RevokedAt] IS NULL");
            builder.Entity<TenantInvitation>()
                .HasIndex(invitation => new
                {
                    invitation.TenantID,
                    invitation.NormalizedEmail,
                    invitation.AcceptedAt,
                    invitation.RevokedAt
                });

            // قيد التقييم من 1 إلى 5
            builder.Entity<VisitorRating>().ToTable(t => t.HasCheckConstraint("CK_VISITOR_RATINGS_Score", "Score >= 1 AND Score <= 5"));

            // Financial invariants are enforced in the database as a final line
            // of defense in addition to API validation and service checks.
            builder.Entity<InvoiceItem>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_InvoiceItems_Quantity_Positive", "Quantity > 0");
                t.HasCheckConstraint("CK_InvoiceItems_UnitPrice_NonNegative", "UnitPrice >= 0");
                t.HasCheckConstraint("CK_InvoiceItems_TotalPrice_NonNegative", "TotalPrice >= 0");
            });
            builder.Entity<Payment>().ToTable(t =>
                t.HasCheckConstraint("CK_Payments_Amount_Positive", "Amount > 0"));
            builder.Entity<CreditNote>().ToTable(t =>
                t.HasCheckConstraint("CK_CreditNotes_Amount_Positive", "Amount > 0"));
            builder.Entity<Expense>().ToTable(t =>
                t.HasCheckConstraint("CK_Expenses_Amount_Positive", "Amount > 0"));
            builder.Entity<ExchangeRate>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_ExchangeRates_Rate_Positive", "Rate > 0");
                t.HasCheckConstraint("CK_ExchangeRates_DifferentCurrencies", "FromCurrency <> ToCurrency");
            });
            builder.Entity<Ticket>().ToTable(t =>
                t.HasCheckConstraint("CK_Tickets_Price_NonNegative", "Price >= 0"));
            builder.Entity<SponsorshipPackage>().ToTable(t =>
            {
                t.HasCheckConstraint("CK_SponsorshipPackages_Price_NonNegative", "Price >= 0");
                t.HasCheckConstraint("CK_SponsorshipPackages_MaxSponsors_Positive", "MaxSponsorsCount > 0");
            });
            builder.Entity<SponsorshipContract>().ToTable(t =>
                t.HasCheckConstraint("CK_SponsorshipContracts_TotalAmount_NonNegative", "TotalAmount >= 0"));
            builder.Entity<AdvertisingSpace>().ToTable(t =>
                t.HasCheckConstraint("CK_AdvertisingSpaces_BasePrice_NonNegative", "BasePrice >= 0"));

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
                .WithOne(r => r.Invoice)
                .HasForeignKey<Invoice>(i => i.ReservationID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CreditNote>()
                .HasOne(note => note.Invoice)
                .WithMany(invoice => invoice.CreditNotes)
                .HasForeignKey(note => note.InvoiceID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CreditNote>()
                .HasOne(note => note.Tenant)
                .WithMany()
                .HasForeignKey(note => note.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CreditNote>()
                .HasOne(note => note.CreatedByUser)
                .WithMany()
                .HasForeignKey(note => note.CreatedByUserId)
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
                .WithMany(e => e.ExhibitionSchedules)
                .HasForeignKey(s => s.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Exhibitor>()
                .HasOne(e => e.PreferredCurrency)
                .WithMany()
                .HasForeignKey(e => e.PreferredCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Service>()
                .HasOne(e => e.DefaultCurrency)
                .WithMany()
                .HasForeignKey(e => e.DefaultCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BoothReservation>()
                .HasOne(e => e.BaseCurrency)
                .WithMany()
                .HasForeignKey(e => e.BaseCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BoothReservation>()
                .HasOne(e => e.BoothSourceCurrency)
                .WithMany()
                .HasForeignKey(e => e.BoothSourceCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReservationService>()
                .HasOne(e => e.SourceCurrency)
                .WithMany()
                .HasForeignKey(e => e.SourceCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InvoiceItem>().HasOne(i => i.ReservationService)
                .WithMany().HasForeignKey(i => i.ReservationServiceID).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<BoothMerge>().HasAlternateKey(m => new { m.MergeID, m.ExhibitionID });
            builder.Entity<BoothMergeItem>().HasOne(i => i.Merge).WithMany(m => m.MergeItems)
                .HasForeignKey(i => new { i.MergeID, i.ExhibitionID })
                .HasPrincipalKey(m => new { m.MergeID, m.ExhibitionID }).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<BoothMergeItem>().HasIndex(i => new { i.ExhibitionID, i.BoothID }).IsUnique();
            builder.Entity<ServicePriceRule>().ToTable(t =>
                t.HasCheckConstraint("CK_ServicePriceRules_Price_NonNegative", "UnitPrice >= 0"));

            builder.Entity<Invoice>()
                .HasOne(e => e.BaseCurrency)
                .WithMany()
                .HasForeignKey(e => e.BaseCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(e => e.BaseCurrency)
                .WithMany()
                .HasForeignKey(e => e.BaseCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Expense>()
                .HasOne(e => e.BaseCurrency)
                .WithMany()
                .HasForeignKey(e => e.BaseCurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ExhibitionSchedule>()
                .HasOne(s => s.Hall)
                .WithMany()
                .HasForeignKey(s => s.HallID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BoothReservation>()
                .HasOne(br => br.Exhibitor)
                .WithMany(e => e.BoothReservations)
                .HasForeignKey(br => br.ExhibitorID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BoothReservation>()
                .HasOne(br => br.Exhibition)
                .WithMany(e => e.BoothReservations)
                .HasForeignKey(br => br.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BoothReservation>()
                .HasOne(br => br.Tenant)
                .WithMany()
                .HasForeignKey(br => br.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>()
                .HasOne(t => t.Exhibition)
                .WithMany()
                .HasForeignKey(t => t.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>()
                .HasOne(t => t.Tenant)
                .WithMany()
                .HasForeignKey(t => t.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booth>()
                .HasOne(b => b.Tenant)
                .WithMany()
                .HasForeignKey(b => b.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>()
                .HasOne(t => t.Visitor)
                .WithMany(v => v.Tickets)
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

            builder.Entity<RefreshSession>()
                .HasOne(session => session.User)
                .WithMany(user => user.RefreshSessions)
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TenantInvitation>()
                .HasOne(invitation => invitation.Tenant)
                .WithMany()
                .HasForeignKey(invitation => invitation.TenantID)
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
            builder.Entity<BoothReservation>()
                .HasIndex(br => new { br.ExhibitionID, br.BoothID })
                .IsUnique()
                .HasFilter("[BoothID] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> N'Cancelled'")
                .HasDatabaseName("UX_BoothReservations_ActiveBooth");
            builder.Entity<BoothReservation>()
                .HasIndex(br => new { br.ExhibitionID, br.MergeID })
                .IsUnique()
                .HasFilter("[MergeID] IS NOT NULL AND [IsDeleted] = 0 AND [Status] <> N'Cancelled'")
                .HasDatabaseName("UX_BoothReservations_ActiveMerge");
            builder.Entity<Booth>().HasIndex(b => new { b.HallID, b.IsDeleted, b.Status });
            builder.Entity<Booth>().HasIndex(b => new { b.TenantID, b.HallID });
            builder.Entity<Booth>()
                .HasIndex(b => new { b.HallID, b.BoothNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("UX_Booths_ActiveNumberPerHall");
            builder.Entity<Ticket>().HasIndex(t => new { t.ExhibitionID, t.Status });
            builder.Entity<Ticket>().HasIndex(t => new { t.TenantID, t.ExhibitionID });
            builder.Entity<TicketScan>().HasIndex(ts => ts.TicketID);
            builder.Entity<AuditLog>().HasIndex(a => new { a.TenantID, a.ActionAt });
            builder.Entity<Invoice>().HasIndex(i => i.ReservationID);
            builder.Entity<Payment>().HasIndex(p => p.InvoiceID);
            builder.Entity<Payment>().HasIndex(p => new { p.TenantID, p.InvoiceID });
            builder.Entity<Exhibitor>().HasIndex(e => new { e.TenantID, e.IsDeleted });
            builder.Entity<Visitor>().HasIndex(v => new { v.TenantID, v.IsDeleted });

            builder.Entity<Expense>()
                .HasOne(e => e.Exhibition)
                .WithMany()
                .HasForeignKey(e => e.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Expense>()
                .HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Expense>().HasIndex(e => new { e.ExhibitionID, e.IsDeleted });

            // Badge & Sponsorship Indexes & Unique Constraints
            builder.Entity<BadgeTemplate>().HasIndex(b => new { b.TenantID, b.ExhibitionID, b.TargetParticipantType });
            builder.Entity<Sponsor>().HasIndex(s => new { s.TenantID, s.IsDeleted });
            builder.Entity<SponsorshipPackage>().HasIndex(p => new { p.ExhibitionID, p.IsDeleted });
            builder.Entity<SponsorshipContract>().HasIndex(c => new { c.TenantID, c.ContractNumber }).IsUnique();
            builder.Entity<AdvertisingSpace>().HasIndex(a => new { a.ExhibitionID, a.SpaceCode }).IsUnique();
            builder.Entity<SponsorAdAssignment>().HasIndex(sa => new { sa.ContractID, sa.SpaceID });
            builder.Entity<SponsorAdAssignment>()
                .HasIndex(sa => sa.SpaceID)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0")
                .HasDatabaseName("UX_SponsorAdAssignments_ActiveSpace");

            // Badge & Sponsorship FK Configurations
            builder.Entity<BadgeTemplate>()
                .HasOne(b => b.Tenant)
                .WithMany()
                .HasForeignKey(b => b.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BadgeTemplate>()
                .HasOne(b => b.Exhibition)
                .WithMany()
                .HasForeignKey(b => b.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Sponsor>()
                .HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorshipPackage>()
                .HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorshipPackage>()
                .HasOne(p => p.Exhibition)
                .WithMany()
                .HasForeignKey(p => p.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorshipPackage>()
                .HasOne(p => p.Currency)
                .WithMany()
                .HasForeignKey(p => p.CurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorshipContract>()
                .HasOne(c => c.Tenant)
                .WithMany()
                .HasForeignKey(c => c.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorshipContract>()
                .HasOne(c => c.Exhibition)
                .WithMany()
                .HasForeignKey(c => c.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorshipContract>()
                .HasOne(c => c.Sponsor)
                .WithMany(s => s.Contracts)
                .HasForeignKey(c => c.SponsorID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorshipContract>()
                .HasOne(c => c.Package)
                .WithMany(p => p.Contracts)
                .HasForeignKey(c => c.PackageID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorshipContract>()
                .HasOne(c => c.Currency)
                .WithMany()
                .HasForeignKey(c => c.CurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdvertisingSpace>()
                .HasOne(a => a.Tenant)
                .WithMany()
                .HasForeignKey(a => a.TenantID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdvertisingSpace>()
                .HasOne(a => a.Exhibition)
                .WithMany()
                .HasForeignKey(a => a.ExhibitionID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdvertisingSpace>()
                .HasOne(a => a.Venue)
                .WithMany()
                .HasForeignKey(a => a.VenueID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdvertisingSpace>()
                .HasOne(a => a.Hall)
                .WithMany()
                .HasForeignKey(a => a.HallID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdvertisingSpace>()
                .HasOne(a => a.Currency)
                .WithMany()
                .HasForeignKey(a => a.CurrencyCode)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorAdAssignment>()
                .HasOne(sa => sa.Contract)
                .WithMany(c => c.AdAssignments)
                .HasForeignKey(sa => sa.ContractID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SponsorAdAssignment>()
                .HasOne(sa => sa.Space)
                .WithMany(s => s.AdAssignments)
                .HasForeignKey(sa => sa.SpaceID)
                .OnDelete(DeleteBehavior.Restrict);

            // Global Cascade Delete Disable for Custom Models to prevent SQL Server multiple cascade path errors
            var cascadeFKs = builder.Model.GetEntityTypes()
                .Where(t => t.ClrType.Namespace == "ExhibitionManagementSystem.Models")
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk =>
                    !fk.IsOwnership &&
                    fk.DeleteBehavior == DeleteBehavior.Cascade &&
                    fk.DeclaringEntityType.ClrType != typeof(RefreshSession));

            foreach (var fk in cascadeFKs)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // Global tenant isolation and soft-delete filters. Both conditions are
            // combined so adding one filter never overwrites the other.
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType);
                var isTenantEntity = typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType);
                if (!isSoftDeletable && !isTenantEntity)
                {
                    continue;
                }

                var parameter = Expression.Parameter(entityType.ClrType, "e");
                Expression body = Expression.Constant(true);

                if (isSoftDeletable)
                {
                    var notDeleted = Expression.Equal(
                        Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)),
                        Expression.Constant(false)
                    );
                    body = Expression.AndAlso(body, notDeleted);
                }

                if (isTenantEntity)
                {
                    var bypass = Expression.Property(
                        Expression.Constant(this),
                        nameof(CanBypassTenantIsolation));
                    var tenantMatches = Expression.Equal(
                        Expression.Property(parameter, nameof(ITenantEntity.TenantID)),
                        Expression.Property(
                            Expression.Constant(this),
                            nameof(CurrentTenantId)));
                    body = Expression.AndAlso(body, Expression.OrElse(bypass, tenantMatches));
                }

                builder.Entity(entityType.ClrType).HasQueryFilter(
                    Expression.Lambda(body, parameter));
            }
        }

        public override int SaveChanges()
        {
            EnforceTenantWrites();
            EnforceIssuedInvoiceImmutability();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            EnforceTenantWrites();
            EnforceIssuedInvoiceImmutability();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void EnforceIssuedInvoiceImmutability()
        {
            var immutableProperties = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(Invoice.TenantID),
                nameof(Invoice.ReservationID),
                nameof(Invoice.InvoiceNumber),
                nameof(Invoice.InvoiceDate),
                nameof(Invoice.SubTotal),
                nameof(Invoice.TaxRate),
                nameof(Invoice.TaxAmount),
                nameof(Invoice.TotalAmount),
                nameof(Invoice.CurrencyCode),
                nameof(Invoice.BaseCurrencyCode),
                nameof(Invoice.ExchangeRateUsed),
                nameof(Invoice.ExchangeRateDate),
                nameof(Invoice.TotalAmountInBaseCurrency),
                nameof(Invoice.DueDate),
                nameof(Invoice.Notes),
                nameof(Invoice.IsDeleted),
                nameof(Invoice.DeletedAt),
                nameof(Invoice.DeletedByUserId)
            };

            foreach (var entry in ChangeTracker.Entries<Invoice>()
                         .Where(candidate => candidate.State == EntityState.Modified))
            {
                var originalStatus = entry.Property(invoice => invoice.Status).OriginalValue;
                if (originalStatus == InvoiceStatus.Draft)
                {
                    continue;
                }

                if (entry.Properties.Any(property =>
                        property.IsModified && immutableProperties.Contains(property.Metadata.Name)))
                {
                    throw new InvalidOperationException(
                        "Issued invoice financial data is immutable; use a credit note or refund.");
                }

                var currentStatus = entry.Entity.Status;
                var validTransition = originalStatus switch
                {
                    InvoiceStatus.Issued => currentStatus is InvoiceStatus.Issued or
                        InvoiceStatus.PartiallyPaid or InvoiceStatus.Paid or InvoiceStatus.Cancelled,
                    InvoiceStatus.PartiallyPaid => currentStatus is InvoiceStatus.PartiallyPaid or
                        InvoiceStatus.Issued or InvoiceStatus.Paid or InvoiceStatus.Cancelled,
                    InvoiceStatus.Paid => currentStatus is InvoiceStatus.Paid or
                        InvoiceStatus.PartiallyPaid or InvoiceStatus.Issued ||
                        (currentStatus == InvoiceStatus.Cancelled &&
                         InvoiceFinancialSummary.Calculate(entry.Entity).NetPaid == 0 &&
                         InvoiceFinancialSummary.Calculate(entry.Entity).AdjustedTotal == 0),
                    InvoiceStatus.Cancelled => currentStatus == InvoiceStatus.Cancelled,
                    _ => false
                };

                if (!validTransition)
                {
                    throw new InvalidOperationException(
                        "Invalid issued invoice status transition.");
                }
            }
        }

        private void EnforceTenantWrites()
        {
            if (CanBypassTenantIsolation)
            {
                return;
            }

            var tenantId = _tenantContext.TenantId ?? _provisioningTenantId;
            var tenantEntries = ChangeTracker.Entries()
                .Where(entry => entry.Entity is ITenantEntity &&
                                entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToArray();

            if (tenantEntries.Length == 0)
            {
                return;
            }

            if (tenantId is null or <= 0)
            {
                throw new InvalidOperationException(
                    "A trusted tenant context is required for tenant-owned writes.");
            }

            foreach (var entry in tenantEntries)
            {
                var entity = (ITenantEntity)entry.Entity;
                if (entry.State == EntityState.Added && entity.TenantID == 0)
                {
                    entity.TenantID = tenantId.Value;
                }

                if (entity.TenantID != tenantId.Value)
                {
                    throw new InvalidOperationException(
                        "Cross-tenant write was blocked.");
                }
            }
        }

        private sealed class TenantProvisioningScope(ApplicationDbContext context) : IDisposable
        {
            private ApplicationDbContext? _context = context;

            public void Dispose()
            {
                if (_context == null)
                    return;

                _context._provisioningTenantId = null;
                _context = null;
            }
        }










    }






}
