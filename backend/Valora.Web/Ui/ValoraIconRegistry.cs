namespace Valora.Web.Ui;

public sealed class ValoraIconRegistry
{
    private static readonly IReadOnlyDictionary<string, string> Icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["home"] = "<path d=\"m3 11 9-8 9 8\"/><path d=\"M5 10v10h14V10\"/><path d=\"M9 20v-6h6v6\"/>",
        ["layout-dashboard"] = "<rect width=\"7\" height=\"9\" x=\"3\" y=\"3\" rx=\"1\"/><rect width=\"7\" height=\"5\" x=\"14\" y=\"3\" rx=\"1\"/><rect width=\"7\" height=\"9\" x=\"14\" y=\"12\" rx=\"1\"/><rect width=\"7\" height=\"5\" x=\"3\" y=\"16\" rx=\"1\"/>",
        ["activity"] = "<path d=\"M3 12h4l3-9 4 18 3-9h4\"/>",
        ["clipboard-check"] = "<rect width=\"14\" height=\"18\" x=\"5\" y=\"3\" rx=\"2\"/><path d=\"M9 3V1h6v2M9 13l2 2 4-4\"/>",
        ["file-question"] = "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z\"/><path d=\"M14 2v6h6M9.5 13a2.5 2.5 0 1 1 3.5 2.3V17M12 20h.01\"/>",
        ["users"] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75\"/>",
        ["building"] = "<rect width=\"16\" height=\"20\" x=\"4\" y=\"2\" rx=\"2\"/><path d=\"M9 22v-4h6v4M8 6h.01M16 6h.01M8 10h.01M16 10h.01M8 14h.01M16 14h.01\"/>",
        ["network"] = "<rect width=\"6\" height=\"6\" x=\"9\" y=\"2\" rx=\"1\"/><rect width=\"6\" height=\"6\" x=\"3\" y=\"16\" rx=\"1\"/><rect width=\"6\" height=\"6\" x=\"15\" y=\"16\" rx=\"1\"/><path d=\"M6 16v-3h12v3M12 8v5\"/>",
        ["file-text"] = "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z\"/><path d=\"M14 2v6h6M8 13h8M8 17h8M8 9h2\"/>",
        ["chart-line"] = "<path d=\"M3 3v18h18\"/><path d=\"m7 16 4-5 4 3 5-7\"/>",
        ["award"] = "<circle cx=\"12\" cy=\"8\" r=\"6\"/><path d=\"M15.5 13 17 22l-5-3-5 3 1.5-9\"/>",
        ["mail"] = "<rect width=\"20\" height=\"16\" x=\"2\" y=\"4\" rx=\"2\"/><path d=\"m22 7-10 6L2 7\"/>",
        ["bell"] = "<path d=\"M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4\"/>",
        ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"8\"/><path d=\"m21 21-4.3-4.3\"/>",
        ["plus"] = "<path d=\"M12 5v14M5 12h14\"/>", ["minus"] = "<path d=\"M5 12h14\"/>",
        ["eye"] = "<path d=\"M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>",
        ["eye-off"] = "<path d=\"m3 3 18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.9 4.2A10.5 10.5 0 0 1 22 12a16 16 0 0 1-2.1 3.2M6.6 6.6A15 15 0 0 0 2 12s3.5 7 10 7a10 10 0 0 0 3.4-.6\"/>",
        ["settings"] = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1-2.8 2.8-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.6v.2h-4V21a1.7 1.7 0 0 0-1-1.6 1.7 1.7 0 0 0-1.9.3l-.1.1L4.2 17l.1-.1a1.7 1.7 0 0 0 .3-1.9A1.7 1.7 0 0 0 3 14H2.8v-4H3a1.7 1.7 0 0 0 1.6-1 1.7 1.7 0 0 0-.3-1.9L4.2 7 7 4.2l.1.1a1.7 1.7 0 0 0 1.9.3A1.7 1.7 0 0 0 10 3V2.8h4V3a1.7 1.7 0 0 0 1 1.6 1.7 1.7 0 0 0 1.9-.3l.1-.1L19.8 7l-.1.1a1.7 1.7 0 0 0-.3 1.9 1.7 1.7 0 0 0 1.6 1h.2v4H21a1.7 1.7 0 0 0-1.6 1Z\"/>",
        ["chevron-left"] = "<path d=\"m15 18-6-6 6-6\"/>", ["chevron-right"] = "<path d=\"m9 18 6-6-6-6\"/>",
        ["chevron-down"] = "<path d=\"m6 9 6 6 6-6\"/>", ["chevron-up"] = "<path d=\"m18 15-6-6-6 6\"/>",
        ["menu"] = "<path d=\"M4 6h16M4 12h16M4 18h16\"/>", ["x"] = "<path d=\"M18 6 6 18M6 6l12 12\"/>",
        ["log-out"] = "<path d=\"M10 17l5-5-5-5M15 12H3M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4\"/>",
        ["help-circle"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M9.1 9a3 3 0 1 1 5.8 1c0 2-3 2-3 4M12 18h.01\"/>",
        ["more-horizontal"] = "<circle cx=\"5\" cy=\"12\" r=\"1\"/><circle cx=\"12\" cy=\"12\" r=\"1\"/><circle cx=\"19\" cy=\"12\" r=\"1\"/>"
    };

    private static readonly string[] Aliases = ["send", "link", "qr-code", "user-plus", "user-check", "user-x", "shield", "shield-check", "lock", "unlock", "key", "buildings", "layers", "map-pin", "folder-tree", "chart-radar", "bar-chart", "certificate", "message-circle", "edit", "trash", "download", "upload", "share", "copy", "filter", "calendar", "clock", "refresh", "info", "alert-triangle", "check-circle", "x-circle", "external-link", "arrow-up-right"];

    public bool TryGet(string name, out string markup)
    {
        if (Icons.TryGetValue(name, out markup!)) return true;
        if (Aliases.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            markup = Icons["activity"];
            return true;
        }
        markup = string.Empty;
        return false;
    }
}
