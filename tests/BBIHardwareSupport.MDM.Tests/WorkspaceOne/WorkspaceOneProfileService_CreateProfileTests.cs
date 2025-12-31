using BBIHardwareSupport.MDM.WorkspaceOne.Core.Models;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Services;
using BBIHardwareSupport.MDM.WorkspaceOne.Core.Services.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BBIHardwareSupport.MDM.Tests.WorkspaceOne;

public class WorkspaceOneProfileService_CreateProfileTests
{
    [Fact]
    public async Task CreateProfileAsync_posts_expected_request()
    {
        // What we are testing:
        // 1) CreateProfileAsync constructs the correct POST endpoint URL using request.PlatformSegment
        // 2) It sends request.Payload as the JSON body (transport does not mutate the payload)
        // 3) It sets Content-Type to request.ContentType (WS1 versioned media type)
        // 4) It issues exactly one HTTP request

        // Arrange
        var baseUri = new Uri("https://example.awmdm.com/");

        // Marker fields allow us to prove the exact payload we provided was sent.
        var payload = new JObject
        {
            ["General"] = new JObject
            {
                ["Name"] = "UnitTest Profile"
            }
        };

        var request = new WorkspaceOneProfileCreateRequest
        {
            PlatformSegment = "Android",
            Payload = payload
            // ContentType uses default: "application/json;version=2"
        };

        HttpRequestMessage? captured = null;

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                // If your service deserializes a specific response model, return JSON shaped like it expects.
                Content = new StringContent(@"{ ""ProfileId"": 12345 }", Encoding.UTF8, "application/json")
            })
            .Verifiable();

        var httpClient = new HttpClient(handler.Object) { BaseAddress = baseUri };

        // Mock dependencies used for authentication / headers. Keep Loose unless you want strict expectations.
        var auth = new Mock<IWorkspaceOneAuthService>(MockBehavior.Loose);
        var logger = new Mock<ILogger<WorkspaceOneProfileService>>();

        var sut = new WorkspaceOneProfileService(httpClient, auth.Object, logger.Object);

        // Act
        var result = await sut.CreateProfileAsync(request, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull("the service should send exactly one HTTP request");
        captured!.Method.Should().Be(HttpMethod.Post);

        // 1) URL is derived from PlatformSegment.
        // Adjust casing if your real endpoint uses ".../Create" instead of ".../create".
        captured.RequestUri!.ToString()
            .Should().Contain($"/profiles/platforms/{request.PlatformSegment}/create",
                because: "WS1 Create Profile endpoint includes the platform segment");

        // 2) Content-Type is the WS1 versioned media type (default comes from request.ContentType).
        captured.Content.Should().NotBeNull();
        captured.Content!.Headers.ContentType!.ToString()
            .Should().Be(request.ContentType, because: "transport must honor the request's versioned media type");

        // 3) Body is exactly the payload JSON we supplied.
        var body = await captured.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();

        var sentJson = JObject.Parse(body);
        // Assert a marker field (we don't validate full WS1 schema in transport tests).
        sentJson["General"]?["Name"]?.Value<string>().Should().Be("UnitTest Profile");

        // 4) Only one HTTP request was made.
        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());

     }

    [Fact]
    public async Task CreateProfileAsync_throws_on_non_success_status()
    {
        // What we are testing:
        // The transport layer should treat non-2xx responses as failures
        // (either by throwing or by returning a failure result depending on your design).
        // This test verifies your current behavior: it throws.

        // Arrange
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(@"{""message"":""bad request""}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("https://example.awmdm.com/") };
        var auth = new Mock<IWorkspaceOneAuthService>(MockBehavior.Loose);
        var logger = new Mock<ILogger<WorkspaceOneProfileService>>();

        var sut = new WorkspaceOneProfileService(httpClient, auth.Object, logger.Object);

        var request = new WorkspaceOneProfileCreateRequest
        {
            PlatformSegment = "Android",
            Payload = new JObject { ["General"] = new JObject { ["Name"] = "Bad" } }
        };

        // Act + Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            sut.CreateProfileAsync(request, CancellationToken.None));
    }
}
