using Microsoft.Extensions.DependencyInjection;

namespace Tripletex.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTripletex(
        this IServiceCollection services,
        Action<TripletexOptions> configure)
    {
        var options = new TripletexOptions
        {
            ConsumerToken = "",
            EmployeeToken = ""
        };
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<TripletexClient>();

        return services;
    }
}
