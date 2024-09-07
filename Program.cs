using Dorico_Remote;

var builder = Host.CreateDefaultBuilder(args).UseWindowsService().UseSystemd();
builder.ConfigureServices((hostContext, services) => { services.AddHostedService<HttpWorker>(); });

var host = builder.Build();
host.Run();