namespace Valora.Web.TagHelpers;

public static class ValoraIconRegistry
{
    private const string Circle = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 8v4l3 2\"/>";
    private const string Document = "<path d=\"M6 2h8l4 4v16H6z\"/><path d=\"M14 2v5h5M9 13h6M9 17h6\"/>";
    private const string User = "<circle cx=\"12\" cy=\"8\" r=\"4\"/><path d=\"M4 22c0-5 3-8 8-8s8 3 8 8\"/>";
    private const string Chart = "<path d=\"M4 20V10M10 20V4M16 20v-7M22 20H2\"/>";
    private const string Shield = "<path d=\"M12 2 20 5v6c0 5-3 9-8 11-5-2-8-6-8-11V5z\"/>";
    private const string Building = "<path d=\"M4 22V4h12v18M8 8h4M8 12h4M8 16h4M2 22h20M16 10h4v12\"/>";
    private const string Arrow = "<path d=\"m9 18 6-6-6-6\"/>";

    private static readonly IReadOnlyDictionary<string, string> Icons = CreateIcons();

    public static bool TryGet(string name, out string path) => Icons.TryGetValue(name, out path!);

    private static IReadOnlyDictionary<string, string> CreateIcons()
    {
        var icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "<path d=\"m3 11 9-8 9 8v10h-6v-6H9v6H3z\"/>",
            ["layout-dashboard"] = "<rect x=\"3\" y=\"3\" width=\"7\" height=\"9\" rx=\"1\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"5\" rx=\"1\"/><rect x=\"14\" y=\"12\" width=\"7\" height=\"9\" rx=\"1\"/><rect x=\"3\" y=\"16\" width=\"7\" height=\"5\" rx=\"1\"/>",
            ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"7\"/><path d=\"m20 20-4-4\"/>",
            ["bell"] = "<path d=\"M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4\"/>",
            ["menu"] = "<path d=\"M4 6h16M4 12h16M4 18h16\"/>",
            ["x"] = "<path d=\"m6 6 12 12M18 6 6 18\"/>",
            ["plus"] = "<path d=\"M12 5v14M5 12h14\"/>",
            ["minus"] = "<path d=\"M5 12h14\"/>",
            ["eye"] = "<path d=\"M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>",
            ["eye-off"] = "<path d=\"m3 3 18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.9 5.2A9 9 0 0 1 12 5c6 0 10 7 10 7a16 16 0 0 1-2.1 3M6.6 6.6A16 16 0 0 0 2 12s4 7 10 7a9 9 0 0 0 3-.5\"/>",
            ["check-circle"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"m8 12 3 3 5-6\"/>",
            ["x-circle"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"m9 9 6 6M15 9l-6 6\"/>",
            ["more-horizontal"] = "<circle cx=\"5\" cy=\"12\" r=\"1\"/><circle cx=\"12\" cy=\"12\" r=\"1\"/><circle cx=\"19\" cy=\"12\" r=\"1\"/>",
            ["log-out"] = "<path d=\"M10 17l5-5-5-5M15 12H3M15 4h5v16h-5\"/>",
            ["settings"] = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M12 2v3M12 19v3M2 12h3M19 12h3M5 5l2 2M17 17l2 2M19 5l-2 2M7 17l-2 2\"/>",
            ["download"] = "<path d=\"M12 3v12m-5-5 5 5 5-5M4 21h16\"/>",
            ["upload"] = "<path d=\"M12 17V5m-5 5 5-5 5 5M4 21h16\"/>",
            ["edit"] = "<path d=\"M4 20h4L19 9l-4-4L4 16zM13 7l4 4\"/>",
            ["trash"] = "<path d=\"M4 7h16M9 7V4h6v3M7 7l1 14h8l1-14M10 11v6M14 11v6\"/>",
            ["mail"] = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"m3 7 9 6 9-6\"/>",
            ["lock"] = "<rect x=\"5\" y=\"10\" width=\"14\" height=\"11\" rx=\"2\"/><path d=\"M8 10V7a4 4 0 0 1 8 0v3\"/>",
            ["filter"] = "<path d=\"M3 5h18l-7 8v6l-4 2v-8z\"/>",
            ["refresh"] = "<path d=\"M20 7V3l-2 2a9 9 0 1 0 2 10M20 3h-4\"/>",
            ["arrow-up-right"] = "<path d=\"M7 17 17 7M8 7h9v9\"/>",
            ["chevron-right"] = Arrow,
            ["chevron-left"] = "<path d=\"m15 18-6-6 6-6\"/>",
            ["chevron-down"] = "<path d=\"m6 9 6 6 6-6\"/>",
            ["chevron-up"] = "<path d=\"m6 15 6-6 6 6\"/>"
        };

        AddAliases(icons, Circle, "activity", "calendar", "clock", "help-circle", "info", "alert-triangle");
        AddAliases(icons, Document, "clipboard-check", "file-question", "file-text", "certificate", "award", "copy", "share", "external-link", "link", "qr-code", "send", "message-circle");
        AddAliases(icons, User, "users", "user-plus", "user-check", "user-x");
        AddAliases(icons, Shield, "shield", "shield-check", "unlock", "key");
        AddAliases(icons, Building, "building", "buildings", "network", "layers", "map-pin", "folder-tree");
        AddAliases(icons, Chart, "chart-radar", "chart-line", "bar-chart");
        return icons;
    }

    private static void AddAliases(IDictionary<string, string> icons, string path, params string[] names)
    {
        foreach (var name in names) icons[name] = path;
    }
}
