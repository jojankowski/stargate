using Dapper;
using StargateAPI.Business.Data;

namespace StargateAPI.Business.Behaviors
{
    public class ProcessLogWriter
    {
        private readonly StargateContext _context;
        private readonly ILogger<ProcessLogWriter> _logger;

        public ProcessLogWriter(StargateContext context, ILogger<ProcessLogWriter> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Write(string outcome, string requestName, string? requestData, string message, Exception? exception = null)
        {
            try
            {
                // dapper instead of EF - if SaveChanges just failed, those changes are still tracked
                // and EF would try to save them again along with the log
                const string sql = "INSERT INTO [ProcessLog] (CreatedAt, Outcome, RequestName, RequestData, Message, Exception) " +
                                   "VALUES (@CreatedAt, @Outcome, @RequestName, @RequestData, @Message, @Exception)";

                await _context.Connection.ExecuteAsync(sql, new ProcessLog
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    Outcome = outcome,
                    RequestName = requestName,
                    RequestData = requestData,
                    Message = message,
                    Exception = exception?.ToString()
                });
            }
            catch (Exception ex)
            {
                // don't fail the request just because logging failed
                _logger.LogError(ex, "Could not write process log for {RequestName}", requestName);
            }
        }
    }
}
