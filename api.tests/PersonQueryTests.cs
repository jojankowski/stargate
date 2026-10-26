using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Queries;

namespace StargateAPI.Tests
{
    public class GetPersonByNameTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private async Task<GetPersonByNameResult> Get(string name)
        {
            using var context = _db.NewContext();
            return await new GetPersonByNameHandler(context).Handle(new GetPersonByName { Name = name }, CancellationToken.None);
        }

        [Fact]
        public async Task Returns_the_person_and_their_current_duty()
        {
            await _db.AddPerson("John Doe");
            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01");

            var result = await Get("john doe");

            Assert.Equal("John Doe", result.Person!.Name);
            Assert.Equal("Pilot", result.Person.CurrentDutyTitle);
        }

        [Fact]
        public async Task Unknown_person_is_a_404()
        {
            var result = await Get("Nobody");

            Assert.False(result.Success);
            Assert.Equal(StatusCodes.Status404NotFound, result.ResponseCode);
        }

        [Fact]
        public async Task Handles_an_apostrophe_in_the_name()
        {
            await _db.AddPerson("Pat O'Brien");

            var result = await Get("Pat O'Brien");

            Assert.Equal("Pat O'Brien", result.Person!.Name);
        }

        [Fact]
        public async Task Treats_sql_in_the_name_as_text()
        {
            await _db.AddPerson("John Doe");

            var result = await Get("x' OR '1'='1");

            Assert.Equal(StatusCodes.Status404NotFound, result.ResponseCode);
        }
    }

    public class GetAstronautDutiesByNameTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private async Task<GetAstronautDutiesByNameResult> Get(string name)
        {
            using var context = _db.NewContext();
            return await new GetAstronautDutiesByNameHandler(context).Handle(new GetAstronautDutiesByName { Name = name }, CancellationToken.None);
        }

        [Fact]
        public async Task Returns_only_their_duties_newest_first()
        {
            await _db.AddPerson("John Doe");
            await _db.AddPerson("Jane Doe");
            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01");
            await _db.AddDuty("John Doe", "CPT", "Commander", "2022-03-15");
            await _db.AddDuty("Jane Doe", "1LT", "Engineer", "2021-01-01");

            var result = await Get("John Doe");

            Assert.Equal(new[] { "Commander", "Pilot" }, result.AstronautDuties.Select(x => x.DutyTitle));
        }

        [Fact]
        public async Task Unknown_person_is_a_404()
        {
            var result = await Get("Nobody");

            Assert.Equal(StatusCodes.Status404NotFound, result.ResponseCode);
        }
    }
}
