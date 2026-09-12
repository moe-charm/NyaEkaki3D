using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NyaForge.Authoring.Simulation
{
    /// <summary>Separates a simulator's output domain so bone poses and mesh deformation cannot be mixed accidentally.</summary>
    public enum SecondaryMotionOutputKind
    {
        BonePose = 0,
        MeshDeformation = 1
    }

    /// <summary>Versioned adapter identity and the bounded capability set exposed to the UI/MCP layer.</summary>
    public sealed class SecondaryMotionCapabilities
    {
        public string AdapterId { get; }
        public string SimulatorId { get; }
        public string AdapterVersion { get; }
        public string PackageVersion { get; }
        public SecondaryMotionOutputKind OutputKind { get; }
        public bool SupportsReset { get; }
        public bool SupportsDeterministicStep { get; }
        public IReadOnlyList<string> Features { get; }

        public SecondaryMotionCapabilities(string adapterId, string simulatorId, string adapterVersion, string packageVersion,
            SecondaryMotionOutputKind outputKind, bool supportsReset, bool supportsDeterministicStep, IEnumerable<string> features)
        {
            Text(adapterId, "Adapter identity"); Text(simulatorId, "Simulator identity"); Text(adapterVersion, "Adapter version");
            OptionalText(packageVersion, "Package version"); ValidateKind(outputKind);
            Checks.Require(features != null, "INVALID_SIMULATION", "Adapter features are required.");
            var values = features.ToArray(); Checks.Require(values.Length <= 64, "BUDGET_EXCEEDED", "Adapter feature count exceeds capacity.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var feature in values) { Text(feature, "Adapter feature"); Checks.Require(seen.Add(feature), "INVALID_SIMULATION", "Adapter feature repeats."); }
            AdapterId = adapterId; SimulatorId = simulatorId; AdapterVersion = adapterVersion; PackageVersion = packageVersion ?? "";
            OutputKind = outputKind; SupportsReset = supportsReset; SupportsDeterministicStep = supportsDeterministicStep;
            Features = Array.AsReadOnly(values);
        }

        public bool Accepts(SecondaryMotionProfile profile)
        {
            return profile != null && AdapterId == profile.AdapterId && SimulatorId == profile.SimulatorId
                && AdapterVersion == profile.AdapterVersion && OutputKind == profile.OutputKind
                && (PackageVersion == "" || profile.PackageVersion == "" || PackageVersion == profile.PackageVersion);
        }

        internal static void ValidateKind(SecondaryMotionOutputKind kind)
        {
            Checks.Require(kind == SecondaryMotionOutputKind.BonePose || kind == SecondaryMotionOutputKind.MeshDeformation,
                "INVALID_SIMULATION", "Unknown secondary motion output kind.");
        }

        internal static void Text(string value, string label)
        {
            Checks.Require(!string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value.IndexOf('\0') < 0,
                "INVALID_SIMULATION", label + " must contain 1 to 128 characters.");
        }

        internal static void OptionalText(string value, string label)
        {
            Checks.Require(value != null && value.Length <= 128 && value.IndexOf('\0') < 0,
                "INVALID_SIMULATION", label + " is invalid.");
        }
    }

    /// <summary>Persisted target profile. Adapter payload is opaque, bounded, and kept separate from common topology.</summary>
    public sealed class SecondaryMotionProfile
    {
        public string AdapterId { get; }
        public string SimulatorId { get; }
        public int SchemaVersion { get; }
        public string AdapterVersion { get; }
        public string PackageVersion { get; }
        public SecondaryMotionOutputKind OutputKind { get; }
        public IReadOnlyList<byte> AdapterPayload { get; }

        public SecondaryMotionProfile(string adapterId, string simulatorId, int schemaVersion, string adapterVersion, string packageVersion,
            SecondaryMotionOutputKind outputKind, byte[] adapterPayload)
        {
            SecondaryMotionCapabilities.Text(adapterId, "Adapter identity"); SecondaryMotionCapabilities.Text(simulatorId, "Simulator identity");
            SecondaryMotionCapabilities.Text(adapterVersion, "Adapter version"); SecondaryMotionCapabilities.OptionalText(packageVersion, "Package version");
            Checks.Require(schemaVersion > 0 && schemaVersion <= 1024, "INVALID_SIMULATION", "Adapter schema version is invalid.");
            SecondaryMotionCapabilities.ValidateKind(outputKind);
            Checks.Require(adapterPayload != null && adapterPayload.Length <= AuthoringLimits.MaxBlobBytes,
                "BUDGET_EXCEEDED", "Adapter payload exceeds capacity.");
            AdapterId = adapterId; SimulatorId = simulatorId; SchemaVersion = schemaVersion; AdapterVersion = adapterVersion;
            PackageVersion = packageVersion ?? ""; OutputKind = outputKind; AdapterPayload = Array.AsReadOnly((byte[])adapterPayload.Clone());
        }

        public static SecondaryMotionProfile FromCapabilities(SecondaryMotionCapabilities capabilities, int schemaVersion, byte[] adapterPayload)
        {
            Checks.Require(capabilities != null, "INVALID_SIMULATION", "Adapter capabilities are required.");
            return new SecondaryMotionProfile(capabilities.AdapterId, capabilities.SimulatorId, schemaVersion,
                capabilities.AdapterVersion, capabilities.PackageVersion, capabilities.OutputKind, adapterPayload);
        }
    }
}
