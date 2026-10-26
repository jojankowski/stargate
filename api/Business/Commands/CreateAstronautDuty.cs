using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;
using System.Net;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        public required string Name { get; set; }

        public required string Rank { get; set; }

        public required string DutyTitle { get; set; }

        public DateTime DutyStartDate { get; set; }
    }

    public class CreateAstronautDutyPreProcessor : IRequestPreProcessor<CreateAstronautDuty>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        public Task Process(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Rank) || string.IsNullOrWhiteSpace(request.DutyTitle) || request.DutyStartDate == default)
                throw new BadHttpRequestException("Rank, duty title and start date are required.");

            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is null) throw new BadHttpRequestException($"No person named '{request.Name}' was found.", StatusCodes.Status404NotFound);

            var currentDuty = _context.AstronautDuties.AsNoTracking()
                .Where(z => z.PersonId == person.Id)
                .OrderByDescending(z => z.DutyStartDate)
                .FirstOrDefault();

            if (currentDuty is null)
            {
                // can't retire if they never served
                if (DutyTitles.IsRetired(request.DutyTitle))
                    throw new BadHttpRequestException($"{person.Name} has no duties yet, so their first duty can't be RETIRED.");

                return Task.CompletedTask;
            }

            if (DutyTitles.IsRetired(currentDuty.DutyTitle))
                throw new BadHttpRequestException($"{person.Name} retired on {currentDuty.DutyStartDate:yyyy-MM-dd} and can't take on new duties.");

            // previous duty ends the day before this one starts, so it has to be later (also catches duplicates)
            if (request.DutyStartDate.Date <= currentDuty.DutyStartDate)
                throw new BadHttpRequestException($"A new duty for {person.Name} must start after {currentDuty.DutyStartDate:yyyy-MM-dd}.");

            return Task.CompletedTask;
        }
    }

    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {

            var query = "SELECT * FROM [Person] WHERE @Name = Name";

            var person = await _context.Connection.QueryFirstAsync<Person>(query, new { request.Name });

            query = "SELECT * FROM [AstronautDetail] WHERE @PersonId = PersonId";

            var astronautDetail = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDetail>(query, new { PersonId = person.Id });

            if (astronautDetail == null)
            {
                astronautDetail = new AstronautDetail();
                astronautDetail.PersonId = person.Id;
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                astronautDetail.CareerStartDate = request.DutyStartDate.Date;

                await _context.AstronautDetails.AddAsync(astronautDetail);

            }
            else
            {
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                if (DutyTitles.IsRetired(request.DutyTitle))
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
                }
                _context.AstronautDetails.Update(astronautDetail);
            }

            query = "SELECT * FROM [AstronautDuty] WHERE @PersonId = PersonId Order By DutyStartDate Desc";

            var astronautDuty = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDuty>(query, new { PersonId = person.Id });

            if (astronautDuty != null)
            {
                astronautDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(astronautDuty);
            }

            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                Rank = request.Rank,
                DutyTitle = request.DutyTitle,
                DutyStartDate = request.DutyStartDate.Date,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDuty);

            await _context.SaveChangesAsync();

            return new CreateAstronautDutyResult()
            {
                Id = newAstronautDuty.Id
            };
        }
    }

    public class CreateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }

    public static class DutyTitles
    {
        public static bool IsRetired(string? dutyTitle) =>
            string.Equals(dutyTitle?.Trim(), "RETIRED", StringComparison.OrdinalIgnoreCase);
    }
}
