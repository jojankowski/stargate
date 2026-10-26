using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Commands;

namespace StargateAPI.Tests
{
    public class CreateAstronautDutyPreProcessorTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private async Task<BadHttpRequestException> AssertRejected(string name, string rank, string dutyTitle, string? startDate)
        {
            using var context = _db.NewContext();
            var request = new CreateAstronautDuty
            {
                Name = name,
                Rank = rank,
                DutyTitle = dutyTitle,
                DutyStartDate = startDate is null ? default : TestDatabase.Date(startDate)
            };

            return await Assert.ThrowsAsync<BadHttpRequestException>(() =>
                new CreateAstronautDutyPreProcessor(context).Process(request, CancellationToken.None));
        }

        [Fact]
        public async Task Unknown_person_is_a_404()
        {
            var ex = await AssertRejected("Nobody", "1LT", "Pilot", "2020-01-01");

            Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
        }

        [Theory]
        [InlineData("", "Pilot", "2020-01-01")]
        [InlineData("1LT", " ", "2020-01-01")]
        [InlineData("1LT", "Pilot", null)]
        public async Task Rank_title_and_start_date_are_required(string rank, string dutyTitle, string? startDate)
        {
            await _db.AddPerson("John Doe");

            var ex = await AssertRejected("John Doe", rank, dutyTitle, startDate);

            Assert.Equal("Rank, duty title and start date are required.", ex.Message);
        }

        [Fact]
        public async Task First_duty_cannot_be_retired()
        {
            await _db.AddPerson("John Doe");

            var ex = await AssertRejected("John Doe", "1LT", "RETIRED", "2020-01-01");

            Assert.Contains("first duty", ex.Message);
        }

        [Fact]
        public async Task No_new_duties_after_retiring()
        {
            await _db.AddPerson("John Doe");
            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01");
            await _db.AddDuty("John Doe", "CPT", "RETIRED", "2025-07-01");

            var ex = await AssertRejected("John Doe", "MAJ", "Commander", "2026-01-01");

            Assert.Contains("retired on 2025-07-01", ex.Message);
        }

        [Theory]
        [InlineData("2019-06-01")]
        [InlineData("2020-01-01")]
        public async Task New_duty_must_start_after_the_current_one(string startDate)
        {
            await _db.AddPerson("John Doe");
            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01");

            var ex = await AssertRejected("John Doe", "1LT", "Commander", startDate);

            Assert.Contains("must start after 2020-01-01", ex.Message);
        }

        [Fact]
        public async Task Two_people_can_have_the_same_duty_on_the_same_day()
        {
            // old duplicate check looked at everyone's duties so this used to fail
            await _db.AddPerson("John Doe");
            await _db.AddPerson("Jane Doe");
            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01");

            var result = await _db.AddDuty("Jane Doe", "1LT", "Pilot", "2020-01-01");

            Assert.True(result.Success);
        }
    }
}
