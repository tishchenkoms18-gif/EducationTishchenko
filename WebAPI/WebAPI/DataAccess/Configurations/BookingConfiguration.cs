using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WebAPI.DataAccess.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
   public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(b => b.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

      
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.ProcessedAt);   
       
        builder.HasOne(b => b.Event)
               .WithMany(e => e.Bookings)
               .HasForeignKey(b => b.EventId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
