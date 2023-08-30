namespace MyModel;

public class ServiceInfoRequest
{
    public string? Greatings { get; set; }
    public string[]? ServiceChains { get; set; }
    public int NextServiceIndexToCall { get; set; }

    public object? Metadata { get; set; }
}

public class ServiceInfoResponse
{
    public string? Name { get; set; }
    public string? Message { get; set; }
    public object? Metadata { get; set; }

    public List<ServiceInfoResponse> ServiceChainInfos { get; set; }
    public override string ToString()
    {
        return $"Service Name: {Name}";
    }
}