using System.Collections;

namespace AnsibleInventoryParser.Models;

public class AnsibleGroup<TAnsibleServer>(string name, TAnsibleServer[] hosts) : IEnumerable<TAnsibleServer>
    where TAnsibleServer : AnsibleServer
{
    public string Name { get; } = name;
    public TAnsibleServer[] Hosts { get; } = hosts;

    public override string ToString() => Name;

    public IEnumerator<TAnsibleServer> GetEnumerator() => Hosts.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public TAnsibleServer? this[string hostname] => Hosts.FirstOrDefault(g => g.Hostname == hostname);
}