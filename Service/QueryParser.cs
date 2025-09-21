namespace Dark;

public record QueryField(string Name, IEnumerable<IEnumerable<(string Key, string? Value)>> AndGroups);

public record QueryGroup(IEnumerable<QueryField> Fields);

public static class QueryParser
{
    public static List<QueryGroup> Parse(string query)
    {
        query = query.Replace(' ', '+'); // Ersetze Leerzeichen durch +, da diese durch URL-Decoding verloren gehen
        List<QueryGroup> groups = [];

        var orGroups = query.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var groupStr in orGroups)
        {
            List<QueryField> fields = [];

            var fieldParts = groupStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var fieldStr in fieldParts)
            {
                var parts = fieldStr.Split('=', 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2) continue;

                // Für date: Nur ODER (|) und Bereich (Tilde ~) erlauben
                if (parts[0].Equals("date", StringComparison.OrdinalIgnoreCase))
                {
                    var orGroup = parts[1]
                        .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(x =>
                        {
                            if (x.Contains('~'))
                            {
                                var range = x.Split('~', 2, StringSplitOptions.TrimEntries);
                                return (Key: range[0], Value: range.Length == 2 ? range[1] : null);
                            }
                            return (Key: x, Value: null);
                        });
                    fields.Add(new(parts[0], [orGroup]));
                }
                else
                {
                    var andGroups = parts[1]
                        .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(andPart => andPart
                            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(x =>
                            {
                                var kv = x.Split(':', 2, StringSplitOptions.TrimEntries);
                                return (kv[0], kv.Length == 2 ? kv[1] : null);
                            })
                        );
                    fields.Add(new(parts[0], andGroups));
                }
            }
            groups.Add(new(fields));
        }
        return groups;
    }
}
