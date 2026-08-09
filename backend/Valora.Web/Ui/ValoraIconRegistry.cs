namespace Valora.Web.Ui;

public sealed class ValoraIconRegistry
{
    private static readonly IReadOnlyDictionary<string, string> Icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["home"] = "<path d=\"m3 11 9-8 9 8\"/><path d=\"M5 10v10h14V10M9 20v-6h6v6\"/>",
        ["layout-dashboard"] = "<rect x=\"3\" y=\"3\" width=\"7\" height=\"9\" rx=\"1\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"5\" rx=\"1\"/><rect x=\"14\" y=\"12\" width=\"7\" height=\"9\" rx=\"1\"/><rect x=\"3\" y=\"16\" width=\"7\" height=\"5\" rx=\"1\"/>",
        ["activity"] = "<path d=\"M3 12h4l3-9 4 18 3-9h4\"/>",
        ["clipboard-check"] = "<rect x=\"5\" y=\"3\" width=\"14\" height=\"18\" rx=\"2\"/><path d=\"M9 3V1h6v2M9 13l2 2 4-4\"/>",
        ["file-question"] = "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z M14 2v6h6M9.5 13a2.5 2.5 0 1 1 3.5 2.3V17M12 20h.01\"/>",
        ["send"] = "<path d=\"m22 2-7 20-4-9-9-4Z M22 2 11 13\"/>",
        ["link"] = "<path d=\"M10 13a5 5 0 0 0 7.1.1l2-2a5 5 0 0 0-7.1-7.1l-1.1 1.1M14 11a5 5 0 0 0-7.1-.1l-2 2A5 5 0 0 0 12 20l1.1-1.1\"/>",
        ["qr-code"] = "<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\"/><path d=\"M14 14h3v3h-3zM21 14v3M17 21h4v-4M14 21v-1\"/>",
        ["users"] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2M22 21v-2a4 4 0 0 0-3-3.9M16 3.1a4 4 0 0 1 0 7.8\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/>",
        ["user-plus"] = "<path d=\"M15 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M19 8v6M22 11h-6\"/><circle cx=\"8\" cy=\"7\" r=\"4\"/>",
        ["user-check"] = "<path d=\"M15 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M16 11l2 2 4-4\"/><circle cx=\"8\" cy=\"7\" r=\"4\"/>",
        ["user-x"] = "<path d=\"M15 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2M18 8l4 4M22 8l-4 4\"/><circle cx=\"8\" cy=\"7\" r=\"4\"/>",
        ["shield"] = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10\"/>",
        ["shield-check"] = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10M9 12l2 2 4-4\"/>",
        ["lock"] = "<rect x=\"4\" y=\"10\" width=\"16\" height=\"12\" rx=\"2\"/><path d=\"M8 10V6a4 4 0 0 1 8 0v4\"/>",
        ["unlock"] = "<rect x=\"4\" y=\"10\" width=\"16\" height=\"12\" rx=\"2\"/><path d=\"M8 10V6a4 4 0 0 1 7.5-2\"/>",
        ["key"] = "<circle cx=\"7.5\" cy=\"15.5\" r=\"5.5\"/><path d=\"m11 12 9-9M15 8l3 3M17 6l2 2\"/>",
        ["building"] = "<rect x=\"4\" y=\"2\" width=\"16\" height=\"20\" rx=\"2\"/><path d=\"M9 22v-4h6v4M8 6h.01M16 6h.01M8 10h.01M16 10h.01M8 14h.01M16 14h.01\"/>",
        ["buildings"] = "<path d=\"M3 21h18M6 21V5l6-3v19M12 8h6v13M9 7h.01M9 11h.01M9 15h.01M15 11h.01M18 11h.01M15 15h.01M18 15h.01\"/>",
        ["network"] = "<rect x=\"9\" y=\"2\" width=\"6\" height=\"6\" rx=\"1\"/><rect x=\"3\" y=\"16\" width=\"6\" height=\"6\" rx=\"1\"/><rect x=\"15\" y=\"16\" width=\"6\" height=\"6\" rx=\"1\"/><path d=\"M6 16v-3h12v3M12 8v5\"/>",
        ["layers"] = "<path d=\"m12 2 9 5-9 5-9-5 9-5ZM3 12l9 5 9-5M3 17l9 5 9-5\"/>",
        ["map-pin"] = "<path d=\"M20 10c0 5-8 12-8 12S4 15 4 10a8 8 0 1 1 16 0\"/><circle cx=\"12\" cy=\"10\" r=\"3\"/>",
        ["folder-tree"] = "<path d=\"M3 3v18M3 7h5M3 17h5M8 5h6v4H8zM8 15h6v4H8zM14 7h3v8\"/>",
        ["file-text"] = "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z M14 2v6h6M8 13h8M8 17h8M8 9h2\"/>",
        ["chart-radar"] = "<path d=\"m12 2 9 7-3.5 11h-11L3 9l9-7Z M12 2v18M3 9l14.5 11M21 9 6.5 20\"/>",
        ["brain"] = "<path d=\"M9.5 4.5A3 3 0 0 0 4 6v.5A3.5 3.5 0 0 0 3 13a3 3 0 0 0 3 4 3 3 0 0 0 3.5 2.5V4.5Zm5 0A3 3 0 0 1 20 6v.5a3.5 3.5 0 0 1 1 6.5 3 3 0 0 1-3 4 3 3 0 0 1-3.5 2.5V4.5ZM9.5 9H7.75A1.75 1.75 0 0 0 6 10.75M14.5 9h1.75A1.75 1.75 0 0 1 18 10.75M9.5 15H8a2 2 0 0 1-2-2m8.5 2H16a2 2 0 0 0 2-2\"/>",
        ["chart-line"] = "<path d=\"M3 3v18h18M7 16l4-5 4 3 5-7\"/>",
        ["bar-chart"] = "<path d=\"M3 3v18h18M7 16v-4M12 16V8M17 16V5\"/>",
        ["award"] = "<circle cx=\"12\" cy=\"8\" r=\"6\"/><path d=\"m15.5 13 1.5 9-5-3-5 3 1.5-9\"/>",
        ["certificate"] = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"M8 7h8M8 11h5M9 17v5l3-2 3 2v-5\"/>",
        ["mail"] = "<rect x=\"2\" y=\"4\" width=\"20\" height=\"16\" rx=\"2\"/><path d=\"m22 7-10 6L2 7\"/>",
        ["message-circle"] = "<path d=\"M21 11.5a8.4 8.4 0 0 1-9 8.5 9 9 0 0 1-4-.9L3 21l1.9-5A9 9 0 1 1 21 11.5Z\"/>",
        ["bell"] = "<path d=\"M18 8a6 6 0 0 0-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4\"/>",
        ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"8\"/><path d=\"m21 21-4.3-4.3\"/>",
        ["plus"] = "<path d=\"M12 5v14M5 12h14\"/>",
        ["minus"] = "<path d=\"M5 12h14\"/>",
        ["edit"] = "<path d=\"M12 20h9M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z\"/>",
        ["trash"] = "<path d=\"M3 6h18M8 6V4h8v2M19 6l-1 15H6L5 6M10 11v5M14 11v5\"/>",
        ["eye"] = "<path d=\"M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>",
        ["eye-off"] = "<path d=\"m3 3 18 18M10.6 10.6a2 2 0 0 0 2.8 2.8M9.9 4.2A10.5 10.5 0 0 1 22 12a16 16 0 0 1-2.1 3.2M6.6 6.6A15 15 0 0 0 2 12s3.5 7 10 7a10 10 0 0 0 3.4-.6\"/>",
        ["download"] = "<path d=\"M12 3v12M7 10l5 5 5-5M5 21h14\"/>",
        ["upload"] = "<path d=\"M12 15V3M7 8l5-5 5 5M5 21h14\"/>",
        ["share"] = "<circle cx=\"18\" cy=\"5\" r=\"3\"/><circle cx=\"6\" cy=\"12\" r=\"3\"/><circle cx=\"18\" cy=\"19\" r=\"3\"/><path d=\"m8.6 10.5 6.8-4M8.6 13.5l6.8 4\"/>",
        ["copy"] = "<rect x=\"8\" y=\"8\" width=\"13\" height=\"13\" rx=\"2\"/><path d=\"M16 8V5a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h3\"/>",
        ["filter"] = "<path d=\"M22 3H2l8 9.5V19l4 2v-8.5L22 3Z\"/>",
        ["calendar"] = "<rect x=\"3\" y=\"5\" width=\"18\" height=\"16\" rx=\"2\"/><path d=\"M16 3v4M8 3v4M3 11h18\"/>",
        ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M12 7v5l3 2\"/>",
        ["refresh"] = "<path d=\"M20 6v5h-5M4 18v-5h5M18.5 9A7 7 0 0 0 6 6.5L4 11M5.5 15A7 7 0 0 0 18 17.5l2-4.5\"/>",
        ["settings"] = "<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M19.4 15a2 2 0 0 0 .4 2.2l-2.6 2.6a2 2 0 0 0-2.2-.4 2 2 0 0 0-1 1.8h-4a2 2 0 0 0-1-1.8 2 2 0 0 0-2.2.4l-2.6-2.6A2 2 0 0 0 4.6 15a2 2 0 0 0-1.8-1v-4a2 2 0 0 0 1.8-1 2 2 0 0 0-.4-2.2l2.6-2.6A2 2 0 0 0 9 4.6a2 2 0 0 0 1-1.8h4a2 2 0 0 0 1 1.8 2 2 0 0 0 2.2-.4l2.6 2.6a2 2 0 0 0-.4 2.2 2 2 0 0 0 1.8 1v4a2 2 0 0 0-1.8 1Z\"/>",
        ["help-circle"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M9 9a3 3 0 1 1 5.8 1c0 2-2.8 2-2.8 4M12 18h.01\"/>",
        ["info"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"M12 11v5M12 8h.01\"/>",
        ["alert-triangle"] = "<path d=\"M10.3 3.5 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.5a2 2 0 0 0-3.4 0ZM12 9v4M12 17h.01\"/>",
        ["check-circle"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m8 12 3 3 5-6\"/>",
        ["x-circle"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><path d=\"m15 9-6 6M9 9l6 6\"/>",
        ["chevron-left"] = "<path d=\"m15 18-6-6 6-6\"/>",
        ["chevron-right"] = "<path d=\"m9 18 6-6-6-6\"/>",
        ["chevron-down"] = "<path d=\"m6 9 6 6 6-6\"/>",
        ["chevron-up"] = "<path d=\"m18 15-6-6-6 6\"/>",
        ["menu"] = "<path d=\"M4 6h16M4 12h16M4 18h16\"/>",
        ["x"] = "<path d=\"M18 6 6 18M6 6l12 12\"/>",
        ["more-horizontal"] = "<circle cx=\"5\" cy=\"12\" r=\"1\"/><circle cx=\"12\" cy=\"12\" r=\"1\"/><circle cx=\"19\" cy=\"12\" r=\"1\"/>",
        ["log-out"] = "<path d=\"m10 17 5-5-5-5M15 12H3M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4\"/>",
        ["external-link"] = "<path d=\"M15 3h6v6M10 14 21 3M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6\"/>",
        ["arrow-up-right"] = "<path d=\"M7 17 17 7M7 7h10v10\"/>",
        ["grip-vertical"] = "<circle cx=\"9\" cy=\"5\" r=\"1\"/><circle cx=\"15\" cy=\"5\" r=\"1\"/><circle cx=\"9\" cy=\"12\" r=\"1\"/><circle cx=\"15\" cy=\"12\" r=\"1\"/><circle cx=\"9\" cy=\"19\" r=\"1\"/><circle cx=\"15\" cy=\"19\" r=\"1\"/>",
        ["move"] = "<path d=\"M12 2v20M2 12h20M8 6l4-4 4 4M8 18l4 4 4-4M6 8l-4 4 4 4M18 8l4 4-4 4\"/>",
        ["save"] = "<path d=\"M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2ZM17 21v-8H7v8M7 3v5h8\"/>",
        ["undo"] = "<path d=\"M9 7 4 12l5 5M4 12h10a6 6 0 0 1 6 6\"/>",
        ["redo"] = "<path d=\"m15 7 5 5-5 5M20 12H10a6 6 0 0 0-6 6\"/>",
        ["target"] = "<circle cx=\"12\" cy=\"12\" r=\"9\"/><circle cx=\"12\" cy=\"12\" r=\"5\"/><circle cx=\"12\" cy=\"12\" r=\"1\"/>",
        ["flag"] = "<path d=\"M5 22V3M5 4h12l-2 4 2 4H5\"/>",
        ["list-checks"] = "<path d=\"m3 6 2 2 4-4M3 14l2 2 4-4M13 6h8M13 14h8M3 22l2-2M5 22l4-4M13 20h8\"/>",
        ["panel-left"] = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M9 3v18\"/>",
        ["panel-right"] = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><path d=\"M15 3v18\"/>",
        ["paperclip"] = "<path d=\"m21.4 11.6-8.9 8.9a6 6 0 0 1-8.5-8.5l9.5-9.5a4 4 0 0 1 5.7 5.7l-9.5 9.5a2 2 0 0 1-2.8-2.8l8.8-8.8\"/>",
        ["image"] = "<rect x=\"3\" y=\"3\" width=\"18\" height=\"18\" rx=\"2\"/><circle cx=\"8.5\" cy=\"8.5\" r=\"1.5\"/><path d=\"m21 15-5-5L5 21\"/>",
        ["file-up"] = "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8Z M14 2v6h6M12 18v-6M9 15l3-3 3 3\"/>",
    };

    public bool TryGet(string name, out string markup)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            markup = string.Empty;
            return false;
        }

        return Icons.TryGetValue(name, out markup!);
    }
}
