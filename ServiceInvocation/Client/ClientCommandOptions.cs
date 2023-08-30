using CommandLine;

namespace Client;

public class ClientCommandOptions
{
    [Option('d', "UseDapr", Required = false, HelpText = "Use Dapr to make call.")]
    public bool UseDapr { get; set; }

    [Option('s', "Service", Required = false, HelpText = "Service Name To call ex: service_a")]
    public string? ServiceAppId { get; set; }

    [Option('c', "ServicesChain", Required = false, HelpText = "Names of Services To call ex: service_a->service_b->service_c.")]
    public string? ServicesChainAppId { get; set; }

    public string[]? ServiceChains { get; private set; }

    public string? ServiceToCall()
    {
        if(ServiceChains == default)
            this.Init();

        if (ServiceChains != null)
            return ServiceChains[0];
        else
            return ServiceAppId;
    }

    private void Init() => ServiceChains = ServicesChainAppId?.Split("->");
}
