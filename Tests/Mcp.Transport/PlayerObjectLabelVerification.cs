using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NyaForge.Mcp;

internal static class PlayerObjectLabelVerification
{
    static async Task<JsonElement> CallLabel(McpClient client, ObjectLabelCommand label, CancellationToken token)
    {
        var response = await client.CallToolAsync("forge_set_object_label",
            new Dictionary<string, object?> { ["label"] = label }, cancellationToken: token);
        if (response.IsError == true)
            throw new Exception("MCP object-label probe failed: " + string.Join("", response.Content.OfType<TextContentBlock>().Select(c => c.Text)));
        using var json = JsonDocument.Parse(string.Join("", response.Content.OfType<TextContentBlock>().Select(c => c.Text)));
        return json.RootElement.Clone();
    }

    public static async Task RunAsync(McpClient client, CancellationToken token)
    {
        var before = await PlayerApplyVerification.Call(client, "forge_get_state", null, token);
        if (before.GetProperty("objects").GetArrayLength() == 0) throw new Exception("Object-label probe needs an authored object");
        var objectId = before.GetProperty("objects")[0].GetProperty("objectId").GetString()!;
        var label = new ObjectLabelCommand
        {
            documentId = before.GetProperty("documentId").GetString()!,
            expectedRevision = before.GetProperty("revision").GetInt64(),
            expectedAttachmentsHash = before.GetProperty("attachmentsHash").GetString()!,
            objectId = objectId,
            displayName = "外部MCPボディ"
        };
        var changed = await CallLabel(client, label, token);
        if (!changed.GetProperty("success").GetBoolean() || changed.GetProperty("displayName").GetString() != label.displayName)
            throw new Exception("External MCP display-name update was rejected: " + changed);
        var current = await PlayerApplyVerification.Call(client, "forge_get_state", null, token);
        if (current.GetProperty("objects")[0].GetProperty("displayName").GetString() != label.displayName ||
            current.GetProperty("attachmentsHash").GetString() == before.GetProperty("attachmentsHash").GetString())
            throw new Exception("External MCP display-name update did not reach live state");
        var stale = await CallLabel(client, label, token);
        if (stale.GetProperty("success").GetBoolean() || stale.GetProperty("code").GetString() != "ATTACHMENTS_CHANGED")
            throw new Exception("Stale object-label metadata was accepted");

        await PlayerApplyVerification.Call(client, "forge_apply", PlayerApplyVerification.Command(current, Guid.NewGuid().ToString("D"), new ApplyOperation { kind = "history.undo" }), token);
        var undone = await PlayerApplyVerification.Call(client, "forge_get_state", null, token);
        if (undone.GetProperty("objects")[0].GetProperty("displayName").ValueKind != JsonValueKind.Null)
            throw new Exception("External MCP label was not removed by Undo");
        await PlayerApplyVerification.Call(client, "forge_apply", PlayerApplyVerification.Command(undone, Guid.NewGuid().ToString("D"), new ApplyOperation { kind = "history.redo" }), token);
        var redone = await PlayerApplyVerification.Call(client, "forge_get_state", null, token);
        if (redone.GetProperty("objects")[0].GetProperty("displayName").GetString() != label.displayName)
            throw new Exception("External MCP label was not restored by Redo");
        await PlayerApplyVerification.Call(client, "forge_apply", PlayerApplyVerification.Command(redone, Guid.NewGuid().ToString("D"), new ApplyOperation { kind = "history.undo" }), token);
        Console.WriteLine("PASS: external MCP object label update, stale metadata rejection and Undo/Redo");
    }
}
