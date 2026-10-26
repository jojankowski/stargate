using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;

namespace StargateAPI.Tests
{
    public class CreateAstronautDutyHandlerTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private async Task<AstronautDetail> GetDetail()
        {
            using var context = _db.NewContext();
            return await context.AstronautDetails.AsNoTracking().SingleAsync();
        }

        private async Task<List<AstronautDuty>> GetDuties()
        {
            using var context = _db.NewContext();
            return await context.AstronautDuties.AsNoTracking().OrderBy(x => x.DutyStartDate).ToListAsync();
        }

        [Fact]
        public async Task First_duty_starts_the_career()
        {
            await _db.AddPerson("John Doe");

            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01");

            var detail = await GetDetail();
            Assert.Equal("1LT", detail.CurrentRank);
            Assert.Equal("Pilot", detail.CurrentDutyTitle);
            Assert.Equal(TestDatabase.Date("2020-01-01"), detail.CareerStartDate);
            Assert.Null(detail.CareerEndDate);
        }

        [Fact]
        public async Task New_duty_ends_the_previous_one_the_day_before()
        {
            await _db.AddPerson("John Doe");
            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01");

            await _db.AddDuty("John Doe", "CPT", "Commander", "2022-03-15");

            var duties = await GetDuties();
            Assert.Equal(TestDatabase.Date("2022-03-14"), duties[0].DutyEndDate);
            Assert.Null(duties[1].DutyEndDate);

            var detail = await GetDetail();
            Assert.Equal("CPT", detail.CurrentRank);
            Assert.Equal("Commander", detail.CurrentDutyTitle);
        }

        [Fact]
        public async Task Retiring_ends_the_career_the_day_before()
        {
            await _db.AddPerson("John Doe");
            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01");

            await _db.AddDuty("John Doe", "CPT", "retired", "2025-07-01");

            var detail = await GetDetail();
            Assert.Equal(TestDatabase.Date("2025-06-30"), detail.CareerEndDate);
        }

        [Fact]
        public async Task Time_of_day_is_dropped()
        {
            await _db.AddPerson("John Doe");

            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01T15:30:00");

            var duty = Assert.Single(await GetDuties());
            Assert.Equal(TestDatabase.Date("2020-01-01"), duty.DutyStartDate);
        }

        [Theory]
        [InlineData("RETIRED", true)]
        [InlineData(" Retired ", true)]
        [InlineData("Commander", false)]
        public void IsRetired_ignores_case_and_spaces(string dutyTitle, bool expected)
        {
            Assert.Equal(expected, DutyTitles.IsRetired(dutyTitle));
        }
    }
}
