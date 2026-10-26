using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using System.Globalization;

namespace StargateAPI.Tests
{
    // in-memory sqlite built from the real migrations. not using EF InMemory since it
    // can't run the dapper queries and ignores unique indexes
    public class TestDatabase : IDisposable
    {
        // db only exists while this connection is open
        private readonly SqliteConnection _connection;

        public TestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            using var context = NewContext();
            context.Database.Migrate();
        }

        // new context per command, same as the API does per request
        public StargateContext NewContext()
        {
            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(_connection)
                .Options;

            return new StargateContext(options);
        }

        public async Task<CreatePersonResult> AddPerson(string name)
        {
            using var context = NewContext();
            var request = new CreatePerson { Name = name };

            await new CreatePersonPreProcessor(context).Process(request, CancellationToken.None);
            return await new CreatePersonHandler(context).Handle(request, CancellationToken.None);
        }

        public async Task<CreateAstronautDutyResult> AddDuty(string name, string rank, string dutyTitle, string startDate)
        {
            using var context = NewContext();
            var request = new CreateAstronautDuty
            {
                Name = name,
                Rank = rank,
                DutyTitle = dutyTitle,
                DutyStartDate = Date(startDate)
            };

            await new CreateAstronautDutyPreProcessor(context).Process(request, CancellationToken.None);
            return await new CreateAstronautDutyHandler(context).Handle(request, CancellationToken.None);
        }

        public static DateTime Date(string value) => DateTime.Parse(value, CultureInfo.InvariantCulture);

        public void Dispose() => _connection.Dispose();
    }
}
