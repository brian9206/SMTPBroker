// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SMTPBroker.Persistence;

#nullable disable

namespace SMTPBroker.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20220314143735_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.3");

            modelBuilder.Entity("SMTPBroker.Models.Message", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DatedAt")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ExpireAt")
                        .HasColumnType("TEXT");

                    b.Property<string>("HTMLBody")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TextBody")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("SMTPBroker.Models.Message", b =>
                {
                    b.OwnsMany("SMTPBroker.Models.Attachment", "Attachments", b1 =>
                        {
                            b1.Property<Guid>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("TEXT");

                            b1.Property<string>("FileName")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<Guid>("MessageId")
                                .HasColumnType("TEXT");

                            b1.Property<string>("MimeType")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.HasKey("Id");

                            b1.HasIndex("MessageId");

                            b1.ToTable("Attachment");

                            b1.WithOwner()
                                .HasForeignKey("MessageId");
                        });

                    b.OwnsMany("SMTPBroker.Models.Address", "From", b1 =>
                        {
                            b1.Property<Guid>("MessageId")
                                .HasColumnType("TEXT");

                            b1.Property<Guid>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("TEXT");

                            b1.Property<string>("Email")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.HasKey("MessageId", "Id");

                            b1.ToTable("MessageFromAddress", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("MessageId");
                        });

                    b.OwnsMany("SMTPBroker.Models.Address", "To", b1 =>
                        {
                            b1.Property<Guid>("MessageId")
                                .HasColumnType("TEXT");

                            b1.Property<Guid>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("TEXT");

                            b1.Property<string>("Email")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.Property<string>("Name")
                                .IsRequired()
                                .HasColumnType("TEXT");

                            b1.HasKey("MessageId", "Id");

                            b1.ToTable("MessageToAddress", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("MessageId");
                        });

                    b.Navigation("Attachments");

                    b.Navigation("From");

                    b.Navigation("To");
                });
#pragma warning restore 612, 618
        }
    }
}
