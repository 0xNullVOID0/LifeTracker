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
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityWatchEvents", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "BuienradarStationMeasurements",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StationId = table.Column<int>(type: "integer", nullable: false),
                    StationName = table.Column<string>(type: "text", nullable: false),
                    WindspeedBft = table.Column<float>(type: "real", nullable: true),
                    AirPressure = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    Humidity = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuienradarStationMeasurements", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DailyHeartRate",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    RestingRate = table.Column<int>(type: "integer", nullable: false),
                    Min = table.Column<int>(type: "integer", nullable: false),
                    Max = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyHeartRate", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "DailySleep",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SleepTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    DeepSleepSeconds = table.Column<int>(type: "integer", nullable: false),
                    LightSleepSeconds = table.Column<int>(type: "integer", nullable: false),
                    RemSleepSeconds = table.Column<int>(type: "integer", nullable: false),
                    AwakeSleepSeconds = table.Column<int>(type: "integer", nullable: false),
                    AvgHeartRate = table.Column<int>(type: "integer", nullable: false),
                    AvgSleepStress = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySleep", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "DailyStress",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Average = table.Column<int>(type: "integer", nullable: false),
                    Max = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyStress", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "RoomClimateMeasurements",
                columns: table => new
                {
                    ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CO2 = table.Column<int>(type: "integer", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    Humidity = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomClimateMeasurements", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "HeartRateSample",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BPM = table.Column<int>(type: "integer", nullable: false),
                    Sleeping = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeartRateSample", x => new { x.Date, x.Timestamp });
                    table.ForeignKey(
                        name: "FK_HeartRateSample_DailyHeartRate_Date",
                        column: x => x.Date,
                        principalTable: "DailyHeartRate",
                        principalColumn: "Date",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityWatchEvents_Timestamp",
                table: "ActivityWatchEvents",
                column: "Timestamp",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityWatchEvents");

            migrationBuilder.DropTable(
                name: "BuienradarStationMeasurements");

            migrationBuilder.DropTable(
                name: "DailySleep");

            migrationBuilder.DropTable(
                name: "DailyStress");

            migrationBuilder.DropTable(
                name: "HeartRateSample");

            migrationBuilder.DropTable(
                name: "RoomClimateMeasurements");

            migrationBuilder.DropTable(
                name: "DailyHeartRate");
        }
    }
}
