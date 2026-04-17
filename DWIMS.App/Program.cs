using System.Text;
using DWIMS.Controllers;
using DWIMS.Data;
using DWIMS.Service;
using DWIMS.Service.Auth;
using DWIMS.Service.Services;
using DWIMS.Service.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace DWIMS;

using Scalar.AspNetCore;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(
                x => 
                    x.SwaggerDoc("v1", new() { Title = "DWIMS API", Version = "v1" })
                );

        builder.Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));
        
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                {
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

        app.Run();
    }
}