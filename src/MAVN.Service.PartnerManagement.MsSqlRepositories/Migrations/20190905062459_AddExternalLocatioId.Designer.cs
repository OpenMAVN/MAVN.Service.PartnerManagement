// <auto-generated />
using System;
using MAVN.Service.PartnerManagement.MsSqlRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MAVN.Service.PartnerManagement.MsSqlRepositories.Migrations
{
    [DbContext(typeof(PartnerManagementContext))]
    [Migration("20190905062459_AddExternalLocatioId")]
    partial class AddExternalLocatioId
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("partner_management")
                .HasAnnotation("ProductVersion", "2.2.4-servicing-10062")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.PartnerEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<int>("BusinessVertical")
                        .HasColumnName("business_vertical");

                    b.Property<string>("ClientId")
                        .HasColumnName("client_id");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnName("created_at");

                    b.Property<Guid>("CreatedBy")
                        .HasColumnName("created_by");

                    b.Property<int>("CurrencyRate")
                        .HasColumnName("currency_rate");

                    b.Property<string>("Description")
                        .HasColumnName("description");

                    b.Property<string>("Name")
                        .HasColumnName("name");

                    b.Property<int>("TokensRate")
                        .HasColumnName("tokens_rate");

                    b.HasKey("Id");

                    b.HasIndex("BusinessVertical");

                    b.HasIndex("ClientId");

                    b.ToTable("partner");
                });

            modelBuilder.Entity("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.PartnerEntity", b =>
                {
                    b.OwnsMany("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.LocationEntity", "Locations", b1 =>
                        {
                            b1.Property<Guid>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnName("id");

                            b1.Property<string>("Address")
                                .HasColumnName("address");

                            b1.Property<DateTime>("CreatedAt")
                                .HasColumnName("created_at");

                            b1.Property<Guid>("CreatedBy")
                                .HasColumnName("created_by");

                            b1.Property<string>("ExternalId")
                                .HasColumnName("external_id");

                            b1.Property<string>("Name")
                                .HasColumnName("name");

                            b1.Property<Guid>("PartnerId")
                                .HasColumnName("partner_id");

                            b1.HasKey("Id");

                            b1.HasIndex("PartnerId");

                            b1.ToTable("location");

                            b1.HasOne("MAVN.Service.PartnerManagement.MsSqlRepositories.Entities.PartnerEntity", "Partner")
                                .WithMany("Locations")
                                .HasForeignKey("PartnerId")
                                .OnDelete(DeleteBehavior.Cascade);
                        });
                });
#pragma warning restore 612, 618
        }
    }
}
