using AnsibleInventoryParser.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AnsibleInventoryParser;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnsibleInventory<TAnsibleServer>(this IServiceCollection services, string inventoryFilePath) 
        where TAnsibleServer : AnsibleServer
    {
        var inventory = AnsibleHostsFileParser.Parse<TAnsibleServer>(inventoryFilePath);
        return services.AddSingleton(inventory);
    }
}