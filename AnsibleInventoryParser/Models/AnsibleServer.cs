namespace AnsibleInventoryParser.Models;

public class AnsibleServer
{
    public required string Hostname { get; init; }
    public override string ToString() => Hostname;
}