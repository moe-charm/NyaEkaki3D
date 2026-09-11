using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed class AvatarInstance : IDisposable
    {
        public readonly GameObject Root;
        public readonly PackManifest Manifest;
        public readonly Dictionary<string, Renderer> Renderers = new Dictionary<string, Renderer>();
        readonly Dictionary<string, AnimationClip> clips;
        readonly Transform[] transforms;
        readonly Vector3[] positions, scales;
        readonly Quaternion[] rotations;
        readonly Dictionary<Renderer, Material[]> original = new Dictionary<Renderer, Material[]>();
        readonly List<Material> ownedMaterials = new List<Material>();
        readonly List<(SkinnedMeshRenderer renderer, int index, float value)> defaults = new List<(SkinnedMeshRenderer, int, float)>();
        readonly Dictionary<(string, string), (SkinnedMeshRenderer renderer, int index)> morphs = new Dictionary<(string, string), (SkinnedMeshRenderer, int)>();
        PlayableGraph graph;
        AnimationClipPlayable playable;
        string currentClip, currentMode = "original";
        public AvatarInstance(GameObject prefab, PackManifest manifest, Dictionary<string, AnimationClip> loadedClips)
        {
            Manifest = manifest;
            clips = loadedClips;
            Root = UnityEngine.Object.Instantiate(prefab);
            Root.name = "PreviewAvatar";
            Root.SetActive(false);
            try
            {
                transforms = Root.GetComponentsInChildren<Transform>(true);
                positions = transforms.Select(t => t.localPosition).ToArray();
                rotations = transforms.Select(t => t.localRotation).ToArray();
                scales = transforms.Select(t => t.localScale).ToArray();
                foreach (var row in manifest.renderers)
                {
                    var t = Root.transform.Find(row.path);
                    Renderer r = row.rendererType == "SkinnedMeshRenderer" ? t?.GetComponent<SkinnedMeshRenderer>() : t?.GetComponent<MeshRenderer>();
                    if (r == null) throw new ContractException("REQUIRED_BINDING_MISSING", "パーツが見つかりません: " + row.displayName);
                    Renderers.Add(row.rendererId, r);
                    original.Add(r, r.sharedMaterials);
                    if (r.sharedMaterials.Any(m => m == null || m.shader == null || !m.shader.isSupported))
                        throw new ContractException("SHADER_UNSUPPORTED", "材質が表示できません: " + row.displayName);
                }
                foreach (var row in manifest.morphBindings)
                {
                    var r = Renderers[row.rendererId] as SkinnedMeshRenderer;
                    int i = r.sharedMesh.GetBlendShapeIndex(row.shapeName);
                    if (i < 0) throw new ContractException("REQUIRED_BINDING_MISSING", "シェイプキーがありません: " + row.shapeName);
                    morphs.Add((row.rendererId, row.shapeName), (r, i));
                    defaults.Add((r, i, row.defaultWeight));
                }
                Root.SetActive(true);
            }
            catch { UnityEngine.Object.Destroy(Root); throw; }
        }
        public void Apply(SessionDocument state, double time)
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                transforms[i].localPosition = positions[i];
                transforms[i].localRotation = rotations[i];
                transforms[i].localScale = scales[i];
            }
            foreach (var d in defaults) d.renderer.SetBlendShapeWeight(d.index, d.value);
            if (currentClip != state.motion.clipId)
            {
                if (graph.IsValid()) graph.Destroy();
                graph = PlayableGraph.Create("ViewerPose");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var animator = Root.GetComponent<Animator>() ?? Root.AddComponent<Animator>();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                playable = AnimationClipPlayable.Create(graph, clips[state.motion.clipId]);
                playable.SetApplyFootIK(false); playable.SetApplyPlayableIK(false);
                playable.SetSpeed(0);
                var output = AnimationPlayableOutput.Create(graph, "Pose", animator);
                output.SetSourcePlayable(playable);
                graph.Play();
                currentClip = state.motion.clipId;
            }
            playable.SetTime(time);
            playable.SetDone(false);
            graph.Evaluate(0);
            foreach (var row in Manifest.renderers) Renderers[row.rendererId].enabled = row.defaultVisible;
            foreach (var row in state.visibilityOverrides) Renderers[row.rendererId].enabled = row.visible;
            foreach (var row in state.morphOverrides)
                if (morphs.TryGetValue((row.rendererId, row.shapeName), out var target)) target.renderer.SetBlendShapeWeight(target.index, row.weight);
            ApplyMode(state.preview.mode);
        }
        void ApplyMode(string mode)
        {
            if (mode == currentMode) return;
            foreach (var pair in original) pair.Key.sharedMaterials = pair.Value;
            foreach (var m in ownedMaterials) UnityEngine.Object.Destroy(m);
            ownedMaterials.Clear();
            currentMode = mode;
            if (mode == "original") return;
            foreach (var row in Manifest.renderers)
            {
                if (mode == "bodyDiagnostic" && row.category != "body") continue;
                var r = Renderers[row.rendererId];
                var materials = original[r].Select(source =>
                {
                    var shader = Shader.Find(mode == "bodyDiagnostic" ? "Viewer/BodyDiagnostic" : "Viewer/UnlitColor");
                    var m = new Material(shader);
                    if (source.HasProperty("_MainTex")) m.mainTexture = source.GetTexture("_MainTex");
                    if (source.HasProperty("_MainTex")) { m.mainTextureScale = source.GetTextureScale("_MainTex"); m.mainTextureOffset = source.GetTextureOffset("_MainTex"); }
                    if (m.HasProperty("_Color")) m.color = mode == "bodyDiagnostic" ? new Color(.25f, .75f, 1, .28f) : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                    ownedMaterials.Add(m);
                    return m;
                }).ToArray();
                r.sharedMaterials = materials;
            }
        }
        public float MorphValue(string rendererId, string name)
        {
            var binding = morphs[(rendererId, name)];
            return binding.renderer.GetBlendShapeWeight(binding.index);
        }
        public void Dispose()
        {
            if (graph.IsValid()) graph.Destroy();
            if (Root) { Root.SetActive(false); UnityEngine.Object.Destroy(Root); }
            foreach (var m in ownedMaterials) UnityEngine.Object.Destroy(m);
            ownedMaterials.Clear();
        }
    }
}
