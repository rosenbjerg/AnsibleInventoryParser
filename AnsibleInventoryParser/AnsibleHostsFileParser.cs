using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AnsibleInventoryParser.Converters;
using AnsibleInventoryParser.Models;

namespace AnsibleInventoryParser;

public static partial class AnsibleHostsFileParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter(), new BooleanConverterFactory() }
    };

    public static AnsibleInventory<TAnsibleServer> Parse<TAnsibleServer>(string inputIniFile)
        where TAnsibleServer : AnsibleServer
    {
        var lines = File.ReadLines(inputIniFile);
        return Parse<TAnsibleServer>(lines);
    }

    public static AnsibleInventory<TAnsibleServer> Parse<TAnsibleServer>(IEnumerable<string> lines)
        where TAnsibleServer : AnsibleServer
    {
        var hostsGroups = ParseAsDictionaries(lines);
        var nonEmptyGroups = hostsGroups.Where(g => g.Value.Count > 0).ToDictionary();
        
        var json = JsonSerializer.Serialize(nonEmptyGroups);
        var rawInventory = JsonSerializer.Deserialize<Dictionary<string, List<TAnsibleServer>>>(json, JsonOptions)!;
        
        var ansibleGroups = rawInventory.Select(g => new AnsibleGroup<TAnsibleServer>(g.Key, g.Value.ToArray())).ToArray();
        return new AnsibleInventory<TAnsibleServer>(ansibleGroups);
    }

    private static Dictionary<string, List<Dictionary<string, string>>> ParseAsDictionaries(IEnumerable<string> lines)
    {
        var groups = new Dictionary<string, List<Dictionary<string, string>>>();
        var currentGroup = new List<Dictionary<string, string>>();
        var ungrouped = new List<Dictionary<string, string>>();
        var currentGroupName = string.Empty;
        
        foreach (var line in lines)
        {
            var (trimmedLine, wasComment) = CleanLine(line);

            if (string.IsNullOrEmpty(trimmedLine))
            {
                if (!wasComment)
                {
                    currentGroup = ungrouped;
                    currentGroupName = string.Empty;
                }

                continue;
            }

            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                currentGroupName = trimmedLine[1..^1].Trim();
                currentGroup = [];
                groups.Add(currentGroupName, currentGroup);
                continue;
            }
            
            if (currentGroupName.EndsWith(":vars"))
            {
                var props = SplitProps([trimmedLine]);
                currentGroup.Add(props);
            }
            else if (currentGroupName.EndsWith(":children"))
            {
                currentGroup.Add(new Dictionary<string, string> {{"hostname", trimmedLine.Trim()}});
            }
            else
            {
                var split = trimmedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var hostname = split[0];
                var props = SplitProps(split[1..]);
                props.Add("hostname", hostname);
                currentGroup.Add(props);
            }
        }

        if (ungrouped.Count != 0)
        {
            groups.Add("ungrouped", ungrouped);
        }
        
        return Transform(groups);
    }

    private static (string cleanedLine, bool wasComment) CleanLine(string line)
    {
        var commentCanBeginFrom = 0;
        var trimmedLine = Unquote().Replace(line.Trim(), m =>
        {
            commentCanBeginFrom = m.Index + m.Groups[1].Value.Length;
            return m.Groups[1].Value!;
        });
        
        var commentStart = trimmedLine.IndexOfAny([';', '#'], commentCanBeginFrom);
        if (commentStart != -1)
        {
            trimmedLine = trimmedLine[..commentStart].TrimEnd();
        }

        return (trimmedLine, commentStart != -1);
    }

    private static Dictionary<string, List<Dictionary<string, string>>> Transform(Dictionary<string, List<Dictionary<string, string>>> groups)
    {
        var hostsGroups = groups.Where(g => !g.Key.Contains(':')).ToDictionary();
        var childrenGroups = groups.Where(g => g.Key.EndsWith(":children")).ToDictionary();
        var varsGroups = groups.Where(g => g.Key.EndsWith(":vars")).ToDictionary();
        
        foreach (var childrenGroup in childrenGroups)
        {
            var groupName = childrenGroup.Key.Split(':')[0];
            var childrenEntries = childrenGroup.Value.SelectMany(v => hostsGroups[v["hostname"]]).ToList();
            if (hostsGroups.TryGetValue(groupName, out var group))
            {
                group.AddRange(childrenEntries);
            }
            else
            {
                hostsGroups.Add(groupName, childrenEntries);
            }

        }

        foreach (var varsGroup in varsGroups)
        {
            var groupName = varsGroup.Key.Split(':')[0];
            var group = groupName != "all" ? hostsGroups[groupName] : hostsGroups.SelectMany(h => h.Value);
            foreach (var entry in group)
            {
                foreach (var prop in varsGroup.Value.SelectMany(value => value))
                {
                    if (entry.ContainsKey(prop.Key))
                        continue;
                    
                    entry[prop.Key] = prop.Value;
                }
            }
        }

        return hostsGroups;
    }

    private static Dictionary<string, string> SplitProps(string[] pairs)
    {
        return pairs
            .Select(e => e.Split('=', StringSplitOptions.RemoveEmptyEntries))
            .Where(e => e.Length == 2)
            .ToDictionary(e => e.First().TrimEnd(), e => e.Last().TrimStart());
    }

    [GeneratedRegex(@"""'(.+)'""")]
    private static partial Regex Unquote();
}