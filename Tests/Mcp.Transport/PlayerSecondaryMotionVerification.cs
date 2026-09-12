using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

internal static class PlayerSecondaryMotionVerification
{
    static async Task<JsonElement> Call(McpClient client,string name,CancellationToken token)
    {
        var reply=await client.CallToolAsync(name,cancellationToken:token);
        if(reply.IsError==true) throw new Exception(name+" failed: "+string.Join("",reply.Content.OfType<TextContentBlock>().Select(c=>c.Text)));
        var text=string.Join("",reply.Content.OfType<TextContentBlock>().Select(c=>c.Text));
        using var json=JsonDocument.Parse(text);
        return json.RootElement.Clone();
    }

    static void Check(bool condition,string message)
    {
        if(!condition) throw new Exception(message);
    }

    static void CheckIdentity(JsonElement state,string instance,string document,string revision,string hash)
    {
        Check(state.GetProperty("instanceId").GetString()==instance && state.GetProperty("documentId").GetString()==document && state.GetProperty("revision").GetInt64().ToString()==revision && state.GetProperty("stateHash").GetString()==hash,
            "External MCP secondary-motion probe changed authored identity");
    }

    public static async Task RunAsync(McpClient client,string[] args,CancellationToken token)
    {
        if(args.Length!=5) throw new ArgumentException("Expected --player-secondary instance document revision stateHash");
        var tools=await client.ListToolsAsync(cancellationToken:token);
        foreach(var suffix in new[]{"state","play","pause","reset","rebuild","step"})
            Check(tools.Any(t=>t.Name=="forge_secondary_motion_"+suffix),"Secondary-motion MCP tool absent: "+suffix);

        var before=await Call(client,"forge_get_state",token);
        CheckIdentity(before,args[1],args[2],args[3],args[4]);
        var initial=await Call(client,"forge_secondary_motion_state",token);
        Check(initial.GetProperty("success").GetBoolean() && initial.GetProperty("available").GetBoolean() && initial.GetProperty("format").GetString()=="vrm1" && !initial.GetProperty("playing").GetBoolean() && initial.GetProperty("transient").GetBoolean() && !initial.GetProperty("saved").GetBoolean(),"Secondary-motion state did not describe the imported transient VRM preview");

        var play=await Call(client,"forge_secondary_motion_play",token);
        Check(play.GetProperty("success").GetBoolean() && play.GetProperty("playing").GetBoolean(),"External MCP play did not start preview");
        var pause=await Call(client,"forge_secondary_motion_pause",token);
        Check(pause.GetProperty("success").GetBoolean() && !pause.GetProperty("playing").GetBoolean(),"External MCP pause did not pause preview");
        long beforeStep=pause.GetProperty("completedSteps").GetInt64();
        var step=await Call(client,"forge_secondary_motion_step",token);
        Check(step.GetProperty("success").GetBoolean() && !step.GetProperty("playing").GetBoolean() && step.GetProperty("completedSteps").GetInt64()==beforeStep+1,"External MCP fixed-step did not advance exactly one paused step");
        var rebuilt=await Call(client,"forge_secondary_motion_rebuild",token);
        Check(rebuilt.GetProperty("success").GetBoolean() && !rebuilt.GetProperty("playing").GetBoolean() && rebuilt.GetProperty("completedSteps").GetInt64()==0,"External MCP rebuild did not reset the paused transient owner");
        var resumed=await Call(client,"forge_secondary_motion_play",token);
        Check(resumed.GetProperty("success").GetBoolean() && resumed.GetProperty("playing").GetBoolean(),"External MCP resume did not restart preview");
        var reset=await Call(client,"forge_secondary_motion_reset",token);
        Check(reset.GetProperty("success").GetBoolean() && !reset.GetProperty("playing").GetBoolean() && reset.GetProperty("completedSteps").GetInt64()==0,"External MCP reset did not discard preview");

        var after=await Call(client,"forge_get_state",token);
        CheckIdentity(after,args[1],args[2],args[3],args[4]);
        Console.WriteLine("PASS: external MCP secondary-motion lifecycle, fixed-step and authored-state preservation");
    }
}
