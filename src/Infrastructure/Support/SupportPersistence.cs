using Application.Support;
using Domain.Support;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Support;

public sealed class SupportConversationConfiguration : IEntityTypeConfiguration<SupportConversation>
{
    public void Configure(EntityTypeBuilder<SupportConversation> builder)
    {
        builder.ToTable("SupportConversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasIndex(x => x.LearnerId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.UpdatedAt });
        builder.HasIndex(x => new { x.AssignedAdminId, x.Status, x.UpdatedAt });
        builder.HasMany(x => x.Messages).WithOne().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.ToTable("SupportMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SenderKind).HasConversion<int>();
        builder.Property(x => x.Text).HasMaxLength(8000);
        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt });
        builder.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class SupportAttachmentConfiguration : IEntityTypeConfiguration<SupportAttachment>
{
    public void Configure(EntityTypeBuilder<SupportAttachment> builder)
    {
        builder.ToTable("SupportAttachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ObjectKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Checksum).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.MessageId);
    }
}

public sealed class EfSupportConversationRepository(EnglishAiDbContext db) : ISupportConversationRepository
{
    private IQueryable<SupportConversation> Query() => db.SupportConversations
        .Include(x => x.Messages).ThenInclude(x => x.Attachments);

    public Task<SupportConversation?> GetByLearnerAsync(Guid learnerId, CancellationToken cancellationToken) =>
        Query().SingleOrDefaultAsync(x => x.LearnerId == learnerId, cancellationToken);

    public Task<SupportConversation?> GetAsync(Guid conversationId, CancellationToken cancellationToken) =>
        Query().SingleOrDefaultAsync(x => x.Id == conversationId, cancellationToken);

    public async Task<IReadOnlyList<SupportConversation>> ListAsync(
        string filter, Guid adminId, int limit, CancellationToken cancellationToken)
    {
        var query = Query().AsNoTracking();
        query = filter.Trim().ToLowerInvariant() switch
        {
            "mine" => query.Where(x => x.Status == SupportConversationStatus.Open && x.AssignedAdminId == adminId),
            "unassigned" => query.Where(x => x.Status == SupportConversationStatus.Open && x.AssignedAdminId == null),
            "closed" => query.Where(x => x.Status == SupportConversationStatus.Closed),
            _ => query.Where(x => x.Status == SupportConversationStatus.Open),
        };
        return await query.OrderByDescending(x => x.UpdatedAt).Take(limit).ToListAsync(cancellationToken);
    }

    public Task AddAsync(SupportConversation conversation, CancellationToken cancellationToken) =>
        db.SupportConversations.AddAsync(conversation, cancellationToken).AsTask();
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

public sealed class InMemorySupportConversationRepository : ISupportConversationRepository
{
    private readonly List<SupportConversation> _items = new();
    private readonly object _gate = new();
    public Task<SupportConversation?> GetByLearnerAsync(Guid learnerId, CancellationToken cancellationToken)
    { lock (_gate) return Task.FromResult(_items.SingleOrDefault(x => x.LearnerId == learnerId)); }
    public Task<SupportConversation?> GetAsync(Guid conversationId, CancellationToken cancellationToken)
    { lock (_gate) return Task.FromResult(_items.SingleOrDefault(x => x.Id == conversationId)); }
    public Task<IReadOnlyList<SupportConversation>> ListAsync(string filter, Guid adminId, int limit, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IEnumerable<SupportConversation> query = _items;
            query = filter.Trim().ToLowerInvariant() switch
            {
                "mine" => query.Where(x => x.Status == SupportConversationStatus.Open && x.AssignedAdminId == adminId),
                "unassigned" => query.Where(x => x.Status == SupportConversationStatus.Open && x.AssignedAdminId == null),
                "closed" => query.Where(x => x.Status == SupportConversationStatus.Closed),
                _ => query.Where(x => x.Status == SupportConversationStatus.Open),
            };
            return Task.FromResult<IReadOnlyList<SupportConversation>>(query.OrderByDescending(x => x.UpdatedAt).Take(limit).ToList());
        }
    }
    public Task AddAsync(SupportConversation conversation, CancellationToken cancellationToken)
    { lock (_gate) _items.Add(conversation); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
