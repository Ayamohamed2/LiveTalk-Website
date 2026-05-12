using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using NEEFRA.Core;
using Restaurant.Core.Interfaces.IService;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace IntegrationTests.TestHelpers
{

    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Mock<IAccountService> AccountServiceMock { get; } = new();
        public Mock<IChatService> ChatServiceMock { get; } = new();
        public Mock<IGroupService> GroupServiceMock { get; } = new();
        public Mock<ICallService> CallServiceMock { get; } = new();
        public Mock<IUserProfileService> UserProfileServiceMock { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>   
            {
                ReplaceService(services, AccountServiceMock.Object);
                ReplaceService(services, ChatServiceMock.Object);
                ReplaceService(services, GroupServiceMock.Object);
                ReplaceService(services, CallServiceMock.Object);
                ReplaceService(services, UserProfileServiceMock.Object);

                services
                    .AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });

                services.PostConfigure<AuthenticationOptions>(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                    options.DefaultScheme = "Test";
                    options.DefaultForbidScheme = "Test";
                });
            });
        }

        private static void ReplaceService<T>(IServiceCollection services, T implementation) where T : class
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
            if (descriptor != null) services.Remove(descriptor);
            services.AddSingleton(implementation);
        }
    }


    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string TestUserId = "test-user-id";

        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // ← لو مفيش Authorization header يرجع Fail
            if (!Request.Headers.ContainsKey("Authorization"))
                return Task.FromResult(AuthenticateResult.Fail("No Authorization header"));

            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@test.com"),
        };
            var identity = new ClaimsIdentity(claims, "Test");
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
