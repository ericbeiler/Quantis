using Visavi.Quantis.Modeling;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TrainModelJob>();

var host = builder.Build();
host.Run();
