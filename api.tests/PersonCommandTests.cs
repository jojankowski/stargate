using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;

namespace StargateAPI.Tests
{
    public class CreatePersonTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        [Fact]
        public async Task Saves_the_name_trimmed()
        {
            await _db.AddPerson("  John Doe  ");

            using var context = _db.NewContext();
            Assert.Equal("John Doe", (await context.People.SingleAsync()).Name);
        }

        [Fact]
        public async Task Rejects_a_duplicate_name_in_any_case()
        {
            await _db.AddPerson("John Doe");

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => _db.AddPerson("john doe"));

            Assert.Contains("already exists", ex.Message);
        }

        [Fact]
        public async Task Rejects_a_blank_name()
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => _db.AddPerson("   "));

            Assert.Equal("A name is required.", ex.Message);
        }

        [Fact]
        public async Task Database_blocks_duplicates_even_without_the_pre_processor()
        {
            // two requests at once could both get past the pre-processor, the index is what actually stops it
            await _db.AddPerson("John Doe");

            using var context = _db.NewContext();
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                new CreatePersonHandler(context).Handle(new CreatePerson { Name = "JOHN DOE" }, CancellationToken.None));
        }
    }

    public class UpdatePersonTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose() => _db.Dispose();

        private async Task Rename(string name, string newName)
        {
            using var context = _db.NewContext();
            var request = new UpdatePerson { Name = name, NewName = newName };

            await new UpdatePersonPreProcessor(context).Process(request, CancellationToken.None);
            await new UpdatePersonHandler(context).Handle(request, CancellationToken.None);
        }

        [Fact]
        public async Task Renames_and_keeps_their_duties()
        {
            await _db.AddPerson("John Doe");
            await _db.AddDuty("John Doe", "1LT", "Pilot", "2020-01-01");

            await Rename("John Doe", "John A. Doe");

            using var context = _db.NewContext();
            var person = await context.People.Include(x => x.AstronautDuties).SingleAsync();
            Assert.Equal("John A. Doe", person.Name);
            Assert.Single(person.AstronautDuties);
        }

        [Fact]
        public async Task Unknown_person_is_a_404()
        {
            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => Rename("Nobody", "Somebody"));

            Assert.Equal(StatusCodes.Status404NotFound, ex.StatusCode);
        }

        [Fact]
        public async Task Rejects_a_name_someone_else_has()
        {
            await _db.AddPerson("John Doe");
            await _db.AddPerson("Jane Doe");

            var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() => Rename("Jane Doe", "john doe"));

            Assert.Contains("already exists", ex.Message);
        }

        [Fact]
        public async Task Can_fix_the_casing_of_your_own_name()
        {
            await _db.AddPerson("jane doe");

            await Rename("jane doe", "Jane Doe");

            using var context = _db.NewContext();
            Assert.Equal("Jane Doe", (await context.People.SingleAsync()).Name);
        }
    }
}
