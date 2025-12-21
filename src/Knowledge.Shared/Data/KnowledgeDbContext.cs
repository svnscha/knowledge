using Knowledge.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Knowledge.Shared.Data;

/// <summary>
/// Entity Framework Core DbContext for the Knowledge application.
/// Supports PostgreSQL with pgvector extension for embedding storage.
/// </summary>
public class KnowledgeDbContext : DbContext
{
  public KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options)
      : base(options)
  {
  }

  /// <summary>
  /// Chat messages.
  /// </summary>
  public DbSet<Message> Messages => Set<Message>();

  /// <summary>
  /// Vector embeddings for semantic search.
  /// </summary>
  public DbSet<Embedding> Embeddings => Set<Embedding>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Enable pgvector extension
    modelBuilder.HasPostgresExtension("vector");

    ConfigureMessage(modelBuilder);
    ConfigureEmbedding(modelBuilder);
  }

  private static void ConfigureMessage(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Message>(entity =>
    {
      entity.ToTable("messages");

      entity.HasKey(e => e.Id);

      entity.Property(e => e.Id)
              .HasColumnName("id");

      entity.Property(e => e.ConversationId)
              .HasColumnName("conversation_id")
              .IsRequired();

      entity.Property(e => e.Role)
              .HasColumnName("role")
              .HasMaxLength(50)
              .IsRequired();

      entity.Property(e => e.AuthorName)
              .HasColumnName("author_name")
              .HasMaxLength(255);

      entity.Property(e => e.Content)
              .HasColumnName("content")
              .IsRequired();

      entity.Property(e => e.SequenceNumber)
              .HasColumnName("sequence_number")
              .IsRequired();

      entity.Property(e => e.CreatedAt)
              .HasColumnName("created_at")
              .IsRequired();

      // Index for conversation-based retrieval
      entity.HasIndex(e => e.ConversationId)
              .HasDatabaseName("ix_messages_conversation");

      // Composite index for ordered retrieval within a conversation
      entity.HasIndex(e => new { e.ConversationId, e.SequenceNumber })
              .HasDatabaseName("ix_messages_conversation_sequence");

      // Index for time-based queries
      entity.HasIndex(e => e.CreatedAt)
              .HasDatabaseName("ix_messages_created_at");

      // Optional foreign key to embedding
      entity.Property(e => e.EmbeddingId)
              .HasColumnName("embedding_id");

      entity.HasOne(e => e.Embedding)
              .WithOne(e => e.Message)
              .HasForeignKey<Message>(e => e.EmbeddingId)
              .OnDelete(DeleteBehavior.SetNull);

      // Index for finding messages without embeddings (partial index for efficient queries)
      entity.HasIndex(e => e.EmbeddingId)
              .HasDatabaseName("ix_messages_embedding");
    });
  }

  private static void ConfigureEmbedding(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Embedding>(entity =>
    {
      entity.ToTable("embeddings");

      entity.HasKey(e => e.Id);

      entity.Property(e => e.Id)
              .HasColumnName("id");

      entity.Property(e => e.SourceType)
              .HasColumnName("source_type")
              .HasMaxLength(50)
              .IsRequired();

      entity.Property(e => e.SourceId)
              .HasColumnName("source_id")
              .IsRequired();

      entity.Property(e => e.Content)
              .HasColumnName("content")
              .IsRequired();

      // Vector column with 1536 dimensions (text-embedding-3-small)
      entity.Property(e => e.Vector)
              .HasColumnName("vector")
              .HasColumnType("vector(1536)")
              .IsRequired();

      entity.Property(e => e.CreatedAt)
              .HasColumnName("created_at")
              .IsRequired();

      // Index for looking up embeddings by source
      entity.HasIndex(e => new { e.SourceType, e.SourceId })
              .HasDatabaseName("ix_embeddings_source");

      // Index for time-based queries
      entity.HasIndex(e => e.CreatedAt)
              .HasDatabaseName("ix_embeddings_created_at");
    });
  }
}
