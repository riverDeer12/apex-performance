using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPerformance.API.Database.Migrations
{
    /// <summary>
    /// Reads see the last committed version of rows instead of waiting for locks
    /// (row versioning in tempdb). A long read, e.g. GET api/trainings, can then
    /// no longer deadlock with a training being saved at the same time.
    /// Changing it needs a moment without other connections: ROLLBACK IMMEDIATE
    /// ends their open statements, which the API answers like any brief outage.
    /// ALTER DATABASE can't run in a transaction, so the statement has none.
    /// </summary>
    public partial class Enabledreadcommittedsnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql(Set("ON"), suppressTransaction: true);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql(Set("OFF"), suppressTransaction: true);

        // For the database of the connection, whatever its name is on test or production.
        private static string Set(string value) => $"""
            IF (SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = DB_NAME()) <> {(value == "ON" ? 1 : 0)}
            BEGIN
                DECLARE @sql nvarchar(400) = N'ALTER DATABASE ' + QUOTENAME(DB_NAME()) +
                    N' SET READ_COMMITTED_SNAPSHOT {value} WITH ROLLBACK IMMEDIATE';
                EXEC (@sql);
            END
            """;
    }
}
