using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Tripletex.Api.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTripletex(
        this IServiceCollection services,
        Action<TripletexOptions> configure)
    {
        services.AddOptions<TripletexOptions>()
            .Configure(configure)
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConsumerToken), "ConsumerToken must be provided.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.EmployeeToken), "EmployeeToken must be provided.")
            .ValidateOnStart();

        services.AddSingleton(sp => sp.GetRequiredService<IOptions<TripletexOptions>>().Value);
        services.AddSingleton<TripletexClient>();

        return services;
    }
}
