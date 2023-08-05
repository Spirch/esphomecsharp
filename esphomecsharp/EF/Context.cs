using esphomecsharp.EF.Model;
using Microsoft.EntityFrameworkCore;

namespace esphomecsharp.EF
{
    public sealed class Context : DbContext
    {
        public DbSet<Error> Error { get; set; }
        public DbSet<Event> EspHomeEvent { get; set; }
        public DbSet<EventId> EspHomeId { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"DataSource={GlobalVariable.DBFileName};");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>(x =>
            {
                x.HasKey(p => new { p.DescId, p.Date, p.PkSuffix });

                x.Property(p => p.PkSuffix)
                 .IsRequired()
                 .HasDefaultValueSql("random()");

                x.Property(p => p.DescId)
                 .IsRequired();

                x.Property(p => p.Date)
                 .IsRequired();

                x.Ignore(p => p.Id);
                x.Ignore(p => p.Name);
                x.Ignore(p => p.Value);

                x.HasOne(d => d.EspHomeId)
                 .WithMany(dm => dm.EspHomeEvent)
                 .HasForeignKey(dkey => dkey.DescId);
            });

            modelBuilder.Entity<EventId>(x =>
            {
                x.HasKey(p => p.Id);

                x.Property(p => p.Id)
                 .IsRequired()
                 .ValueGeneratedOnAdd();

                x.Property(p => p.Value)
                 .IsRequired();

                x.HasIndex(p => p.Value)
                 .IsUnique();
            });

            modelBuilder.Entity<Error>(x =>
            {
                x.HasKey(p => p.Id);

                x.Property(p => p.Id)
                 .IsRequired()
                 .ValueGeneratedOnAdd();

                x.Property(p => p.Message)
                 .IsRequired();

                x.Property(p => p.Date)
                 .IsRequired();

                x.Property(p => p.IsHandled)
                 .IsRequired();
            });
        }
    }
}
