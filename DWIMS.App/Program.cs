using System.Text;
using Amazon.S3;
using Azure.Identity;
using Azure.Storage.Blobs;
using DWIMS.Controllers;
using DWIMS.Data;
using DWIMS.Service;
using DWIMS.Service.Analytics;
using DWIMS.Service.Auth;
using DWIMS.Service.Common;
using DWIMS.Service.CurrentUser;
using DWIMS.Service.Department;
using DWIMS.Service.Process;
using DWIMS.Service.Services;
using DWIMS.Service.Storage;
using DWIMS.Service.Logs;
using DWIMS.Service.Submission;
using DWIMS.Service.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace DWIMS
{
    using Scalar.AspNetCore;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen(x =>
                    {
                        x.SwaggerDoc("v1", new() { Title = "DWIMS API", Version = "v1" });
                        x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                        {
                            Name = "Authorization",
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT",
                            In = ParameterLocation.Header,
                            Description = "Enter your JWT Access Token",
                        });
                    }
                );

            builder.Services.AddOptions<JwtOptions>()
                .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));
            
            if (builder.Environment.IsProduction())
            {
                builder.Configuration.AddAzureKeyVault(
                    new Uri("https://dwims-vault.vault.azure.net/"),
                    new DefaultAzureCredential());
            }
            
            var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
            
            builder.Services
                .AddOptions<StorageOptions>()
                .Bind(builder.Configuration.GetSection(StorageOptions.SectionName));
            
            builder.Services
                .AddOptions<EmailOptions>()
                .Bind(builder.Configuration.GetSection(EmailOptions.SectionName));
            
            builder.Services
                .AddOptions<GoogleOptions>()
                .Bind(builder.Configuration.GetSection(GoogleOptions.SectionName));


            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                    {
                        options.MapInboundClaims = false;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtOptions.Issuer,
                            ValidAudience = jwtOptions.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                            ClockSkew = TimeSpan.FromSeconds(30)
                        };
                    }
                );
        
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IAuthorizationHandler, RoleHandler>();
            builder.Services.AddScoped<IDepartmentService, DepartmentService>();
            builder.Services.AddScoped<IProcessService, ProcessService>();
            builder.Services.AddScoped<IAcroFormService, AcroFormService>();
            builder.Services.AddScoped<ISubmissionService, SubmissionService>();
            builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();
            builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ILogService, LogService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(
                    builder.Configuration.GetConnectionString("Default"),
                    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("Default")))
                    .LogTo(Console.WriteLine, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
                    .EnableSensitiveDataLogging()
                );

            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddScoped<IStorageService, StorageService>();
                
                builder.Services.AddSingleton<IAmazonS3>(sp =>
                {
                    var opts = sp.GetRequiredService<IOptions<StorageOptions>>().Value;

                    return new AmazonS3Client(
                        opts.AccessKey,
                        opts.SecretKey,
                        new AmazonS3Config { ServiceURL = opts.ServiceUrl});
                });
                
            }

            if (builder.Environment.IsProduction())
            {
                builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
                
                builder.Services.AddSingleton<BlobServiceClient>(optionsService =>
                {
                    var opts = optionsService.GetRequiredService<IOptions<StorageOptions>>().Value;

                    return new BlobServiceClient(opts.ConnectionString);
                });
            }
        
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy.WithOrigins(builder.Configuration["Cors:Origin"])
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    DwimsPolicies.Submitter,
                    x =>
                        x.AddRequirements(new Requirement(GeneralRole.Submitter))
                );

                options.AddPolicy(
                    DwimsPolicies.Reviewer,
                    x =>
                        x.AddRequirements(new Requirement(GeneralRole.Reviewer))
                );

                options.AddPolicy(
                    DwimsPolicies.Administrator,
                    x =>
                        x.AddRequirements(new Requirement(GeneralRole.Administrator))
                );

                options.AddPolicy(
                    DwimsPolicies.SuperAdministrator,
                    x =>
                        x.AddRequirements(new Requirement(GeneralRole.SuperAdministrator))
                );
            });
            
            var app = builder.Build();
        
            app.UseCors("Frontend");
            app.UseAuthentication();
            app.UseAuthorization();
        
            app.MapSwagger("/openapi/{documentName}.json");
            app.MapScalarApiReference();

            app.MapAuthEndpoints();
            app.MapUserEndpoints();
            app.MapDepartmentEndpoints();
            app.MapProcessEndpoints();
            app.MapSubmissionEndpoints();
            app.MapLogEndpoints();
            app.MapAnalyticsEndpoints();

            if (app.Environment.IsProduction())
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<DWIMS.Data.AppDbContext>();
                context.Database.Migrate();
                
                if (!context.Users.Any(x => x.Email == "agjavier@kld.edu.ph"))
                {
                    var user = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = "agjavier@kld.edu.ph",
                        Password = "agjavier",
                        FirstName = "Adrian",
                        MiddleName = "Gabayno",
                        LastName = "Javier",
                    };
                    
                    context.Users.Add(user);

                    context.Roles.Add(new Role
                    {
                        Id = Guid.NewGuid(),
                        Title = "Super Administrator",
                        Description = "Top-level administrator.",
                        UserId = user.Id,
                        GeneralRole = GeneralRole.SuperAdministrator,
                    });
                    
                    context.SaveChanges();
                }

            }
            
            
            // app.Use(async (context, next) =>
            // {
            //     Console.WriteLine("========== RAW HTTP CONTEXT DUMP ==========");
            //
            //     // 1. Print the Method and Path
            //     Console.WriteLine($"[REQUEST] {context.Request.Method} {context.Request.Path}");
            //
            //     // 2. Print ALL Headers (This is where you'll see if Scalar sends the token)
            //     Console.WriteLine("[HEADERS]:");
            //     foreach (var header in context.Request.Headers)
            //     {
            //         // Mask the token slightly so it doesn't spam your console
            //         object? value;
            //         value = header.Key == "Authorization" 
            //             ? $"{header.Value}..." 
            //             : header.Value;
            //         Console.WriteLine($"  {header.Key}: {value}");
            //     }
            //
            //     // 3. Print Authentication Status BEFORE middleware processes it
            //     Console.WriteLine($"[AUTH - BEFORE] IsAuthenticated: {context.User.Identity?.IsAuthenticated}");
            //
            //     // Call the next middleware (This is where UseAuthentication() happens)
            //     await next();
            //
            //     // 4. Print Authentication Status AFTER middleware processes it
            //     Console.WriteLine($"[AUTH - AFTER]  IsAuthenticated: {context.User.Identity?.IsAuthenticated}");
            //
            //     // 5. Print ALL Claims on the User
            //     Console.WriteLine("[CLAIMS]:");
            //     foreach (var claim in context.User.Claims)
            //     {
            //         Console.WriteLine($"  {claim.Type}: {claim.Value}");
            //     }
            //
            //     Console.WriteLine("==========================================\n");
            // });

            app.Run();
        }
    }
}