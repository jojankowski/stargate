using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StargateAPI.Business.Behaviors;
using StargateAPI.Controllers;

namespace StargateAPI.Tests
{
    public class LogValidationErrorsFilterTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private async Task RunFilter(IActionResult result)
        {
            using var context = _db.NewContext();
            var filter = new LogValidationErrorsFilter(new ProcessLogWriter(context, NullLogger<ProcessLogWriter>.Instance));

            var actionDescriptor = new ActionDescriptor();
            actionDescriptor.RouteValues["action"] = "CreatePerson";
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), actionDescriptor);
            var filters = new List<IFilterMetadata>();

            await filter.OnResultExecutionAsync(
                new ResultExecutingContext(actionContext, filters, result, new object()),
                () => Task.FromResult(new ResultExecutedContext(actionContext, filters, result, new object())));
        }

        private async Task<int> LogCount()
        {
            using var context = _db.NewContext();
            return await context.ProcessLogs.CountAsync();
        }

        [Fact]
        public async Task Logs_a_validation_error()
        {
            var problem = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [""] = new[] { "A non-empty request body is required." }
            });

            await RunFilter(new BadRequestObjectResult(problem));

            using var context = _db.NewContext();
            var log = await context.ProcessLogs.SingleAsync();
            Assert.Equal("CreatePerson", log.RequestName);
            Assert.Equal("Invalid request: A non-empty request body is required.", log.Message);
        }

        [Fact]
        public async Task Ignores_other_responses()
        {
            await RunFilter(new OkObjectResult(new BaseResponse()));

            Assert.Equal(0, await LogCount());
        }
    }
}
