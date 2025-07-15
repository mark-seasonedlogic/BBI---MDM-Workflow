using System;
using System.Collections.Generic;

namespace BBIHardwareSupport.MDM.Intune.Models.Packaging
{
    /// <summary>
    /// Represents the metadata and artifact manifest for a versioned Intune package configuration.
    /// </summary>
    public class IntunePackageMetadata
    {
        /// <summary>
        /// The restaurant concept this package is designed for (e.g., OBS, BFG).
        /// </summary>
        public string Concept { get; set; }

        /// <summary>
        /// The type of device this package applies to (e.g., AndroidTablet, iPad).
        /// </summary>
        public string DeviceType { get; set; }

        /// <summary>
        /// The functional role of the device (e.g., POS, CIM, KDS).
        /// </summary>
        public string DeviceFunction { get; set; }

        /// <summary>
        /// The version number or tag of this configuration package.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// A human-readable description of the package purpose or changes.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The username or identity of the person who created this package.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// The UTC date and time the package was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// A manifest of all artifact file names organized by artifact type.
        /// </summary>
        public ArtifactManifest Artifacts { get; set; } = new ArtifactManifest();
    }

    /// <summary>
    /// Represents the file-level artifact manifest for an Intune package.
    /// </summary>
    public class ArtifactManifest
    {
        public List<string> DeviceConfigurationProfiles { get; set; } = new();
        public List<string> CompliancePolicies { get; set; } = new();
        public List<string> Apps { get; set; } = new();
        public List<string> AppConfigurations { get; set; } = new();
        public List<string> OEMConfigProfiles { get; set; } = new();
        public List<string> EnrollmentConfigurations { get; set; } = new();
    }
}
