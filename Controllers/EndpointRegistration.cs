using IntegrationServices.Interfaces;
using System.Reflection;

public static class EndpointRegistration
{
    public static void RegisterAllEndpoints(this IServiceCollection services, Assembly assembly)
    {
        // Scoped
        foreach (var type in assembly.GetTypes()
            .Where(t => typeof(IScopedEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
        {
            services.AddScoped(type);
        }

        // Transient
        foreach (var type in assembly.GetTypes()
            .Where(t => typeof(ITransientEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
        {
            services.AddTransient(type);
        }

        // Singleton
        foreach (var type in assembly.GetTypes()
            .Where(t => typeof(ISingletonEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
        {
            services.AddSingleton(type);
        }
    }

    public static void MapAllEndpoints(this IEndpointRouteBuilder app, Assembly assembly)
    {
        // Static endpoints first
        var staticTypes = assembly.GetTypes()
            .Where(t => typeof(IStaticEndpoint).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var type in staticTypes)
        {
            var mapMethod = type.GetMethod("Map", BindingFlags.Public | BindingFlags.Static);
            mapMethod?.Invoke(null, new object[] { app });
        }

        // Instance endpoints (Transient / Scoped / Singleton)
        var instanceTypes = assembly.GetTypes()
            .Where(t =>
                typeof(ITransientEndpoint).IsAssignableFrom(t) ||
                typeof(IScopedEndpoint).IsAssignableFrom(t) ||
                typeof(ISingletonEndpoint).IsAssignableFrom(t))
            .Where(t => !t.IsInterface && !t.IsAbstract);

        // Create a temporary scope so scoped services can be resolved
        using var scope = app.ServiceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        foreach (var type in instanceTypes)
        {
            var endpointInstance = scopedProvider.GetRequiredService(type);
            ((dynamic)endpointInstance).Map(app);
        }
    }
}