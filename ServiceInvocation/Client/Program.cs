using Client;
using CommandLine;
using Dapr.Client;
using Microsoft.Extensions.Configuration;
using MyModel;
using System.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;


ClientCommandOptions? options = default;

try
{
    ServicesConfig ServiceMapConfig = null;
    Parser.Default.ParseArguments<ClientCommandOptions>(args).WithParsed(o => { options = o; });

    Console.WriteLine($"Current Client options: {JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}\n");

    IConfigurationRoot config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();



    if (options == null)
    {
        throw new Exception("Missing parameter");
    }
    else
    {
        Console.WriteLine("Start Options:");
        Console.WriteLine(JsonSerializer.Serialize(options));
    }

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

        Console.WriteLine("============ HTTP Client NO DAPR ============");
        Console.WriteLine($"Calling {options.ServiceToCall()} ({ServiceMapConfig.ServiceDetails[options.ServiceToCall()]}) using HttpClient...\n");
        await CallUsingHttpClientAsync();
        Console.WriteLine("Done.");
        Console.WriteLine("=====================================");
    }
    else
    {
        Console.WriteLine("============ HTTP Client ============");
        Console.WriteLine($"Calling Services: {options.ServiceToCall()} using HttpClient...\n");
        await CallDaprUsingHttpClientAsync();
        Console.WriteLine("Done.");
        Console.WriteLine("=====================================");
        /**/
        Console.WriteLine();

        Console.WriteLine("========= Dapr HTTP Client ==========");
        Console.WriteLine($"Calling Services: {options.ServiceToCall()} using DaprHttpClient...\n");
        await CallUsingHttpDaprClientAsync();
        Console.WriteLine("Done.");
        Console.WriteLine("=====================================");
        /**/
        Console.WriteLine();

        Console.WriteLine("=========== Dapr Client ============");
        Console.WriteLine($"Calling Services: {options.ServiceToCall()} using DaprClient...\n");
        await CallUsingDaprClientAsync();
        Console.WriteLine("Done.");
        Console.WriteLine("====================================");
    }



    async Task CallUsingHttpClientAsync()
    {
        using var httpClient = new HttpClient();
        string service = options.ServiceToCall();
        string baseUrl = ServiceMapConfig.ServiceDetails[service];

        if (baseUrl == null)
            throw new Exception($"Unable to find endpoint for service: {service}.");

        //if (options.ServiceAppId != default)
        {

            /**/
            var helloResponse = new HttpResponseMessage();
            var helloMessage = "";

            helloResponse = await httpClient.GetAsync($"{baseUrl}/");
            helloResponse.EnsureSuccessStatusCode();
            helloMessage = await helloResponse.Content.ReadAsStringAsync();
            Console.WriteLine($" - GET \"{baseUrl}/\" ---> Result: {helloMessage}\n");

            var name = "Tommaso";
            helloResponse = await httpClient.GetAsync($"{baseUrl}/hello/{name}");
            helloResponse.EnsureSuccessStatusCode();
            helloMessage = await helloResponse.Content.ReadAsStringAsync();
            Console.WriteLine($" - GET \"{baseUrl}/hello/{name}\" --->  Result: {helloMessage}\n");

            var data = new UserAccount() { Name = "Tommaso" };
            var registerUserResponse = await httpClient.PostAsJsonAsync($"{baseUrl}/registerUser", data);
            registerUserResponse.EnsureSuccessStatusCode();
            var userAccount = await registerUserResponse.Content.ReadFromJsonAsync<UserAccount>();
            if (userAccount != null)
            {
                Console.WriteLine($" - POST \"{baseUrl}/registerUser\" ---> Result: {userAccount}\n");
            }
            /**/

            {
                var info = options.CreateServiceInfoRequest(nameof(CallUsingHttpClientAsync));

                var postResponse = await httpClient.PostAsJsonAsync($"{baseUrl}/service", info);
                postResponse.EnsureSuccessStatusCode();
                var infoResponse = await postResponse.Content.ReadFromJsonAsync<ServiceInfoResponse>();
                if (infoResponse != null)
                {
                    Console.WriteLine($" - POST \"{baseUrl}/service\" ---> Result:");
                    Console.WriteLine($"{JsonSerializer.Serialize(infoResponse, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}\n");
                }
            }
        }

    }


    async Task CallDaprUsingHttpClientAsync()
    {
        using var httpClient = new HttpClient();

        var daprPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT") ?? "3500";

        Console.WriteLine($"DAPR_HTTP_PORT: {daprPort}");
        Console.WriteLine($"DAPR {nameof(options.ServiceAppId)}: {options.ServiceAppId} or service chains: {options.ServicesChainAppId}");

        var baseUrl = $"http://localhost:{daprPort}/v1.0";

        string daprUrl = $"{baseUrl}/invoke/{options.ServiceToCall()}/method";
        {
            /**/
            var helloResponse = new HttpResponseMessage();
            var helloMessage = "";


            {
                helloResponse = await httpClient.GetAsync($"{daprUrl}/");
                helloResponse.EnsureSuccessStatusCode();
                helloMessage = await helloResponse.Content.ReadAsStringAsync();
                Console.WriteLine($" - GET \"with httpClient {daprUrl}/\" ---> Result: {helloMessage}\n");
            }

            {
                var name = "Tommaso";
                helloResponse = await httpClient.GetAsync($"{daprUrl}/hello/{name}");
                helloResponse.EnsureSuccessStatusCode();
                helloMessage = await helloResponse.Content.ReadAsStringAsync();
                Console.WriteLine($" - GET \"with httpClient {daprUrl}/hello/{name}\" --->  Result: {helloMessage}\n");
                {
                    var data = new UserAccount() { Name = "Tommaso" };
                    var registerUserResponse = await httpClient.PostAsJsonAsync($"{daprUrl}/registerUser", data);
                    registerUserResponse.EnsureSuccessStatusCode();
                    var userAccount = await registerUserResponse.Content.ReadFromJsonAsync<UserAccount>();
                    if (userAccount != null)
                    {
                        Console.WriteLine($" - POST \"with httpClient {daprUrl}/registerUser\" ---> Result: {userAccount}\n");
                    }
                }
            }


            /**/

            {
                var info = options.CreateServiceInfoRequest(nameof(CallDaprUsingHttpClientAsync));

                var postResponse = await httpClient.PostAsJsonAsync($"{daprUrl}/service", info);
                postResponse.EnsureSuccessStatusCode();
                var infoResponse = await postResponse.Content.ReadFromJsonAsync<ServiceInfoResponse>();
                if (infoResponse != null)
                {
                    Console.WriteLine($" - POST \"with httpClient {daprUrl}/service\" ---> Result:");
                    Console.WriteLine($"{JsonSerializer.Serialize(infoResponse, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}\n");
                }
            }
        }
    }

    async Task CallUsingHttpDaprClientAsync()
    {
        using var httpDaprClient = DaprClient.CreateInvokeHttpClient(appId: options.ServiceToCall());

        //if (options.ServiceAppId != default)
        {
            /**/

            Console.WriteLine($"httpDaprClient BaseAddress: {httpDaprClient.BaseAddress}");

            var helloResponse = new HttpResponseMessage();
            var helloMessage = "";

            helloResponse = await httpDaprClient.GetAsync("");
            helloResponse.EnsureSuccessStatusCode();
            helloMessage = await helloResponse.Content.ReadAsStringAsync();
            Console.WriteLine($" - GET \"with httpDaprClient {httpDaprClient.BaseAddress} \" ---> Result: {helloMessage}\n");

            var name = "Tommaso";
            helloResponse = await httpDaprClient.GetAsync($"hello/{name}");
            helloResponse.EnsureSuccessStatusCode();
            helloMessage = await helloResponse.Content.ReadAsStringAsync();
            Console.WriteLine($" - GET \"with httpDaprClient {httpDaprClient.BaseAddress} \" --->  Result: {helloMessage}\n");

            var data = new UserAccount() { Name = "Tommaso" };
            var registerUserResponse = await httpDaprClient.PostAsJsonAsync("registerUser", data);
            registerUserResponse.EnsureSuccessStatusCode();
            var userAccount = await registerUserResponse.Content.ReadFromJsonAsync<UserAccount>();
            if (userAccount != null)
            {
                Console.WriteLine($" - POST \"with httpDaprClient {httpDaprClient.BaseAddress}\" ---> Result: {userAccount}\n");
            }
            /**/

            {
                var info = options.CreateServiceInfoRequest(nameof(CallUsingHttpDaprClientAsync));

                var postResponse = await httpDaprClient.PostAsJsonAsync("service", info);
                postResponse.EnsureSuccessStatusCode();
                var infoResponse = await postResponse.Content.ReadFromJsonAsync<ServiceInfoResponse>();
                if (infoResponse != null)
                {
                    Console.WriteLine($" - POST \"with httpDaprClient {httpDaprClient.BaseAddress}\" ---> Result:");
                    Console.WriteLine($"{JsonSerializer.Serialize(infoResponse, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}\n");
                }
            }
        }
    }

    async Task CallUsingDaprClientAsync()
    {
        using var daprClient = new DaprClientBuilder().Build();
        //if (options.ServiceAppId != default)
        {
            /**/
            var helloMessage = "";
            {
                helloMessage = await daprClient.InvokeMethodAsync<string>(HttpMethod.Get, options.ServiceToCall(), "");
                Console.WriteLine($" - GET \"with daprClient (appId {options.ServiceToCall()})\" ---> Result: {helloMessage}\n");
            }

            {
                var name = "Tommaso";
                helloMessage = await daprClient.InvokeMethodAsync<string>(HttpMethod.Get, options.ServiceToCall(), "hello/" + name);
                Console.WriteLine($" - GET \"\"with daprClient (appId {options.ServiceToCall()} \"hello/\")\" --->  Result: {helloMessage}\n");
            }

            {
                var data = new UserAccount() { Name = "Tommaso" };
                var userAccount = await daprClient.InvokeMethodAsync<UserAccount, UserAccount>(HttpMethod.Post, options.ServiceToCall(), "registerUser", data);
                Console.WriteLine($" - POST \"with daprClient (appId {options.ServiceToCall()}  \"registerUser\")\" ---> Result: {userAccount}\n");
            }
            /**/

            {
                var info = options.CreateServiceInfoRequest($"CLIENT - Services Chain: {options.ServicesChainAppId?.Replace("->", " ---D ")}");

                var infoResponse = await daprClient.InvokeMethodAsync<ServiceInfoRequest, ServiceInfoResponse>(HttpMethod.Post, options.ServiceToCall(), "service", info);

                if (infoResponse != null)
                {
                    Console.WriteLine($" - POST \"with daprClient (appId {options.ServiceToCall()} \"service\")\" ---> Result:");
                    Console.WriteLine($"{JsonSerializer.Serialize(infoResponse, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })}\n");
                }
            }
        }
    }

}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    //Console.BackgroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"=========== ERROR: {ex.Message} ============");
    Console.ResetColor();

    Console.ForegroundColor = ConsoleColor.White;
    Console.BackgroundColor = ConsoleColor.Black;
}