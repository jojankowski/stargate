using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;
using StargateAPI.Controllers;

namespace StargateAPI.Tests
{
    // fake MediatR so the controllers can be tested by themselves
    public class FakeMediator : IMediator
    {
        private readonly Func<object, object> _respond;

        public FakeMediator(Func<object, object> respond)
        {
            _respond = respond;
        }

        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)_respond(request));
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotImplementedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new NotImplementedException();
    }

    public class ControllerTests
    {
        private static BaseResponse AssertStatus(IActionResult actionResult, int status)
        {
            var result = Assert.IsType<ObjectResult>(actionResult);
            Assert.Equal(status, result.StatusCode);
            return Assert.IsAssignableFrom<BaseResponse>(result.Value);
        }

        [Fact]
        public async Task Broken_rule_is_a_400_with_its_message()
        {
            var controller = new PersonController(new FakeMediator(_ => throw new BadHttpRequestException("A person named 'John Doe' already exists.")));

            var body = AssertStatus(await controller.CreatePerson("John Doe"), StatusCodes.Status400BadRequest);

            Assert.Equal("A person named 'John Doe' already exists.", body.Message);
        }

        [Fact]
        public async Task Unexpected_error_is_a_500_without_details()
        {
            var controller = new PersonController(new FakeMediator(_ => throw new InvalidOperationException("SQLite Error 1: no such table: Person")));

            var body = AssertStatus(await controller.GetPeople(), StatusCodes.Status500InternalServerError);

            Assert.DoesNotContain("SQLite", body.Message);
        }

        [Fact]
        public async Task Duties_endpoint_asks_for_duties()
        {
            // this used to send GetPersonByName
            var mediator = new FakeMediator(_ => new GetAstronautDutiesByNameResult());

            await new AstronautDutyController(mediator).GetAstronautDutiesByName("John Doe");

            Assert.IsType<GetAstronautDutiesByName>(mediator.LastRequest);
        }
    }
}
