using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Helix.Models;
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Helix.Data;

/// <summary>
/// Application database context for managing conversations and messages.
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Conversations in the application.
    /// </summary>
    public DbSet<Conversation> Conversations { get; set; } = null!;

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

            // Chat history is serialized as JSON to make sure that we keep the tool calls
            // as they contain important context information for the coding agent.
            entity.Property(x => x.ChatHistory)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<ChatHistory>(v, (JsonSerializerOptions?)null) ?? new ChatHistory())
                .Metadata.SetValueComparer(new ValueComparer<ChatHistory>(
                    (c1, c2) => c1 == c2 || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                    c => c.Aggregate(0, (hash, msg) => HashCode.Combine(hash, msg.GetHashCode())),
                    c => JsonSerializer.Deserialize<ChatHistory>(JsonSerializer.Serialize(c, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new ChatHistory()));
        });
    }
}
