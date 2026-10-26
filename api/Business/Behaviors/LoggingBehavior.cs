using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Text.Json;

namespace StargateAPI.Business.Behaviors
{
    // logs every MediatR request to the ProcessLog table
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ProcessLogWriter _log;

        public LoggingBehavior(ProcessLogWriter log)
        {
            _log = log;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestData = JsonSerializer.Serialize(request);

            try
            {
                var response = await next();

                // handler sent back an error, like a 404
                if (response is BaseResponse { Success: false } failed)
                    await _log.Write(ProcessLogOutcomes.Rejected, requestName, requestData, failed.Message);
                else
                    await _log.Write(ProcessLogOutcomes.Success, requestName, requestData, $"{requestName} succeeded");

                return response;
            }
            catch (BadHttpRequestException ex)
            {
                await _log.Write(ProcessLogOutcomes.Rejected, requestName, requestData, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                await _log.Write(ProcessLogOutcomes.Error, requestName, requestData, $"{requestName} failed: {ex.Message}", ex);
                throw;
            }
        }
    }
}
