﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Phantom.Controller.Database;

#nullable disable

namespace Phantom.Controller.Database.Postgres.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20221007033307_Agents")]
    partial class Agents
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0-rc.1.22426.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Phantom.Controller.Database.Entities.AgentEntity", b =>
                {
                    b.Property<Guid>("AgentGuid")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("MaxInstances")
                        .HasColumnType("integer");

                    b.Property<ushort>("MaxMemory")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Version")
                        .HasColumnType("integer");

                    b.HasKey("AgentGuid");

                    b.ToTable("Agents", "agents");
                });
#pragma warning restore 612, 618
        }
    }
}
