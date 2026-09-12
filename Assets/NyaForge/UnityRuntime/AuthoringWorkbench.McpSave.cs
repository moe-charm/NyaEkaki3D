using System;
using System.IO;
using NyaForge.Authoring;
using Newtonsoft.Json.Linq;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        string McpSaveDirectory()=>Path.GetFullPath(projectPath.value).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        JObject McpSaveTarget()
        {
            string directory=McpSaveDirectory();
            return new JObject { ["directory"]=directory,["expectedSaveVersion"]=string.Equals(directory,savedDirectory,StringComparison.OrdinalIgnoreCase) ? workspace.SaveVersion : 0 };
        }
        JObject SaveMcpProject(ProjectSaveRequest request)
        {
            try
            {
                string directory=Path.GetFullPath(request.Directory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                if(!string.Equals(directory,McpSaveDirectory(),StringComparison.OrdinalIgnoreCase)) return new JObject { ["success"]=false,["code"]="SAVE_TARGET_CHANGED",["message"]="Use the destination currently selected in the GUI." };
                var result=ProjectSaveService.Save(workspace,request);
                saveIncomplete=false;
                savedDirectory=result.Directory;projectPath.SetValueWithoutNotify(result.Directory);Refresh();SetStatus("AIから制作データを保存しました："+result.Directory);
                return new JObject { ["success"]=true,["code"]="OK",["directory"]=result.Directory,["documentId"]=result.DocumentId,["revision"]=result.Revision,["stateHash"]=result.StateHash,["saveVersion"]=result.SaveVersion };
            }
            catch(AuthoringException e) { return new JObject { ["success"]=false,["code"]=e.Code,["message"]=e.Message }; }
        }
    }
}
