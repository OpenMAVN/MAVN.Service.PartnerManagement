﻿using System.Data.Common;
using JetBrains.Annotations;
using MAVN.Persistence.PostgreSQL.Legacy;
using MAVN.Service.PartnerManagement.MsSqlRepositories.Entities;
using Microsoft.EntityFrameworkCore;

namespace MAVN.Service.PartnerManagement.MsSqlRepositories
{
    public class PartnerManagementContext : PostgreSQLContext
    {
        private const string Schema = "partner_management";

        public DbSet<PartnerEntity> Partners { get; set; }
        public DbSet<LocationEntity> Locations { get; set; }
        public DbSet<PartnerLinkingInfoEntity> PartnersLinkingInfo { get; set; }
        public DbSet<LinkedPartnerEntity> LinkedPartners { get; set; }

        public PartnerManagementContext(DbContextOptions contextOptions)
            : base(Schema, contextOptions)
        {
        }

        // C-tor for EF migrations
        [UsedImplicitly]
        public PartnerManagementContext()
            : base(Schema)
        {
        }

        public PartnerManagementContext(string connectionString, bool isTraceEnabled)
            : base(Schema, connectionString, isTraceEnabled)
        {
        }

        public PartnerManagementContext(DbContextOptions options, bool isForMocks = false)
            : base(Schema, options, isForMocks)
        {
        }

        public PartnerManagementContext(DbConnection dbConnection)
            : base(Schema, dbConnection)
        {
        }

        protected override void OnMAVNModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PartnerEntity>()
                .HasMany(e => e.Locations);

            modelBuilder.Entity<PartnerEntity>()
                .HasIndex(p => p.ClientId);

            modelBuilder.Entity<PartnerEntity>()
                .HasIndex(p => p.BusinessVertical);

            modelBuilder.Entity<LocationEntity>()
                .HasIndex(p => p.ExternalId)
                .IsUnique(false);

            modelBuilder.Entity<LocationEntity>()
                .Property(x => x.AccountingIntegrationCode)
                .ValueGeneratedNever()
                .HasDefaultValue("000000")
                .HasMaxLength(80);

            modelBuilder.Entity<LocationEntity>()
                .HasIndex(p => p.Geohash)
                .IsUnique(false);

            modelBuilder.Entity<LocationEntity>()
                .HasIndex(l => l.CountryIso3Code)
                .IsUnique(false);

            modelBuilder.Entity<PartnerLinkingInfoEntity>()
                .HasIndex(p => p.PartnerCode)
                .IsUnique();

            modelBuilder.Entity<LinkedPartnerEntity>()
                .HasIndex(p => p.PartnerId)
                .IsUnique(false);
        }
    }
}
