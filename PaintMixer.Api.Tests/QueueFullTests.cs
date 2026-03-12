using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace PaintMixer.Api.Tests
{
    public class QueueFullWebApplicationFactory : CustomWebApplicationFactory
    { }
    public class QueueFullTests : IClassFixture<QueueFullWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public QueueFullTests(QueueFullWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task FillQueue()
        {
            for (int i = 0; i < 32; i++)
                await _client.PostAsJsonAsync("/jobs", new { red = 1 });
        }

        [Fact]
        public async Task SubmitJob_QueueFull_Returns422()
        {
            await FillQueue();

            var response = await _client.PostAsJsonAsync("/jobs", new { red = 1 });

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task QueryJob_QueueFull_StillReturnsExistingJobs()
        {
            var submit = await _client.PostAsJsonAsync("/jobs", new { red = 10 });
            var submitted = await submit.Content.ReadFromJsonAsync<JobSubmittedResponse>();

            await FillQueue();

            var response = await _client.GetAsync($"/jobs/{submitted!.JobCode}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CancelJob_QueueFull_StillAllowsCancellation()
        {
            var submit = await _client.PostAsJsonAsync("/jobs", new { red = 10 });
            var submitted = await submit.Content.ReadFromJsonAsync<JobSubmittedResponse>();

            await FillQueue();

            var response = await _client.DeleteAsync($"/jobs/{submitted!.JobCode}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
