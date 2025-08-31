namespace IntegrationServices.Controllers
{

    using IntegrationServices.Interfaces;
    using IntegrationServices.Models;
    using Microsoft.AspNetCore.Http.HttpResults;
    using Microsoft.EntityFrameworkCore;
    using System.Threading.Channels;

    public class CheckFiles : IScopedEndpoint
    {
        private readonly ILogger<CheckFiles> _logger;
        private readonly NiFiTestContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly Channel<int> _channel;

        public CheckFiles(ILogger<CheckFiles> logger, NiFiTestContext dbContext, IConfiguration configuration, Channel<int> channel)
        {
            _logger = logger;
            _dbContext = dbContext;
            _configuration = configuration;
            _channel = channel;
        }

        public void Map(IEndpointRouteBuilder app)
        {
            _logger.LogInformation("DI works in HelloWorld controller!");
            app.MapPost("/checkfiles", (CheckFiles handler) => handler.Fool())
               .WithName("checkfiles");

        }

        private async Task<IResult> Fool()
        {
            // Read from configuration the path
            //var baseDirectory = _configuration.GetSection("ProgramSettings").GetValue<string>("DirectoryToScan");

            var anni = await _dbContext.AnniFatture
                .Select(a => a.Anno)    
                .ToListAsync();

            foreach (var anno in anni)
            {
                _logger.LogInformation("Enqueuing year {Year}", anno);
                // Enqueue the year for processing
                await _channel.Writer.WriteAsync(anno);
            }

            // Al chiamante si torna che la richiesta è stata accettata
            // Poi NiFi si attiverà quando riceverà un file parquet in ListenFTP
            return Results.Accepted();
        }
    }

    public class AskForFiles
    {
        public int anno { get; set; }

    }

}
