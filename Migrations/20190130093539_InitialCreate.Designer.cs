﻿// <auto-generated />
using System;
using Betty.databases.guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Betty.Migrations
{
    [DbContext(typeof(GuildDB))]
    [Migration("20190130093539_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028");

            modelBuilder.Entity("Betty.databases.guilds.ApplicationTB", b =>
                {
                    b.Property<ulong>("GuildID")
                        .ValueGeneratedOnAdd();

                    b.Property<ulong>("Channel");

                    b.Property<DateTime>("Deadline");

                    b.Property<ulong>("FK_Application_Guild");

                    b.Property<string>("InviteID");

                    b.HasKey("GuildID");

                    b.HasIndex("FK_Application_Guild")
                        .IsUnique();

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("Betty.databases.guilds.EventNotificationTB", b =>
                {
                    b.Property<ulong>("NotificationID")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("DateTime");

                    b.Property<ulong?>("FK_EventNotification_Event");

                    b.Property<string>("ResponseKeyword");

                    b.HasKey("NotificationID");

                    b.HasIndex("FK_EventNotification_Event");

                    b.ToTable("EventNotifications");
                });

            modelBuilder.Entity("Betty.databases.guilds.EventTB", b =>
                {
                    b.Property<ulong>("EventID")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Date");

                    b.Property<bool>("DoNotifications");

                    b.Property<ulong?>("FK_Event_Guild");

                    b.Property<string>("Name");

                    b.Property<ulong>("NotificationChannel");

                    b.HasKey("EventID");

                    b.HasIndex("FK_Event_Guild");

                    b.ToTable("Events");
                });

            modelBuilder.Entity("Betty.databases.guilds.GuildTB", b =>
                {
                    b.Property<ulong>("GuildID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Language");

                    b.Property<string>("Name");

                    b.Property<ulong?>("Notification");

                    b.Property<ulong?>("Public");

                    b.HasKey("GuildID");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("Betty.databases.guilds.ApplicationTB", b =>
                {
                    b.HasOne("Betty.databases.guilds.GuildTB", "Guild")
                        .WithOne("Application")
                        .HasForeignKey("Betty.databases.guilds.ApplicationTB", "FK_Application_Guild")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Betty.databases.guilds.EventNotificationTB", b =>
                {
                    b.HasOne("Betty.databases.guilds.EventTB", "Event")
                        .WithMany("Notifications")
                        .HasForeignKey("FK_EventNotification_Event");
                });

            modelBuilder.Entity("Betty.databases.guilds.EventTB", b =>
                {
                    b.HasOne("Betty.databases.guilds.GuildTB", "Guild")
                        .WithMany("Events")
                        .HasForeignKey("FK_Event_Guild");
                });
#pragma warning restore 612, 618
        }
    }
}
