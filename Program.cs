
using IntegrationServices.Controllers;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace IntegrationServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            builder.Services.AddSingleton(Channel.CreateUnbounded<int>());  
            builder.Services.AddHostedService<BackgroundWorkers.CheckFilesWorker>();

            builder.Services.RegisterAllEndpoints(typeof(Program).Assembly);

            // Add database context
            builder.Services.AddDbContext<Models.NiFiTestContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            // Auto‑map all endpoints
            app.MapAllEndpoints(typeof(Program).Assembly);






            app.Run();
        }
    }
}
