namespace Dark;

public record Event([property: JsonPropertyName("$type")] string Type, IEnumerable<string>? Tags)
{
    public IEnumerable<(string Key, string? Value)> ParsedTags =>
        Tags?.Select(t =>
        {
            var parts = t.Split(':', 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], null);
        }) ?? [];

    public bool Matches(IEnumerable<QueryGroup> groups)
    {
        foreach (var group in groups)
        {
            var groupMatches = true;
            foreach (var field in group.Fields)
            {
                if (!MatchesField(this, field))
                {
                    groupMatches = false;
                    break;
                }
            }
            if (groupMatches) return true;
        }
        return false;
    }

    static bool MatchesField(Event ev, QueryField field) => field.Name.ToLower() switch
    {
        "types" => field.AndGroups.All(x => x.Any(v => ev.Type.Equals(v.Key, StringComparison.OrdinalIgnoreCase))),
        "tags" => field.AndGroups.All(x => x.Any(tag =>
            ev.ParsedTags.Any(t =>
                t.Key.Equals(tag.Key, StringComparison.OrdinalIgnoreCase)
                && (tag.Value is null || string.Equals(t.Value, tag.Value, StringComparison.OrdinalIgnoreCase))))),
        _ => false
    };
}
