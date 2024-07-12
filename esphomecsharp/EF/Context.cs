using esphomecsharp.EF.Model;
using Microsoft.EntityFrameworkCore;

namespace esphomecsharp.EF;

public sealed class Context : DbContext
{
    public DbSet<Error> Error { get; set; }
    public DbSet<Event> Event { get; set; }
    public DbSet<RowEntry> RowEntry { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"DataSource={GlobalVariable.Settings.DBFileName};");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(x =>
        {
            x.HasKey(p => p.EventId);

            x.Property(p => p.EventId)
             .IsRequired()
             .ValueGeneratedOnAdd();

            x.Property(p => p.Data)
                .IsRequired()
                .HasColumnType("real");

            x.Property(p => p.RowEntryId)
             .IsRequired();

            x.Property(p => p.UnixTime)
             .IsRequired();

            x.Ignore(p => p.Id);
            x.Ignore(p => p.Name);
            x.Ignore(p => p.Value);
            x.Ignore(p => p.State);
            x.Ignore(p => p.Event_Type);

            x.HasOne(d => d.EspHomeId)
             .WithMany(dm => dm.Event)
             .HasForeignKey(dkey => dkey.RowEntryId);
        });

        modelBuilder.Entity<RowEntry>(x =>
        {
            x.HasKey(p => p.RowEntryId);

            x.Property(p => p.RowEntryId)
             .IsRequired()
             .ValueGeneratedOnAdd();

            x.Property(p => p.Name)
             .IsRequired();

            x.Property(p => p.FriendlyName)
             .IsRequired();

            x.HasIndex(p => new { p.Name, p.FriendlyName })
             .IsUnique();
        });

        modelBuilder.Entity<Error>(x =>
        {
            x.HasKey(p => p.ErrorId);

            x.Property(p => p.ErrorId)
             .IsRequired()
             .ValueGeneratedOnAdd();

            x.Property(p => p.Exception)
             .IsRequired();

            x.Property(p => p.Message)
             .IsRequired();

            x.Property(p => p.Date)
             .IsRequired();

            x.Property(p => p.IsHandled)
             .IsRequired();
        });
    }
}
