using MyModel;
using System.Xml.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommandLine;
using Server;
using Dapr.Client;
using Microsoft.Extensions.Configuration.Json;

var builder = WebApplication.CreateBuilder(args);

ServerCommandOptions options = new();

Parser.Default.ParseArguments<ServerCommandOptions>(args).WithParsed(o => { options = o; });

Console.WriteLine($"Current Server configuration: {JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}\n");

var app = builder.Build();

ServicesConfig ServiceMapConfig = null;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();


if (!options.UseDapr)
{

    #region BuilServicesConfiguration

    ServiceMapConfig = config.GetSection("ServiceConfig").Get<ServicesConfig>();

    Console.WriteLine($"Service Mapping Configuration:");

    foreach (var item in ServiceMapConfig.ServiceDetails)
    {
        Console.WriteLine($"\t{item.Key}-->{item.Value}");
    }
    #endregion

}

try
{


    app.MapGet("/", () =>
    {
        Console.WriteLine($"CALLING: /");

        var message = $"Hello Dapr from service {options.ServiceName}!";

        return Results.Json(message);
    });

    app.MapGet("/hello/{name}", (string name) =>
    {
        Console.WriteLine($"CALLING: /hello/{name}");

        var message = $"Hello {name}!";
        return Results.Json(message);
    });

    app.MapPost("/registerUser", (UserAccount myModel) =>
    {
        Console.WriteLine($"CALLING: /registerUser/");

        myModel.Id = Guid.NewGuid().ToString();

        return myModel;
    });


    app.MapPost("/service", async (ServiceInfoRequest request) =>
    {
        Console.WriteLine($"\n Service {options?.ServiceName}: /service/ Received Request with service info:");
        Console.WriteLine($"{JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}");

        ServiceInfoResponse response = new()
        {
            Message = $"{request.Greatings}"
        };

        if (response.ServiceChainInfos == default)
            response.ServiceChainInfos = new();

        response.ServiceChainInfos.Add(new ServiceInfoResponse()
        {
            Name = options?.ServiceName,
            Message = $"This workload is made by Service: {options?.ServiceName}",
            Metadata = new { Id = Guid.NewGuid(), WorkloadBy = $"{options?.ServiceName}" }
        }
        );

        if (request.ServiceChains != default && request.NextServiceIndexToCall < request.ServiceChains.Length)
        {
            var nextInfo = new ServiceInfoRequest()
            {
                Greatings = $"Calling from Service: {options?.ServiceName}",
                ServiceChains = request.ServiceChains,
                NextServiceIndexToCall = request.NextServiceIndexToCall + 1
            }
            ;

            var nextService = request.ServiceChains[request.NextServiceIndexToCall];

            Console.WriteLine($"Calling next service: info.ServiceChains[{request.NextServiceIndexToCall}]= \"{nextService}\" with service info:");
            Console.WriteLine($"{JsonSerializer.Serialize(nextInfo, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}");

            ServiceInfoResponse? infoResponse = default;

            if (options.UseDapr)
            {
                Console.WriteLine($"Using DAPR client");
                using var daprClient = new DaprClientBuilder().Build();
                
                infoResponse = await daprClient.InvokeMethodAsync<ServiceInfoRequest, ServiceInfoResponse>(HttpMethod.Post, nextService, "service", nextInfo);
            }
            else
            {
                string baseUrl = ServiceMapConfig.ServiceDetails[nextService];
                Console.WriteLine($"Using HTTP client {baseUrl}/service");

                using var httpClient = new HttpClient();
                
                var postResponse = await httpClient.PostAsJsonAsync($"{baseUrl}/service", nextInfo);

                postResponse.EnsureSuccessStatusCode();
                infoResponse = await postResponse.Content.ReadFromJsonAsync<ServiceInfoResponse>();

            }

            if (infoResponse != null)
            {

                if (response.ServiceChainInfos == default)
                    response.ServiceChainInfos = new();

                response.ServiceChainInfos.Add(infoResponse);
            }
        }
        else
        {
            Console.WriteLine($"END OF calling chain Service {options?.ServiceName}.");
        }

        return response;
    });


    if (!options.ServiceName?.Equals("Unknow", StringComparison.OrdinalIgnoreCase) ?? false)
    {
        Console.WriteLine($"Start service: \"{options.ServiceName}\" ");
        app.Run($"http://*:{options.Port}");
    }

}
catch (Exception ex)
{
    Console.WriteLine($"\n\nERROR: \"{ex.Message}\" ");
}

