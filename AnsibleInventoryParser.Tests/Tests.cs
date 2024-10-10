using AnsibleInventoryParser.Models;
using Newtonsoft.Json;

namespace AnsibleInventoryParser.Tests;

public class Tests
{
    public string TestIni { get; set; } = @"
# fmt: ini
# Example 1
[web]
host1
host2 ansible_port=222 # defined inline, interpreted as an integer

[web:vars]
http_port=8080 # all members of 'web' will inherit these
myvar=23 # defined in a :vars section, interpreted as a string

[web:children] # child groups will automatically add their hosts to parent group
apache
nginx

[apache]
tomcat1
tomcat2 myvar=34 # host specific vars override group vars
tomcat3 mysecret=""'03#pa33w0rd'"" # proper quoting to prevent value changes

[nginx]
jenkins1

[nginx:vars]
has_java = True # vars in child groups override same in parent

[all:vars]
has_java = False # 'all' is 'top' parent

# Example 2
host1 # this is 'ungrouped'

# both hosts have same IP but diff ports, also 'ungrouped'
host2 ansible_host=127.0.0.1 ansible_port=44
host3 ansible_host=127.0.0.1 ansible_port=45

[g1]
host4

[g2]
host4 # same host as above, but member of 2 groups, will inherit vars from both
      # inventory hostnames are unique
";


    [Test]
    public void Verify_web_group()
    {
        var parsed = AnsibleHostsFileParser.Parse<TestAnsibleServer>(TestIni.Split(Environment.NewLine));
        
        Assert.That(parsed["web"], Is.Not.Null);
        Assert.That(parsed["web"]["host1"], Is.Not.Null);
        Assert.That(parsed["web"]["host1"]!.Myvar, Is.EqualTo(23));
        Assert.That(parsed["web"]["host2"], Is.Not.Null);
        Assert.That(parsed["web"]["host2"].AnsiblePort, Is.EqualTo(222));
        Assert.That(parsed["web"]["tomcat1"], Is.Not.Null);
        Assert.That(parsed["web"]["tomcat1"]!.Myvar, Is.EqualTo(23));
        Assert.That(parsed["web"]["tomcat2"], Is.Not.Null);
        Assert.That(parsed["web"]["tomcat2"]!.Myvar, Is.EqualTo(34));
        Assert.That(parsed["web"]["tomcat3"], Is.Not.Null);
        Assert.That(parsed["web"]["tomcat3"]!.Myvar, Is.EqualTo(23));
        Assert.That(parsed["web"]["jenkins1"], Is.Not.Null);
    }
    
    [Test]
    public void Verify_nginx_group()
    {
        var parsed = AnsibleHostsFileParser.Parse<TestAnsibleServer>(TestIni.Split(Environment.NewLine));
        
        Assert.That(parsed["nginx"], Is.Not.Null);
        Assert.That(parsed["nginx"]["jenkins1"], Is.Not.Null);
    }
    
    [Test]
    public void Verify_apache_group()
    {
        var parsed = AnsibleHostsFileParser.Parse<TestAnsibleServer>(TestIni.Split(Environment.NewLine));
        
        Assert.That(parsed["apache"], Is.Not.Null);
        Assert.That(parsed["apache"].Count(), Is.EqualTo(3));
        Assert.That(parsed["apache"].All(s => s.Hostname.StartsWith("tomcat")), Is.True);
        Assert.That(parsed["apache"]["tomcat2"]!.Myvar, Is.EqualTo(34));
        Assert.That(parsed["apache"]["tomcat3"]!.Mysecret, Is.EqualTo("03#pa33w0rd"));
    }
    
    [Test]
    public void Verify_ungrouped()
    {
        var parsed = AnsibleHostsFileParser.Parse<TestAnsibleServer>(TestIni.Split(Environment.NewLine));
        
        Assert.That(parsed["ungrouped"], Is.Not.Null);
        Assert.That(parsed["ungrouped"].Count(), Is.EqualTo(3));
        Assert.That(parsed["ungrouped"].All(s => s.Hostname.StartsWith("host")), Is.True);
        Assert.That(parsed["ungrouped"]["host2"]!.AnsibleHost, Is.EqualTo("127.0.0.1"));
        Assert.That(parsed["ungrouped"]["host2"]!.AnsiblePort, Is.EqualTo(44));
        Assert.That(parsed["ungrouped"]["host3"]!.AnsibleHost, Is.EqualTo("127.0.0.1"));
        Assert.That(parsed["ungrouped"]["host3"]!.AnsiblePort, Is.EqualTo(45));
    }
    
    [Test]
    public void Verify_g1_g2()
    {
        var parsed = AnsibleHostsFileParser.Parse<TestAnsibleServer>(TestIni.Split(Environment.NewLine));
        
        Assert.That(parsed["g1"], Is.Not.Null);
        Assert.That(parsed["g1"]["host4"], Is.Not.Null);
        Assert.That(parsed["g2"], Is.Not.Null);
        Assert.That(parsed["g2"]["host4"], Is.Not.Null);
    }
    
    private class TestAnsibleServer : AnsibleServer
    {
        public int? Myvar { get; set; }
        public string? Mysecret { get; set; }
        public string? AnsibleHost { get; set; }
        public int? AnsiblePort { get; set; }
    }
}