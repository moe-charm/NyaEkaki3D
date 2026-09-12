using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
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
                var evaluation = workspace.Preview?.Evaluation;
                var output = workspace.Preview?.Output;
                if (evaluation == null || !workspace.Preview.IsComplete || output == null || output.Mesh == null)
                {
                    return Result(workspace, request, limits, "unknown", checks, null, null, null, null, null, null,
                        new JArray("Final output is incomplete or has no renderable mesh."));
                }

                int triangles = output.Mesh.TriangleCount;
                int materials = output.SlotMaterials?.Count ?? (output.Material == null ? 0 : 1);
                int textures = 0, maxTexture = 0;
                foreach (var image in Images(output))
                {
                    textures++;
                    maxTexture = Math.Max(maxTexture, Math.Max(image.Width, image.Height));
                }
                AddBound(checks, "triangles", triangles, limits.Triangles, "Triangles");
                AddBound(checks, "materials", materials, limits.Materials, "Materials");
                AddBound(checks, "maxTextureDimension", maxTexture, limits.TextureDimension, "Largest texture dimension");
                var skin = SummarizeSkin(evaluation);
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
                return Result(workspace, request, limits, status, checks, triangles, output.Mesh.VertexCount, materials, textures, maxTexture, skin, new JArray());
            }
        }

        sealed class SkinSummary
        {
            public bool HasBinding;
            public int Bones;
            public int MaxInfluences;
        }

        static SkinSummary SummarizeSkin(GraphEvaluation evaluation)
        {
            var result = new SkinSummary();
            if (evaluation == null || evaluation.SkinBindingOutputs == null || evaluation.SkinBindingOutputs.Count == 0) return result;
            result.HasBinding = true;
            result.MaxInfluences = evaluation.SkinBindingOutputs.Values
                .SelectMany(value => value.Binding.Weights.Values)
                .Select(weights => weights == null ? 0 : weights.Count)
                .DefaultIfEmpty(0).Max();
            result.Bones = evaluation.SkeletonOutputs == null
                ? 0
                : evaluation.SkeletonOutputs.Values.Select(value => value.Skeleton.Bones.Count).DefaultIfEmpty(0).Max();
            return result;
        }

        static IEnumerable<PaintImage> Images(GraphMeshValue output)
        {
            if (output.BaseColor?.Image != null) yield return output.BaseColor.Image;
            if (output.Material?.BaseColor?.Image != null) yield return output.Material.BaseColor.Image;
            if (output.SlotMaterials != null)
                foreach (var slot in output.SlotMaterials.Values)
                    if (slot.Material.BaseColor?.Image != null) yield return slot.Material.BaseColor.Image;
        }

        static void AddBound(JArray checks, string name, int actual, int limit, string label)
        {
            checks.Add(new JObject { ["name"] = name, ["status"] = actual <= limit ? "pass" : "fail", ["actual"] = actual, ["limit"] = limit, ["message"] = label + ": " + actual + " / " + limit });
        }

        static JObject Result(AuthoringWorkspace workspace, AuthoringValidationRequest request, Limits limits, string status, JArray checks,
            int? triangles, int? vertices, int? materials, int? textures, int? maxTexture, SkinSummary skin, JArray warnings)
        {
            var metrics = new JObject();
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
                ["metrics"] = metrics,
                ["checks"] = checks,
                ["warnings"] = warnings
            };
        }
    }
}
