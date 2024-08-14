﻿// <auto-generated />
using MarketBasketAnalysis.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace MarketBasketAnalysis.Server.Data.Migrations
{
    [DbContext(typeof(MarketBasketAnalysisDbContext))]
    [Migration("20240814185321_AddAssociationRuleEntities")]
    partial class AddAssociationRuleEntities
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.7");

            modelBuilder.Entity("MarketBasketAnalysis.Server.Data.AssociationRuleChunk", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AssociationRuleSetId")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Data")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<int>("PayloadSize")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AssociationRuleSetId");

                    b.ToTable("AssociationRuleChunks");
                });

            modelBuilder.Entity("MarketBasketAnalysis.Server.Data.AssociationRuleSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsLoaded")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("TransactionCount")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("AssociationRuleSets");
                });

            modelBuilder.Entity("MarketBasketAnalysis.Server.Data.ItemChunk", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AssociationRuleSetId")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Data")
                        .IsRequired()
                        .HasColumnType("BLOB");

                    b.Property<int>("PayloadSize")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AssociationRuleSetId");

                    b.ToTable("ItemChunks");
                });

            modelBuilder.Entity("MarketBasketAnalysis.Server.Data.AssociationRuleChunk", b =>
                {
                    b.HasOne("MarketBasketAnalysis.Server.Data.AssociationRuleSet", "AssociationRuleSet")
                        .WithMany("AssociationRuleChunks")
                        .HasForeignKey("AssociationRuleSetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AssociationRuleSet");
                });

            modelBuilder.Entity("MarketBasketAnalysis.Server.Data.ItemChunk", b =>
                {
                    b.HasOne("MarketBasketAnalysis.Server.Data.AssociationRuleSet", "AssociationRuleSet")
                        .WithMany("ItemChunks")
                        .HasForeignKey("AssociationRuleSetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AssociationRuleSet");
                });

            modelBuilder.Entity("MarketBasketAnalysis.Server.Data.AssociationRuleSet", b =>
                {
                    b.Navigation("AssociationRuleChunks");

                    b.Navigation("ItemChunks");
                });
#pragma warning restore 612, 618
        }
    }
}
