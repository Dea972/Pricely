using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pricely.Common.Infrastructure.Outbox;

namespace Pricely.Common.Infrastructure.Inbox;
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Content)
               .HasColumnType("text"); // not jsonb: the serialized event starts with its $type, which jsonb would reorder
    }
}

