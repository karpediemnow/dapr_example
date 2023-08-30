using MyModel;

namespace Client
{
    public static class Helper
    {
        public static ServiceInfoRequest CreateServiceInfoRequest(this ClientCommandOptions options, string fromStr)
        {
            var info = new ServiceInfoRequest() { Greatings = $"Start call from: {fromStr}" };


            if (options?.ServiceChains != default)
            {
                // service_a->service_b->service_c

                info.ServiceChains = options.ServiceChains;

                info.NextServiceIndexToCall = 1;
            }

            return info;
        }
    }
}
