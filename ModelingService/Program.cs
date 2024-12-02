using Visavi.Quantis.Modeling;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TrainModelsService>();

var host = builder.Build();
host.Run();
