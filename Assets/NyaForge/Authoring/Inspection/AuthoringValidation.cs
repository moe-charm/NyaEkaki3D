using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Rig;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Inspection
{
    /// <summary>Read-only, profile-based output budget check. It never changes the authored document.</summary>
    public sealed class AuthoringValidationRequest
    {
        public string DocumentId { get; private set; }
        public long ExpectedRevision { get; private set; }
        public string Profile { get; private set; }

        public static AuthoringValidationRequest Create(string documentId, long expectedRevision, string profile)
        {
            Checks.Id(documentId);
            Checks.Require(expectedRevision >= 0, "INVALID_VALIDATION_REQUEST", "Validation revision must be non-negative.");
            Checks.Require(profile == "pc" || profile == "mobile", "INVALID_VALIDATION_REQUEST", "Profile must be pc or mobile.");
            return new AuthoringValidationRequest { DocumentId = documentId, ExpectedRevision = expectedRevision, Profile = profile };
        }

        public static AuthoringValidationRequest Read(JObject value)
        {
            Checks.Require(value != null && value.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)
                .SequenceEqual(new[] { "documentId", "expectedRevision", "profile" }),
                "INVALID_VALIDATION_REQUEST", "Unexpected validation fields.");
            Checks.Require(value["documentId"].Type == JTokenType.String && value["profile"].Type == JTokenType.String &&
                value["expectedRevision"].Type == JTokenType.Integer, "INVALID_VALIDATION_REQUEST", "Validation fields have invalid types.");
            return Create((string)value["documentId"], (long)value["expectedRevision"], (string)value["profile"]);
        }
    }

    public static class AuthoringValidationReader
    {
        sealed class Limits
        {
            public readonly string Name;
            public readonly int Triangles;
            public readonly int Materials;
            public readonly int TextureDimension;
            public Limits(string name, int triangles, int materials, int textureDimension)
            { Name = name; Triangles = triangles; Materials = materials; TextureDimension = textureDimension; }
        }

        public static JObject Read(AuthoringWorkspace workspace, string expectedInstanceId, AuthoringValidationRequest request)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            if (request == null) throw new ArgumentNullException(nameof(request));
            lock (workspace.Gate)
            {
                Checks.Require(expectedInstanceId == workspace.InstanceId, "STALE_INSTANCE", "Select the current authoring instance before validating.");
                Checks.Require(!workspace.Executing, "REENTRANT_STATE", "Cannot validate during a command transaction.");
                Checks.Require(request.DocumentId == workspace.Document.DocumentId, "DOCUMENT_MISMATCH", "Validation document differs from the current document.");
                Checks.Require(request.ExpectedRevision == workspace.Document.DocumentRevision, "REVISION_CONFLICT", "Validation revision is stale.");

                var limits = request.Profile == "mobile" ? new Limits("mobile", 20000, 1, 1024) : new Limits("pc", 70000, 8, 2048);
                var checks = new JArray();
                var evaluations = new List<Tuple<AuthoringObject, GraphEvaluation>>();
                var incomplete = new List<string>();
                var objectMetrics = new JArray();
                foreach (var item in workspace.Document.Objects)
                {
                    GraphEvaluation evaluation = item == workspace.Document.ActiveObject
                        ? workspace.Preview?.Evaluation
                        : item.EvaluateGraph();
                    bool stale = item == workspace.Document.ActiveObject && workspace.Preview != null && workspace.Preview.IsStale;
                    if (evaluation == null || stale || !evaluation.IsComplete || evaluation.Output == null || evaluation.Output.Mesh == null)
                    {
                        incomplete.Add(item.ObjectId);
                        objectMetrics.Add(new JObject { ["objectId"] = item.ObjectId, ["status"] = "unknown" });
                        continue;
                    }
                    evaluations.Add(Tuple.Create(item, evaluation));
                    objectMetrics.Add(new JObject
                    {
                        ["objectId"] = item.ObjectId,
                        ["status"] = "pass",
                        ["triangles"] = evaluation.Output.Mesh.TriangleCount,
                        ["renderVertices"] = evaluation.Output.Mesh.VertexCount,
                        ["materials"] = evaluation.Output.SlotMaterials?.Count ?? (evaluation.Output.Material == null ? 0 : 1),
                        ["textures"] = Images(evaluation.Output).Count()
                    });
                }
                if (incomplete.Count > 0 || evaluations.Count == 0)
                {
                    var warnings = new JArray();
                    if (incomplete.Count > 0)
                        warnings.Add("Final output is incomplete or has no renderable mesh for object(s): " + string.Join(", ", incomplete));
                    else
                        warnings.Add("Final output is incomplete or has no renderable mesh.");
                    return Result(workspace, request, limits, "unknown", checks, null, null, null, null, null, null, warnings, objectMetrics);
                }

                int triangles = evaluations.Sum(pair => pair.Item2.Output.Mesh.TriangleCount);
                int vertices = evaluations.Sum(pair => pair.Item2.Output.Mesh.VertexCount);
                int materials = evaluations.Sum(pair => pair.Item2.Output.SlotMaterials?.Count ?? (pair.Item2.Output.Material == null ? 0 : 1));
                int textures = 0, maxTexture = 0;
                var imageHashes = new HashSet<string>(StringComparer.Ordinal);
                foreach (var pair in evaluations)
                    foreach (var image in Images(pair.Item2.Output))
                        if (imageHashes.Add(Checks.Hash(PaintImageCodec.Write(image))))
                        {
                            textures++;
                            maxTexture = Math.Max(maxTexture, Math.Max(image.Width, image.Height));
                        }
                AddBound(checks, "triangles", triangles, limits.Triangles, "Triangles");
                AddBound(checks, "materials", materials, limits.Materials, "Materials");
                AddBound(checks, "maxTextureDimension", maxTexture, limits.TextureDimension, "Largest texture dimension");
                var skin = new SkinSummary();
                foreach (var pair in evaluations) skin.Merge(SummarizeSkin(pair.Item2, pair.Item1.Graph));
                if (!skin.HasBinding)
                {
                    checks.Add(new JObject { ["name"] = "bones", ["status"] = "unknown", ["reason"] = "No evaluated skin binding is present in the current authoring profile." });
                    checks.Add(new JObject { ["name"] = "maxInfluences", ["status"] = "unknown", ["reason"] = "No evaluated skin binding is present in the current authoring profile." });
                }
                else
                {
                    AddBound(checks, "bones", skin.Bones, NyaForge.Authoring.Rig.SkeletonDefinition.MaxBones, "Skeleton bones (authoring capacity)");
                    AddBound(checks, "maxInfluences", skin.MaxInfluences, NyaForge.Authoring.Rig.SkinBinding.MaxInfluencesPerVertex, "Maximum vertex influences (authoring capacity)");
                }
                checks.Add(new JObject { ["name"] = "fit", ["status"] = "unknown", ["reason"] = "Avatar fit and pose deformation are not part of this static profile." });
                string status = checks.OfType<JObject>().Any(c => (string)c["status"] == "fail") ? "fail" : "pass";
                return Result(workspace, request, limits, status, checks, triangles, vertices, materials, textures, maxTexture, skin, new JArray(), objectMetrics);
            }
        }

        sealed class SkinSummary
        {
            public bool HasBinding;
            public int Bones;
            public int MaxInfluences;
            readonly HashSet<string> skeletons = new HashSet<string>(StringComparer.Ordinal);

            public void AddSkeleton(SkeletonDefinition skeleton)
            {
                if (skeleton != null && skeletons.Add(skeleton.ContentHash)) Bones += skeleton.Bones.Count;
            }

            public void Merge(SkinSummary other)
            {
                if (other == null) return;
                HasBinding |= other.HasBinding;
                MaxInfluences = Math.Max(MaxInfluences, other.MaxInfluences);
                foreach (var hash in other.skeletons)
                    if (skeletons.Add(hash)) Bones += other.skeletonCounts[hash];
            }

            internal readonly Dictionary<string, int> skeletonCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        static SkinSummary SummarizeSkin(GraphEvaluation evaluation, AuthoringGraph graph)
        {
            var result = new SkinSummary();
            if (evaluation == null || evaluation.SkinBindingOutputs == null || evaluation.SkinBindingOutputs.Count == 0) return result;
            var reachable = ReachableNodes(graph);
            var bindings = evaluation.SkinBindingOutputs.Where(pair => reachable.Contains(pair.Key)).Select(pair => pair.Value).ToArray();
            if (bindings.Length == 0) return result;
            result.HasBinding = true;
            result.MaxInfluences = bindings
                .SelectMany(value => value.Binding.Weights.Values)
                .Select(weights => weights == null ? 0 : weights.Count)
                .DefaultIfEmpty(0).Max();
            if (evaluation.SkeletonOutputs != null)
                foreach (var pair in evaluation.SkeletonOutputs)
                    if (reachable.Contains(pair.Key) && pair.Value?.Skeleton != null)
                    {
                        result.AddSkeleton(pair.Value.Skeleton);
                        result.skeletonCounts[pair.Value.Skeleton.ContentHash] = pair.Value.Skeleton.Bones.Count;
                    }
            return result;
        }

        static HashSet<string> ReachableNodes(AuthoringGraph graph)
        {
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            if (graph == null || string.IsNullOrEmpty(graph.OutputNodeId) || !graph.Nodes.ContainsKey(graph.OutputNodeId)) return reachable;
            var incoming = graph.Edges.GroupBy(edge => edge.ToNode).ToDictionary(group => group.Key, group => group.Select(edge => edge.FromNode).ToArray(), StringComparer.Ordinal);
            var pending = new Stack<string>(); pending.Push(graph.OutputNodeId);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!reachable.Add(node)) continue;
                if (!incoming.TryGetValue(node, out var upstream)) continue;
                foreach (var id in upstream) if (graph.Nodes.ContainsKey(id)) pending.Push(id);
            }
            return reachable;
        }

        static IEnumerable<PaintImage> Images(GraphMeshValue output)
        {
            // A material assignment deliberately keeps the resolved image on both
            // the output's legacy BaseColor slot and Material.BaseColor. Count the
            // owned image resource once so validation reflects actual texture usage,
            // rather than the number of graph references to that resource.
            var seen = new HashSet<string>(StringComparer.Ordinal);
            IEnumerable<PaintImage> Unique(IEnumerable<PaintImage> candidates)
            {
                foreach (var image in candidates)
                    if (image != null && seen.Add(Checks.Hash(PaintImageCodec.Write(image)))) yield return image;
            }
            foreach (var image in Unique(new[] { output.BaseColor?.Image, output.Material?.BaseColor?.Image })) yield return image;
            if (output.SlotMaterials != null)
                foreach (var slot in output.SlotMaterials.Values)
                    foreach (var image in Unique(new[] { slot.Material.BaseColor?.Image })) yield return image;
        }

        static void AddBound(JArray checks, string name, int actual, int limit, string label)
        {
            checks.Add(new JObject { ["name"] = name, ["status"] = actual <= limit ? "pass" : "fail", ["actual"] = actual, ["limit"] = limit, ["message"] = label + ": " + actual + " / " + limit });
        }

        static JObject Result(AuthoringWorkspace workspace, AuthoringValidationRequest request, Limits limits, string status, JArray checks,
            int? triangles, int? vertices, int? materials, int? textures, int? maxTexture, SkinSummary skin, JArray warnings, JArray objectMetrics)
        {
            var metrics = new JObject();
            metrics["objects"] = workspace.Document.Objects.Count;
            if (triangles.HasValue) metrics["triangles"] = triangles.Value;
            if (vertices.HasValue) metrics["renderVertices"] = vertices.Value;
            if (materials.HasValue) metrics["materials"] = materials.Value;
            if (textures.HasValue) metrics["textures"] = textures.Value;
            if (maxTexture.HasValue) metrics["maxTextureDimension"] = maxTexture.Value;
            if (skin != null && skin.HasBinding)
            {
                metrics["bones"] = skin.Bones;
                metrics["maxInfluences"] = skin.MaxInfluences;
            }
            return new JObject
            {
                ["success"] = true,
                ["status"] = status,
                ["profile"] = limits.Name,
                ["instanceId"] = workspace.InstanceId,
                ["documentId"] = workspace.Document.DocumentId,
                ["revision"] = workspace.Document.DocumentRevision,
                ["stateHash"] = workspace.Document.StateHash,
                ["attachmentsHash"] = workspace.Attachments.ContentHash,
                ["metrics"] = metrics,
                ["objects"] = objectMetrics ?? new JArray(),
                ["checks"] = checks,
                ["warnings"] = warnings
            };
        }
    }
}
