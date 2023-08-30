using CommandLine;

namespace Server;

public class ServerCommandOptions
{
    [Option('n', "ServiceName", Required = true, HelpText = "Set Service Name.")]
    public string? ServiceName { get; set; }

    [Option('d', "UseDapr", Required = false, HelpText = "Use Dapr to make call.")]
    public bool UseDapr { get; set; }

    [Option('p', "Port", Required = true, HelpText = "Set Service Port.")]
    public int Port { get; set; }

}
