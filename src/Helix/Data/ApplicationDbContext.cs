using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Helix.Models;
using System.Text.Json;

namespace Helix.Data;

/// <summary>
/// Application database context for managing conversations and messages.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Conversations in the application.
    /// </summary>
    public DbSet<Conversation> Conversations { get; set; } = null!;

    /// <summary>
    /// Messages in conversations.
    /// </summary>
    public DbSet<Message> Messages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Conversation entity
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure concurrency token
            entity.Property<byte[]>("ConcurrencyToken")
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.Property(e => e.Topic)
                .IsRequired()
                .HasMaxLength(500);

            // Configure one-to-many relationship
            entity.HasMany(e => e.Messages)
                .WithOne(e => e.Conversation!)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Message hierarchy using Table-Per-Hierarchy (TPH)
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Configure discriminator for inheritance
            entity.HasDiscriminator<string>("MessageType")
                .HasValue<UserMessage>("User")
                .HasValue<AssistantResponse>("Assistant")
                .HasValue<ToolCallMessage>("ToolCall");
            
            // Configure concurrency token
            entity.Property<byte[]>("ConcurrencyToken")
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.Property(e => e.Timestamp)
                .IsRequired();
        });

        modelBuilder.Entity<UserMessage>(entity =>
        {
            entity.Property(e => e.Content).IsRequired();
        });
        
        modelBuilder.Entity<AssistantResponse>(entity =>
        {
            entity.Property(e => e.Content).IsRequired();
        });
        
        modelBuilder.Entity<ToolCallMessage>(entity =>
        {
            entity.Property(e => e.ToolName).IsRequired();

            entity.Property(e => e.Arguments)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()));

            entity.Property(e => e.Arguments)
                .HasColumnType("TEXT");
        });
    }
}
