using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.Business.Data
{
    [Table("ProcessLog")]
    public class ProcessLog
    {
        public int Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string Outcome { get; set; } = string.Empty;

        public string RequestName { get; set; } = string.Empty;

        public string? RequestData { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? Exception { get; set; }
    }

    public static class ProcessLogOutcomes
    {
        public const string Success = "Success";
        public const string Rejected = "Rejected";
        public const string Error = "Error";
    }

    public class ProcessLogConfiguration : IEntityTypeConfiguration<ProcessLog>
    {
        public void Configure(EntityTypeBuilder<ProcessLog> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        }
    }
}
