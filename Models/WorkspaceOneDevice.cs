using System;
using System.Collections.Generic;

namespace BBIHardwareSupport.MDM.WorkspaceOne.Models
{
    public class WorkspaceOneDevice
    {
        public int DeviceId { get; set; }
        public string? DeviceUuid { get; set; }
        public string? Udid { get; set; }
        public string? SerialNumber { get; set; }
        public string? DeviceFriendlyName { get; set; }
        public int OrganizationGroupId { get; set; }
        public string? OrganizationGroupUuid { get; set; }
        public string? UserName { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public bool Compliant { get; set; }
        public string? AssetNumber { get; set; }
        public string? EnrollmentStatus { get; set; }
        public DateTime? UnEnrolledDate { get; set; }

        public List<DeviceNetworkInfo>? DeviceNetworkInfo { get; set; }
        public List<SmartGroup>? SmartGroups { get; set; }
        public List<Product>? Products { get; set; }
        public List<WorkspaceOneCustomAttribute>? CustomAttributes { get; set; }
    }

    public class DeviceNetworkInfo
    {
        public string? ConnectionType { get; set; }
        public string? MACAddress { get; set; }
        public string? IPAddress { get; set; }  // only appears for Android
    }

    public class SmartGroup
    {
        public int SmartGroupId { get; set; }
        public string? SmartGroupUuid { get; set; }
        public string? Name { get; set; }
    }

    public class Product
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
    }

    public class WorkspaceOneCustomAttribute
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? ApplicationGroup { get; set; }
    }
}
