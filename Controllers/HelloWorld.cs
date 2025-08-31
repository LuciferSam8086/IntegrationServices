using IntegrationServices.Interfaces;

namespace IntegrationServices.Controllers
{
    public class HelloWorld : IScopedEndpoint
    {
        private readonly ILogger<HelloWorld> _logger;   

        public HelloWorld(ILogger<HelloWorld> logger)
        {
            _logger = logger;
        }   

        public void Map(IEndpointRouteBuilder app)
        {
            _logger.LogInformation("DI works in HelloWorld controller!");
            app.MapGet("/hello", Fool).WithName("HelloWorld");
        }

        private async Task<string> Fool()
        {
            _logger.LogCritical("oh shit!");
            await Task.Delay(5000);
            return "sto cazzo";
        }
    }
}
