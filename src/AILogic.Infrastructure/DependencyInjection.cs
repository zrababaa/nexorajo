using AILogic.Application.Abstractions;
using AILogic.Infrastructure.AI;
using AILogic.Infrastructure.Configuration;
using AILogic.Infrastructure.Meta;
using AILogic.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace AILogic.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        DigitalOceanAgentOptions agentOptions,
        MetaOptions metaOptions)
    {
        services.AddSingleton(agentOptions);
        services.AddSingleton(metaOptions);
        services.AddSingleton<IConversationStore, InMemoryConversationStore>();

        services.AddHttpClient<IAgentClient, DigitalOceanAgentClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<IMessengerClient, MessengerClient>(client =>
        {
            client.BaseAddress = new Uri("https://graph.facebook.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IWhatsAppClient, WhatsAppClient>(client =>
        {
            client.BaseAddress = new Uri("https://graph.facebook.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
