using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NEEFRA.Core.Services;
using NEEFRA.Core;
using Realtima_Chat_project.Hubs;
using Restaurant.Core.DTO.Acoount.Validation;
using Restaurant.Core.DTO.Email;
using Restaurant.Core.Interfaces.IService;
using Restaurant.Core.Interfaces.IService.Redis;
using Restaurant.Core.Mapping;
using Restaurant.Core.Models.Account;
using Restaurant.Core.Services;
using Restaurant.Core.Services.EmailService;
using Restaurant.Core.Services.Redis;
using Restaurant.Infrastructure.Reposatory;
using Serilog;
using SignalIR_practice.Hubs;
using StackExchange.Redis;
using Villa_API_Project.Custom_Middleware;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;
using Restaurant.API.Background_Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Restaurant.API.Custom_Middleware;
using Restaurant.API.HealhCheck;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Hangfire;
using AspNetCoreRateLimit;
using Hangfire.SqlServer;

namespace Restaurant_Project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                options.MaximumReceiveMessageSize = 102400;
                options.StreamBufferCapacity = 10;
            });

            #region Serilog — Logging
            builder.Host.UseSerilog((HostBuilderContext context, IServiceProvider services, LoggerConfiguration loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services);
            });
            #endregion

            #region Database — EF Core + Identity
            builder.Services.AddDbContext<Context>(optionbuilder =>
            {
                optionbuilder.UseSqlServer(builder.Configuration.GetConnectionString("cs"));
            });

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
                options.Lockout.MaxFailedAccessAttempts = 3;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<Context>()
            .AddDefaultTokenProviders();

            builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
            {
                opt.TokenLifespan = TimeSpan.FromMinutes(5);
            });
            #endregion

            #region AutoMapper
            builder.Services.AddAutoMapper(typeof(MappingConfig));
            #endregion

            #region API Versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            builder.Services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });
            #endregion

            #region Controllers + Cache Profiles
            builder.Services.AddControllers(option =>
            {
                option.CacheProfiles.Add("Default30", new CacheProfile()
                {
                    Duration = 30
                });
            });
            #endregion

            #region Swagger / OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1.0",
                    Title = "Magic Villa V1",
                    Description = "API to manage Villa",
                });

                options.SwaggerDoc("v2", new OpenApiInfo
                {
                    Version = "v2.0",
                    Title = "Magic Villa V2",
                    Description = "API to manage Villa",
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Enter JWT token in the format: Bearer {your token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
            #endregion

            #region Authentication — JWT + Google OAuth
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "JwtBearer";
                options.DefaultChallengeScheme = "JwtBearer";
            })
            .AddCookie("ExternalCookies")
            .AddJwtBearer("JwtBearer", options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };


                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            (path.StartsWithSegments("/chat") || path.StartsWithSegments("/callHub")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            })
     
            .AddGoogle("Google", options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                options.CallbackPath = "/signin-google";
                options.SignInScheme = "ExternalCookies";
            });


            #endregion


            #region CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            #endregion

            #region Redis Cache
            var connectionString = builder.Configuration.GetConnectionString("Redis");
            try
            {
                var redis = ConnectionMultiplexer.Connect(connectionString);
                Console.WriteLine("✅ Connected to Redis successfully!");
                redis.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Redis connection failed: {ex.Message}");
            }

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "";
            });
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configuration = builder.Configuration.GetConnectionString("Redis");
                return ConnectionMultiplexer.Connect(configuration);
            });
            #endregion


            #region Rate Limiting
            builder.Services.AddMemoryCache();

            builder.Services.Configure<IpRateLimitOptions>(
                builder.Configuration.GetSection("IpRateLimiting"));

            builder.Services.Configure<IpRateLimitPolicies>(
                builder.Configuration.GetSection("IpRateLimitPolicies"));

            // لو بتستخدم Redis (موجود في مشروعك) أحسن من MemoryCache في Production
            builder.Services.AddSingleton<IIpPolicyStore, DistributedCacheIpPolicyStore>();
            builder.Services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
            #endregion

            #region Hangfire
            builder.Services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(
                    builder.Configuration.GetConnectionString("cs"),
                    new SqlServerStorageOptions
                    {
                        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.Zero,
                        UseRecommendedIsolationLevel = true,
                        DisableGlobalLocks = true
                    }));

            builder.Services.AddHangfireServer();

            // سجّل الـ Services
            builder.Services.AddScoped<ExpiredTokensCleanupService>();

            #endregion

            #region Dependency Injection — Services

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAuthRepository, AuthRepository>();
            builder.Services.AddScoped<IJWT_TokenReposatory, JWT_TokenReposatory>();
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();
            builder.Services.AddScoped<IAccountService, AccountService>();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            builder.Services.AddScoped<IGroupService, GroupService>();
            builder.Services.AddScoped<ICallService, CallService>();
            builder.Services.AddScoped<IChatService, ChatService>();

            builder.Services.AddScoped<RequestTimeLoggingMiddleware>();

            builder.Services.AddAuthorization();

            #region Health Checks
            // سجّل الـ Custom Checks في الـ DI
            builder.Services.AddScoped<RedisHealthCheck>();


            builder.Services.AddHealthChecks()
                .AddSqlServer(
        builder.Configuration.GetConnectionString("cs"))

                // ✅ Redis
                .AddCheck<RedisHealthCheck>(
            name: "Redis",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "cache", "redis" });




            // ✅ Health Check UI
            builder.Services.AddHealthChecksUI(options =>
            {
                options.SetEvaluationTimeInSeconds(15);
                options.MaximumHistoryEntriesPerEndpoint(50);
                options.AddHealthCheckEndpoint("Restaurant API", "/health");
            })
            .AddInMemoryStorage();


            #endregion
            #endregion

            #region Validation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<ChangePasswordValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<ResetPasswordValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<EmailDTO>();
            #endregion

            #region Middleware Pipeline
            var app = builder.Build();
            app.UseHangfireDashboard("/hangfire");

            // ✅ Recurring Jobs
            var jobs = app.Services.GetRequiredService<IRecurringJobManager>();

            jobs.AddOrUpdate<ExpiredTokensCleanupService>(
                "cleanup-expired-tokens",
                service => service.CleanupAsync(),
                Cron.Hourly);




            app.UseIpRateLimiting();
            app.UseMiddleware<ConcurrentRequestsMiddleware>();
            app.UseMiddleware<RequestTimeLoggingMiddleware>();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v2/swagger.json", "Magic_VillaV2");
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Magic_VillaV1");
                });
            }
            #region Health Check Endpoints

            // ✅ Endpoint تفصيلي JSON
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
                ResultStatusCodes =
    {
        [HealthStatus.Healthy]   = 200,
        [HealthStatus.Degraded]  = 200,
        [HealthStatus.Unhealthy] = 503
    }
            });

            // ✅ Endpoint بسيط للـ Load Balancer
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false
            });

            // ✅ بس الـ DB checks
            app.MapHealthChecks("/health/db", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("db"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            // ✅ Dashboard UI
            app.MapHealthChecksUI(options =>
            {
                options.UIPath = "/health-ui";
            });

            #endregion

            app.UseMiddleware<ExeptionHandlingMiddleware>();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMiddleware<BlackListTokensMiddleware>();
            app.UseAuthorization();
            app.UseCors("AllowAll");

            app.MapHub<ChatHub>("/chat");
            app.MapHub<CallHub>("/callHub");
            app.MapControllers();
            app.Run();
            #endregion
    }
    }
  
}  public partial class Program { }