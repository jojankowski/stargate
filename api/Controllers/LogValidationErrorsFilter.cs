using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StargateAPI.Business.Behaviors;
using StargateAPI.Business.Data;
using System.Text.Json;

namespace StargateAPI.Controllers
{
    // [ApiController] rejects bad request bodies before the action runs, so they never hit
    // LoggingBehavior. this logs them instead
    public class LogValidationErrorsFilter : IAsyncAlwaysRunResultFilter
    {
        private readonly ProcessLogWriter _log;

        public LogValidationErrorsFilter(ProcessLogWriter log)
        {
            _log = log;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is ObjectResult { Value: ValidationProblemDetails problem })
            {
                var action = context.ActionDescriptor.RouteValues["action"] ?? "Unknown";
                var errors = problem.Errors.SelectMany(x => x.Value);

                await _log.Write(ProcessLogOutcomes.Rejected, action, JsonSerializer.Serialize(problem.Errors), $"Invalid request: {string.Join(" ", errors)}");
            }

            await next();
        }
    }
}
