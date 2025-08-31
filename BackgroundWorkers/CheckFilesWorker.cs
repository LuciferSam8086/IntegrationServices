using IntegrationServices.Models;
using Microsoft.EntityFrameworkCore;
using Parquet.Serialization;
using System.Threading.Channels;

namespace IntegrationServices.BackgroundWorkers
{
    public class CheckFilesWorker : BackgroundService
    {
        private readonly Channel<int> _channel;
        private readonly ILogger<CheckFilesWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _semaphore;
        private readonly IServiceScopeFactory _scopeFactory;

        public CheckFilesWorker(Channel<int> channel, ILogger<CheckFilesWorker> logger, IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            _channel = channel;
            _logger = logger;
            _configuration = configuration;
            _semaphore = new SemaphoreSlim(1, 1); // Limita lo scan di solo un anno alla volta
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CheckFilesWorker is starting.");
             

            await foreach (var year in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                // Start processing in the background but respect concurrency limit
                _ = ProcessYearAsync(year, stoppingToken);
            }
        }

        private async Task ProcessYearAsync(int year, CancellationToken token)
        {
            await _semaphore.WaitAsync(token);
            try
            {
                _logger.LogInformation("Processing year {Year}", year);

                var baseDir = _configuration.GetSection("ProgramSettings").GetValue<string>("DirectoryToScan");

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<NiFiTestContext>();

                var fullPath = Path.Combine(baseDir, year.ToString());

                // Get SHA1 hash of the file name
                using var sha1 = System.Security.Cryptography.SHA1.Create();

                var addRecordToParquet = false;

                var parquetData = new List<ParquetData>() { };

                foreach (var file in Directory.EnumerateFiles(fullPath, "*.pdf",enumerationOptions: new EnumerationOptions() { RecurseSubdirectories = false }))
                {
                    var fileInfo = new FileInfo(file);


                    var hashBytes = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(fileInfo.FullName));
                    var hashString = Convert.ToHexString(hashBytes);

                    var exists = (from f in dbContext.ListaFatture
                                 where f.FileNameHash == hashString
                                 select 1).Any();

                    if (!exists)
                    {
                        // Inserisci il file nel DB
                        var newEntry = new ListaFatture
                        {
                            PercorsoFile = fileInfo.FullName,
                            FileNameHash = hashString,
                            UltimaDataAggiornamento = fileInfo.LastWriteTimeUtc
                        };
                        await dbContext.ListaFatture.AddAsync(newEntry, token);
                        await dbContext.SaveChangesAsync(token);
                        addRecordToParquet = true;
                    }
                    else
                    {
                        // Se la data di aggiornamento è cambiata, aggiorna il record
                        var rows = await dbContext.ListaFatture.Where(y => y.FileNameHash == hashString && y.UltimaDataAggiornamento < fileInfo.LastWriteTimeUtc)
                            .ExecuteUpdateAsync(s => s
                            .SetProperty(f => f.UltimaDataAggiornamento, fileInfo.LastWriteTime),token);

                        _logger.LogInformation("Updated {Rows} rows for file {File}", rows, fileInfo.FullName); 

                        addRecordToParquet = rows > 0;
                    }

                    if(addRecordToParquet)
                    {
                        _logger.LogInformation("File {File} is new or updated, will be added to Parquet export", fileInfo.FullName);

                        parquetData.Add( new ParquetData
                        {
                            FilePath = fileInfo.FullName,
                        });
                    }

                }


                // once the year is processed, if there are new or updated files, export to Parquet
                if (parquetData.Count > 0)
                {
                    await ParquetSerializer.SerializeAsync(parquetData, $"d:\\source\\filesDaImportare_{year}.parquet", cancellationToken: token);
                }

                //// Simulate scan + DB update + Parquet export
                //await Task.Delay(TimeSpan.FromSeconds(10), token);

                _logger.LogInformation("Finished year {Year}", year);

            }
            catch (Exception ex)
            {
                _logger.LogCritical("Error processing year {Year} {message}", year, ex.Message);
                
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
