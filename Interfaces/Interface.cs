namespace IntegrationServices.Interfaces
{
    public interface IStaticEndpoint
    {
        static abstract void Map(IEndpointRouteBuilder app);
    }

    // Endpoints/ITransientEndpoint.cs
    public interface ITransientEndpoint
    {
        void Map(IEndpointRouteBuilder app);
    }

    // Endpoints/IScopedEndpoint.cs
    public interface IScopedEndpoint
    {
        void Map(IEndpointRouteBuilder app);
    }

    // Endpoints/ISingletonEndpoint.cs
    public interface ISingletonEndpoint
    {
        void Map(IEndpointRouteBuilder app);
    }


}
