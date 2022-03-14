using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMTPBroker.Models;

namespace SMTPBroker.Persistence;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(e => e.Id);

        builder.OwnsMany(e => e.From, builder => builder.ToTable("MessageFromAddress"));
        builder.OwnsMany(e => e.To, builder => builder.ToTable("MessageToAddress"));
        builder.OwnsMany(e => e.Attachments, builder => builder.HasKey("Id"));
    }
}