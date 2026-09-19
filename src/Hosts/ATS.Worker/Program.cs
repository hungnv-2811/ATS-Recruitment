using ATS.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<ScreeningWorker>();

var host = builder.Build();
host.Run();
