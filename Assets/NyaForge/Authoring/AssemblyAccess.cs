using System.Runtime.CompilerServices;

// The host's in-Player verification uses controlled builders to exercise the real worker queue.
[assembly: InternalsVisibleTo("NyaForge.UnityRuntime")]
