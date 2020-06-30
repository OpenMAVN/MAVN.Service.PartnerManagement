﻿// <auto-generated />
using System;
using MAVN.Service.PartnerManagement.MsSqlRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace MAVN.Service.PartnerManagement.MsSqlRepositories.Migrations
{
    [DbContext(typeof(PartnerManagementContext))]
    [Migration("20200630121221_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("partner_management")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.LinkedPartnerEntity", b =>
                {
                    b.Property<Guid>("CustomerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("customer_id")
                        .HasColumnType("uuid");

                    b.Property<Guid>("PartnerId")
                        .HasColumnName("partner_id")
                        .HasColumnType("uuid");

                    b.HasKey("CustomerId");

                    b.HasIndex("PartnerId");

                    b.ToTable("linked_partners");
                });

            modelBuilder.Entity("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.LocationEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid");

                    b.Property<string>("AccountingIntegrationCode")
                        .IsRequired()
                        .HasColumnName("accounting_integration_code")
                        .HasColumnType("character varying(80)")
                        .HasMaxLength(80)
                        .HasDefaultValue("000000");

                    b.Property<string>("Address")
                        .HasColumnName("address")
                        .HasColumnType("text");

                    b.Property<string>("CountryIso3Code")
                        .HasColumnName("сountry_iso3_code")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnName("created_at")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<string>("ExternalId")
                        .HasColumnName("external_id")
                        .HasColumnType("text");

                    b.Property<string>("Geohash")
                        .HasColumnName("geohash")
                        .HasColumnType("text");

                    b.Property<double?>("Latitude")
                        .HasColumnName("latitude")
                        .HasColumnType("double precision");

                    b.Property<double?>("Longitude")
                        .HasColumnName("longitude")
                        .HasColumnType("double precision");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<Guid>("PartnerId")
                        .HasColumnName("partner_id")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("CountryIso3Code");

                    b.HasIndex("ExternalId");

                    b.HasIndex("Geohash");

                    b.HasIndex("PartnerId");

                    b.ToTable("location");
                });

            modelBuilder.Entity("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.PartnerEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("uuid");

                    b.Property<decimal?>("AmountInCurrency")
                        .HasColumnName("amount_in_currency")
                        .HasColumnType("numeric");

                    b.Property<string>("AmountInTokens")
                        .HasColumnName("amount_in_tokens")
                        .HasColumnType("text");

                    b.Property<int>("BusinessVertical")
                        .HasColumnName("business_vertical")
                        .HasColumnType("integer");

                    b.Property<string>("ClientId")
                        .HasColumnName("client_id")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnName("created_at")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by")
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .HasColumnName("description")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnName("name")
                        .HasColumnType("text");

                    b.Property<bool>("UseGlobalCurrencyRate")
                        .HasColumnName("use_global_currency_rate")
                        .HasColumnType("boolean");

                    b.HasKey("Id");

                    b.HasIndex("BusinessVertical");

                    b.HasIndex("ClientId");

                    b.ToTable("partner");
                });

            modelBuilder.Entity("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.PartnerLinkingInfoEntity", b =>
                {
                    b.Property<Guid>("PartnerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("partner_id")
                        .HasColumnType("uuid");

                    b.Property<string>("PartnerCode")
                        .IsRequired()
                        .HasColumnName("partner_code")
                        .HasColumnType("text");

                    b.Property<string>("PartnerLinkingCode")
                        .IsRequired()
                        .HasColumnName("partner_linking_code")
                        .HasColumnType("text");

                    b.HasKey("PartnerId");

                    b.HasIndex("PartnerCode")
                        .IsUnique();

                    b.ToTable("partner_linking_info");
                });

            modelBuilder.Entity("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.LocationEntity", b =>
                {
                    b.HasOne("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.PartnerEntity", "Partner")
                        .WithMany("Locations")
                        .HasForeignKey("PartnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
