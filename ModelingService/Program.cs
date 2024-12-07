using Visavi.Quantis;
using Visavi.Quantis.Data;
using Visavi.Quantis.Modeling;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<TrainModelsService>();
builder.Services.AddQuantisCoreServices();

var host = builder.Build();
host.Run();
