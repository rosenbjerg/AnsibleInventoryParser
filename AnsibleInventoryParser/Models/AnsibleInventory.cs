namespace AnsibleInventoryParser.Models;

public class AnsibleInventory<TAnsibleServer>(AnsibleGroup<TAnsibleServer>[] groups)
    where TAnsibleServer : AnsibleServer
{
    public AnsibleGroup<TAnsibleServer>[] Groups { get; } = groups;
    
    public AnsibleGroup<TAnsibleServer>? this[string groupName] => Groups.FirstOrDefault(g => g.Name == groupName);
}