using DWIMS.Controllers;
using DWIMS.Data;
using DWIMS.Service.Auth;
using DWIMS.Service.Services;

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
        
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        
        builder.Services.AddDbContext<AppDbContext>();
        
        var app = builder.Build();
        
        app.MapSwagger("/openapi/{documentName}.json");
        app.MapScalarApiReference();

        app.MapAuthEndpoints();

        app.Run();
    }
}