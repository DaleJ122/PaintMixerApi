using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace PaintMixer.Api.Tests
{
    public class JobEndpointTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public JobEndpointTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SubmitJob_ValidRequest_Returns200WithJobCode()
        {
            var response = await _client.PostAsJsonAsync("/jobs",
                new { red = 30, blue = 20 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<JobSubmittedResponse>();
            body!.JobCode.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task SubmitJob_AllZeroes_Returns200()
        {
            var response = await _client.PostAsJsonAsync("/jobs",
                new { red = 0, blue = 0 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData(101, 0, 0, 0, 0, 0)]   // single dye over 100
        [InlineData(50, 51, 0, 0, 0, 0)]   // total over 100
        [InlineData(-1, 0, 0, 0, 0, 0)]    // negative value
        public async Task SubmitJob_InvalidDyes_Returns400(
            int red, int black, int white, int yellow, int blue, int green)
        {
            var response = await _client.PostAsJsonAsync("/jobs",
                new { red, black, white, yellow, blue, green });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task QueryJob_ExistingJob_Returns200WithStatus()
        {
            var submit = await _client.PostAsJsonAsync("/jobs", new { red = 10 });
            var submitted = await submit.Content.ReadFromJsonAsync<JobSubmittedResponse>();

            var response = await _client.GetAsync($"/jobs/{submitted!.JobCode}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
            body!.Status.Should().Be("Queued or Running");
        }

        [Fact]
        public async Task QueryJob_NonExistentJobCode_Returns404()
        {
            var response = await _client.GetAsync("/jobs/9999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CancelJob_ExistingJob_Returns200()
        {
            var submit = await _client.PostAsJsonAsync("/jobs", new { red = 10 });
            var submitted = await submit.Content.ReadFromJsonAsync<JobSubmittedResponse>();

            var response = await _client.DeleteAsync($"/jobs/{submitted!.JobCode}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CancelJob_NonExistentJobCode_Returns422()
        {
            var response = await _client.DeleteAsync("/jobs/9999");

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task CancelJob_AlreadyCancelled_Returns422()
        {
            var submit = await _client.PostAsJsonAsync("/jobs", new { red = 10 });
            var submitted = await submit.Content.ReadFromJsonAsync<JobSubmittedResponse>();

            await _client.DeleteAsync($"/jobs/{submitted!.JobCode}");

            // Cancel again
            var response = await _client.DeleteAsync($"/jobs/{submitted.JobCode}");

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task QueryJob_AfterCancellation_Returns404()
        {
            var submit = await _client.PostAsJsonAsync("/jobs", new { red = 10 });
            var submitted = await submit.Content.ReadFromJsonAsync<JobSubmittedResponse>();

            await _client.DeleteAsync($"/jobs/{submitted!.JobCode}");

            var response = await _client.GetAsync($"/jobs/{submitted.JobCode}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SubmitJob_TotalExactly100_Returns200()
        {
            var response = await _client.PostAsJsonAsync("/jobs", new { red = 50, blue = 50 });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SubmitJob_EmptyBody_ReturnsOk()
        {
            var response = await _client.PostAsync("/jobs",
                new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SubmitJob_MissingBody_Returns400()
        {
            var response = await _client.PostAsync("/jobs", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task QueryJob_NegativeJobCode_Returns404()
        {
            var response = await _client.GetAsync("/jobs/-1");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CancelJob_NegativeJobCode_Returns422()
        {
            var response = await _client.DeleteAsync("/jobs/-1");

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }

        [Fact]
        public async Task CancelJob_CompletedJob_Returns422()
        {
            var submit = await _client.PostAsJsonAsync("/jobs", new { red = 10 });
            var submitted = await submit.Content.ReadFromJsonAsync<JobSubmittedResponse>();

            await Task.Delay(3000);

            var response = await _client.DeleteAsync($"/jobs/{submitted!.JobCode}");

            response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        }
    }
}

