using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SCH_CONFIG;

namespace ITV2ADI_Engine.ITV2ADI_Database
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
        public virtual DbSet<ItvConversionData> ItvConversionData { get; set; }
        public virtual DbSet<Itvfilter> Itvfilter { get; set; }
        public virtual DbSet<MediaLocations> MediaLocations { get; set; }
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
            modelBuilder.HasAnnotation("ProductVersion", "2.2.4-servicing-10062");

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

            modelBuilder.Entity<ItvConversionData>(entity =>
            {
                entity.ToTable("ITV_Conversion_Data");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.IsTvod).HasColumnName("IsTVOD");

                entity.Property(e => e.LicenseEndDate).HasColumnType("datetime");

                entity.Property(e => e.LicenseStartDate).HasColumnType("datetime");

                entity.Property(e => e.MediaChecksum).HasMaxLength(250);

                entity.Property(e => e.MediaFileName).HasMaxLength(250);

                entity.Property(e => e.OriginalAdi)
                    .HasColumnName("Original_ADI")
                    .HasColumnType("xml");

                entity.Property(e => e.OriginalItv).HasColumnName("Original_ITV");

                entity.Property(e => e.Paid)
                    .HasColumnName("PAID")
                    .HasMaxLength(50);

                entity.Property(e => e.ProcessedDateTime).HasColumnType("datetime");

                entity.Property(e => e.ProviderId).HasMaxLength(50);

                entity.Property(e => e.ProviderName).HasMaxLength(50);

                entity.Property(e => e.PublicationDate).HasColumnType("datetime");

                entity.Property(e => e.Title).HasMaxLength(250);

                entity.Property(e => e.UpdateAdi)
                    .HasColumnName("Update_ADI")
                    .HasColumnType("xml");

                entity.Property(e => e.UpdatedDateTime).HasColumnType("datetime");

                entity.Property(e => e.UpdatedFileLocation).HasMaxLength(250);

                entity.Property(e => e.UpdatedFileName).HasMaxLength(250);

                entity.Property(e => e.UpdatedItv).HasColumnName("Updated_ITV");

                entity.Property(e => e.UpdatedMediaChecksum).HasMaxLength(250);
            });

            modelBuilder.Entity<Itvfilter>(entity =>
            {
                entity.ToTable("ITVFilter");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedNever();

                entity.Property(e => e.MatchString).IsRequired();

                entity.Property(e => e.MoveOnMatchDirectory).IsRequired();
            });

            modelBuilder.Entity<MediaLocations>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.MediaLocation).IsRequired();
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

                entity.Property(e => e.FolderLocation)
                    .IsRequired()
                    .HasColumnName("Folder_Location")
                    .HasMaxLength(50);

                entity.Property(e => e.ReportingClass)
                    .IsRequired()
                    .HasColumnName("Reporting_Class")
                    .HasMaxLength(50);

                entity.Property(e => e.ShowType)
                    .IsRequired()
                    .HasMaxLength(50);
            });
        }
    }
}
