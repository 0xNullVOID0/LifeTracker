using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LifeTracker.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityWatchEvents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AwID = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<double>(type: "double precision", nullable: false),
                    App = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityWatchEvents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DailyHeartRate",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    RestingRate = table.Column<int>(type: "integer", nullable: true),
                    Min = table.Column<int>(type: "integer", nullable: true),
                    Max = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyHeartRate", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "DailyStress",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Average = table.Column<int>(type: "integer", nullable: true),
                    Max = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyStress", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "WeatherLogs",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StationId = table.Column<int>(type: "integer", nullable: false),
                    StationName = table.Column<string>(type: "text", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: true),
                    Humidity = table.Column<float>(type: "real", nullable: true),
                    WindspeedBft = table.Column<float>(type: "real", nullable: true),
                    AirPressure = table.Column<float>(type: "real", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherLogs", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HeartRateSample",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DailyHeartRateDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Bpm = table.Column<int>(type: "integer", nullable: false),
                    Sleeping = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeartRateSample", x => new { x.Date, x.Timestamp });
                    table.ForeignKey(
                        name: "FK_HeartRateSample_DailyHeartRate_DailyHeartRateDate",
                        column: x => x.DailyHeartRateDate,
                        principalTable: "DailyHeartRate",
                        principalColumn: "Date",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityWatchEvents_Timestamp",
                table: "ActivityWatchEvents",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_HeartRateSample_DailyHeartRateDate",
                table: "HeartRateSample",
                column: "DailyHeartRateDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityWatchEvents");

            migrationBuilder.DropTable(
                name: "DailyStress");

            migrationBuilder.DropTable(
                name: "HeartRateSample");

            migrationBuilder.DropTable(
                name: "WeatherLogs");

            migrationBuilder.DropTable(
                name: "DailyHeartRate");
        }
    }
}
