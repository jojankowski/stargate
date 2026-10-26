using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StargateAPI.Business.Behaviors;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;

namespace StargateAPI.Tests
{
    public class LoggingBehaviorTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private static LoggingBehavior<TRequest, TResponse> Behavior<TRequest, TResponse>(StargateContext context)
            where TRequest : notnull
            => new(new ProcessLogWriter(context, NullLogger<ProcessLogWriter>.Instance));

        private async Task<ProcessLog> GetOnlyLog()
        {
            using var context = _db.NewContext();
            return await context.ProcessLogs.AsNoTracking().SingleAsync();
        }

        [Fact]
        public async Task Logs_a_success()
        {
            using var context = _db.NewContext();

            await Behavior<CreatePerson, CreatePersonResult>(context)
                .Handle(new CreatePerson { Name = "John Doe" }, () => Task.FromResult(new CreatePersonResult()), CancellationToken.None);

            var log = await GetOnlyLog();
            Assert.Equal(ProcessLogOutcomes.Success, log.Outcome);
            Assert.Equal("CreatePerson", log.RequestName);
            Assert.Contains("John Doe", log.RequestData);
        }

        [Fact]
        public async Task Logs_a_not_found_result_as_rejected()
        {
            using var context = _db.NewContext();
            var notFound = new GetPersonByNameResult { Success = false, Message = "No person named 'Nobody' was found.", ResponseCode = 404 };

            await Behavior<GetPersonByName, GetPersonByNameResult>(context)
                .Handle(new GetPersonByName { Name = "Nobody" }, () => Task.FromResult(notFound), CancellationToken.None);

            var log = await GetOnlyLog();
            Assert.Equal(ProcessLogOutcomes.Rejected, log.Outcome);
            Assert.Equal(notFound.Message, log.Message);
        }

        [Fact]
        public async Task Logs_a_broken_rule_as_rejected()
        {
            using var context = _db.NewContext();

            await Assert.ThrowsAsync<BadHttpRequestException>(() => Behavior<CreatePerson, CreatePersonResult>(context)
                .Handle(new CreatePerson { Name = "John Doe" }, () => throw new BadHttpRequestException("Already exists"), CancellationToken.None));

            var log = await GetOnlyLog();
            Assert.Equal(ProcessLogOutcomes.Rejected, log.Outcome);
            Assert.Equal("Already exists", log.Message);
        }

        [Fact]
        public async Task Logs_an_exception_as_an_error()
        {
            using var context = _db.NewContext();

            await Assert.ThrowsAsync<InvalidOperationException>(() => Behavior<GetPeople, GetPeopleResult>(context)
                .Handle(new GetPeople(), () => throw new InvalidOperationException("Something broke"), CancellationToken.None));

            var log = await GetOnlyLog();
            Assert.Equal(ProcessLogOutcomes.Error, log.Outcome);
            Assert.Contains("InvalidOperationException", log.Exception);
        }

        [Fact]
        public async Task Still_logs_when_a_save_has_just_failed()
        {
            // the failed insert is still tracked, so logging through EF would retry it and lose the log
            await _db.AddPerson("John Doe");
            using var context = _db.NewContext();

            await Assert.ThrowsAsync<DbUpdateException>(() => Behavior<CreatePerson, CreatePersonResult>(context)
                .Handle(new CreatePerson { Name = "John Doe" }, async () =>
                {
                    context.People.Add(new Person { Name = "John Doe" });
                    await context.SaveChangesAsync();
                    return new CreatePersonResult();
                }, CancellationToken.None));

            Assert.Equal(ProcessLogOutcomes.Error, (await GetOnlyLog()).Outcome);
        }
    }
}
