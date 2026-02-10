using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core.Serialization;
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
		services.Configure<WorkerOptions>(options =>
		{
			options.Serializer = new JsonObjectSerializer(new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			});
		});
	})
	.Build();

host.Run();
