using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NPPE.Domain.Entities;

namespace NPPE.Infrastructure.Persistence;
public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<AttemptedAnswer> AttemptedAnswers => Set<AttemptedAnswer>();
    public DbSet<Payment> Payments { get; set; }
    public DbSet<ProcessedStripeEvent> ProcessedStripeEvents => Set<ProcessedStripeEvent>();
    public DbSet<Cost> Costs => Set<Cost>();
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Application user
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.IsPremium);
            entity.Property(u => u.StripeCustomerId)?.HasMaxLength(255);
            entity.Property(u => u.StripeSubscriptionId).HasMaxLength(255);
            entity.Property(u => u.SubscriptionEndDate);
        });

        // Configure Exam
        builder.Entity<Exam>(entity =>
        {
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
        });

        // Question
        builder.Entity<Question>(entity =>
        {
            entity.Property(q => q.Text).IsRequired().HasMaxLength(2000);
            entity.Property(q => q.IsActive).HasDefaultValue(true);
            entity.HasOne(q => q.Exam)
                  .WithMany(e => e.Questions)
                  .HasForeignKey(q => q.ExamId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AnswerOption
        builder.Entity<AnswerOption>(entity =>
        {
            entity.Property(o => o.Text).IsRequired().HasMaxLength(1000);
            entity.Property(o => o.Label).IsRequired();
            entity.HasOne(o => o.Question)
                  .WithMany(q => q.Options)
                  .HasForeignKey(o => o.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Payment
        builder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.StripeSessionId).IsRequired().HasMaxLength(255);
            entity.Property(p => p.Amount).HasPrecision(18, 2);
            entity.Property(p => p.Currency).IsRequired().HasMaxLength(3);
            entity.Property(p => p.Status).IsRequired().HasMaxLength(50);
            entity.Property(p => p.PaymentType).IsRequired();
            entity.Property(p => p.StripeSubscriptionId).HasMaxLength(255);
            entity.Property(p => p.SubscriptionStatus);
            entity.Property(p => p.StripeInvoiceId).HasMaxLength(255);
            entity.Property(p => p.CustomerCountry).HasMaxLength(2);
        });

        // Business costs (admin-recorded)
        builder.Entity<Cost>(entity =>
        {
            entity.Property(c => c.Provider).IsRequired().HasMaxLength(120);
            entity.Property(c => c.Amount).HasPrecision(18, 2);
            entity.Property(c => c.Currency).IsRequired().HasMaxLength(3);
            entity.Property(c => c.Note).HasMaxLength(500);
        });

        // Processed Stripe webhook events (idempotency)
        builder.Entity<ProcessedStripeEvent>(entity =>
        {
            entity.Property(e => e.StripeEventId).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.StripeEventId).IsUnique();
        });
    }
}