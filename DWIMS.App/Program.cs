using System.Text;
using DWIMS.Controllers;
using DWIMS.Data;
using DWIMS.Service;
using DWIMS.Service.Auth;
using DWIMS.Service.CurrentUser;
using DWIMS.Service.Services;
using DWIMS.Service.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
        
            var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

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
        
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
        
            builder.Services.AddDbContext<AppDbContext>();

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

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IAuthorizationHandler, RoleHandler>();
        
            var app = builder.Build();
        
            app.UseAuthentication();
            app.UseAuthorization();
        
            app.MapSwagger("/openapi/{documentName}.json");
            app.MapScalarApiReference();

            app.MapAuthEndpoints();
            app.MapUserEndpoints();
            
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