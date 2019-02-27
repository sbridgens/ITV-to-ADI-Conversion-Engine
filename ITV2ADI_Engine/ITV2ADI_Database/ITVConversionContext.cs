using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SCH_CONFIG;

namespace ITV2ADI_Engine
{
    public partial class ITVConversionContext : DbContext
    {
        public ITVConversionContext()
        {
        }

        public ITVConversionContext(DbContextOptions<ITVConversionContext> options)
            : base(options)
        {
        }

        public virtual DbSet<FieldMappings> FieldMappings { get; set; }
        public virtual DbSet<ProviderContentTierMapping> ProviderContentTierMapping { get; set; }
        public virtual DbSet<ReportClassMapping> ReportClassMapping { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer($"Server={ITV2ADI_CONFIG.Database_Host},1433;" +
                                            $"Database={ITV2ADI_CONFIG.Database_Name};" +
                                            $"Trusted_Connection={ITV2ADI_CONFIG.Integrated_Security};" +
                                            $"MultipleActiveResultSets=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.2-servicing-10034");

            modelBuilder.Entity<FieldMappings>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.AdiAppType)
                    .IsRequired()
                    .HasColumnName("ADI_App_Type")
                    .HasMaxLength(3);

                entity.Property(e => e.AdiElement)
                    .IsRequired()
                    .HasColumnName("ADI_Element")
                    .HasMaxLength(50);

                entity.Property(e => e.ItvElement)
                    .IsRequired()
                    .HasColumnName("ITV_Element")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<ProviderContentTierMapping>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Distributor)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.ProviderContentTier)
                    .IsRequired()
                    .HasColumnName("Provider_Content_Tier")
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<ReportClassMapping>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ReportingClass)
                    .IsRequired()
                    .HasColumnName("Reporting_Class")
                    .HasMaxLength(50);

                entity.Property(e => e.FolderLocation)
                    .IsRequired()
                    .HasColumnName("ClassIncludes");

                entity.Property(e => e.FolderLocation)
                    .IsRequired()
                    .HasColumnName("Folder_Location")
                    .HasMaxLength(50);

                entity.Property(e => e.ShowType)
                   .IsRequired()
                   .HasColumnName("ShowType")
                   .HasMaxLength(50);
            });
        }
    }
}
