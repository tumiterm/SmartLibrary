using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartLibrary.Application.Circulation.Holds;
using SmartLibrary.Application.Common.Behaviours;

namespace SmartLibrary.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });
        services.AddValidatorsFromAssembly(assembly);
        services.AddScoped<HoldExpiryService>();

        return services;
    }
}
