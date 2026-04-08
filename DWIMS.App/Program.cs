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
        
        var app = builder.Build();
        
        app.MapSwagger("/openapi/{documentName}.json");
        app.MapScalarApiReference();

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }
}