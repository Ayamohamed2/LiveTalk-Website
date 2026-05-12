using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Moq;
using NEEFRA.Core.DTO.Service;
using Restaurant.Core.DTO.Profie;
using Xunit;

namespace IntegrationTests
{

    public class UserProfileControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public UserProfileControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        }

        

        [Fact]
        public async Task GetProfile_Authenticated_Returns200WithProfileData()
        {
            _factory.UserProfileServiceMock
                .Setup(s => s.GetProfileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Ok(new
                {
                    UserId = "test-user-id",
                    FullName = "Ahmed Mohamed",
                    Email = "ahmed@test.com",
                    ProfilePicture = "http://example.com/pic.jpg"
                }));

            var response = await _client.GetAsync("/api/v1/UserProfile");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain("Ahmed Mohamed");
        }

     

        [Fact]
        public async Task GetProfile_UserNotFound_Returns404()
        {
            _factory.UserProfileServiceMock
                .Setup(s => s.GetProfileAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Profile not found", "NotFound"));

            var response = await _client.GetAsync("/api/v1/UserProfile");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }



  

        [Fact]
        public async Task UpdateProfile_Unauthenticated_Returns401()
        {
            var anonClient = _factory.CreateClient();
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Ahmed"), "FullName");

            var response = await anonClient.PutAsync("/api/v1/UserProfile", content);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateProfile_InvalidData_Returns400()
        {
            _factory.UserProfileServiceMock
                .Setup(s => s.UpdateProfileAsync(
                    It.IsAny<string>(),
                    It.IsAny<UserProfileDTO>(),
                    It.IsAny<string>(),
                    It.IsAny<IWebHostEnvironment>()))
                .ReturnsAsync(ServiceResult<object>.Fail("Invalid profile data"));

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(""), "FullName"); // اسم فاضي

            var response = await _client.PutAsync("/api/v1/UserProfile", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

    
    }

   
}
