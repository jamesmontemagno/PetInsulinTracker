using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PetInsulinTracker.Api.Services;

var host = new HostBuilder()
	.ConfigureFunctionsWebApplication()
	.ConfigureServices(services =>
	{
		services.AddSingleton<TableStorageService>();
		services.AddSingleton<BlobStorageService>();
	})
	.Build();

host.Run();
