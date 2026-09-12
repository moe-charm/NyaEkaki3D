using System;
using System.Diagnostics;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        [Serializable] sealed class WorkbenchCommandMeasurement
        {
            public CommandTimings core;
            public double envelopeMs,guiMs;
        }
        bool measureCommands;
        WorkbenchCommandMeasurement commandMeasurement;
        CommandResult ExecuteMeasured(AuthoringOperation[] operations)
        {
            if(!measureCommands) return commands.Execute(workspace.NewCommand(operations),projection);
            commandMeasurement=new WorkbenchCommandMeasurement { core=new CommandTimings() };
            var watch=Stopwatch.StartNew();var envelope=workspace.NewCommand(operations);commandMeasurement.envelopeMs=watch.Elapsed.TotalMilliseconds;
            return commands.Execute(envelope,projection,commandMeasurement.core);
        }
    }
}
