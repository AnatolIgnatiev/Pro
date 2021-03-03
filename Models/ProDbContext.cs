using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace Pro.Models
{
    public partial class ProDBContext : DbContext
    {
        private PasswordHasher hasher = new PasswordHasher();
        public ProDBContext()
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
            
        }

        public ProDBContext(DbContextOptions<ProDBContext> options)
            : base(options)
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
            
        }
        public DbSet<ProUser> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public virtual DbSet<Result> Results { get; set; }
        public virtual DbSet<Search> Searches { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(LocalDB)\\MSSQLLocalDB;Database=ProDB;Integrated Security=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            string adminRoleName = "admin";
            string adminLogin = "Admin";
            string adminPassword = hasher.Hash("123456");

            string userRoleName = "user";

            Role adminRole = new Role { Id = 1, Name = adminRoleName };
            Role userRole = new Role { Id = 2, Name = userRoleName };

            ProUser adminUser = new ProUser { Id = 1, Login= adminLogin, Password = adminPassword, RoleId = adminRole.Id };

            modelBuilder.Entity<Role>().HasData(new Role[] { adminRole, userRole });
            modelBuilder.Entity<ProUser>().HasData(new ProUser[] { adminUser });

            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<Result>(entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PK__Results__7C3F0D50D9F8F109");

                entity.Property(e => e.Id);
                entity.Property(e => e.PartId)
                .HasColumnType("varchar(100)")
                .IsRequired();

                entity.Property(e => e.Brand)
                    .IsRequired()
                    .IsUnicode(false);

                entity.Property(e => e.FirstPrice).HasColumnType("varchar(100)");

                entity.Property(e => e.OriginalPrice).HasColumnType("varchar(100)");

                entity.Property(e => e.SecondPrice).HasColumnType("varchar(100)");

                entity.HasOne(d => d.Search)
                    .WithMany(p => p.Results)
                    .HasForeignKey(d => d.SearchId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_Results_Searches");
                entity.Property(e=>e.IsSuccessful).HasColumnType("bit")
                    .IsRequired();
            });

            modelBuilder.Entity<Search>(entity =>
            {
                entity.Property(e => e.SearchId).ValueGeneratedNever();
                entity.Property(e => e.SearchId)
                .HasColumnType("varchar(100)")
                .IsRequired();

                entity.Property(e => e.Date)
                .HasColumnType("datetime")
                .IsRequired();
            });

            OnModelCreatingPartial(modelBuilder);
        }
        
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
