# 📘 Intune iPad Management Project Documentation

## 🧾 Project Overview
This project establishes a scalable and secure process for managing corporate-owned iPads at Bloomin' Brands using:
- **Apple Business Manager (ABM)** for device provisioning
- **Microsoft Intune** for MDM
- **Microsoft Entra ID (Azure AD)** for dynamic group management
- **Microsoft Graph API** for automation and scripting

The goal is to automate enrollment, organize devices dynamically, and deliver managed apps and configurations without user interaction.

---

## 🧱 Technical Components

| Component | Role |
|----------|------|
| **Apple Business Manager (ABM)** | Device assignment to Intune MDM server |
| **Intune** | Enrollment, compliance policies, app deployment |
| **Microsoft Graph API** | Automation of group creation, app config deployment, and device auditing |
| **Microsoft Entra ID** | Hosts dynamic groups used by Intune for targeting policies and apps |

---

## 🔐 Required Microsoft Graph API Permissions

| Permission Scope | Purpose | Admin Consent |
|------------------|---------|----------------|
| `Group.ReadWrite.All` | Create, modify, and delete Entra ID groups | ✅ |
| `DeviceManagementApps.ReadWrite.All` | Create managed app configuration for iOS | ✅ |
| `DeviceManagementManagedDevices.Read.All` | Query enrolled device data | ✅ |

---

## 🔄 Device Lifecycle Workflow

```plaintext
[Apple Business Manager] 
    ⇓
[Intune Enrollment (ADE)] 
    ⇓
[Dynamic Group Assignment via Entra ID] 
    ⇓
[Configuration Profiles + Apps Assigned via Intune] 
    ⇓
[Managed App Configurations Applied]
```

---

## 📦 Managed App Configuration Strategy
- Apps are added via **Apple Business Manager** and synced into Intune via **VPP token**
- Apps must be assigned with **Device Licensing** for silent install
- Managed App Config for iOS apps is entered in **XML plist format** within the Intune UI

Example snippet:
```xml
<dict>
  <key>storeId</key>
  <string>1034</string>
  <key>env</key>
  <string>production</string>
</dict>
```

---

## 🧠 Dynamic Group Rules and Naming Strategy

### Hierarchical Dynamic Group Structure
Dynamic groups will be created based on a hierarchy starting at the Organization level:
- **Organization**: Bloomin' Brands, Inc.
  - **Concept**: OBS = Outback, CIG = Carrabba's, BFG = Bonefish Grill, FPS = Fleming's Prime Steakhouse

### Device Naming Convention
Prior to enrollment, devices will be named using the following structure:

```plaintext
{Concept-Abbreviation}{Restaurant-Number}{Device-Function}
```

Example:
```plaintext
OBS1234CIM
```
Meaning:
- **OBS** = Outback Steakhouse
- **1234** = Restaurant number
- **CIM** = Customer Interaction Manager device

### Supported Device Function Types

| Function Type | Example Naming Convention |
|---------------|-----------------------------|
| POS Server (P1) | OBS1234P1 |
| Network Device (N1) | OBS1234N1 |
| Kitchen Device (K1) | OBS1234K1 |
| Queue Management Device (Q1) | OBS1234Q1 |
| Kitchen Display System (KDS, ST1 - ST10) | OBS1234ST1, OBS1234ST2, ..., OBS1234ST10 |
| Back of House (BoH) | OBS1234BoH |
| Laptop | OBS1234LTT |
| POS Mobile Device | OBS1234POS |
| CIM Mobile Device | OBS1234CIM |

Dynamic device group rules will use this naming structure to automatically place devices into the correct concept, restaurant, and functional hierarchy.

### Example Dynamic Group Rule
```plaintext
(device.deviceName -startsWith "OBS1234")
```
Assigns all Outback devices from restaurant 1234.

---

## 🧠 Known Issues and Considerations

| Issue | Workaround |
|-------|------------|
| iPads prompt for Apple ID during app install | Ensure apps are VPP-distributed and use **Device Licensing** |
| MAC randomization causes Wi-Fi issues | Confirm system is not silently toggling MAC type due to failures |
| Notes field not searchable in Intune UI | Use Graph API or export CSV for filtering notes |

---

## 📌 Future Enhancements
- Automate app config updates using Graph API and integrated UI flows
- Build out management tooling and auditing capabilities directly within the WinUI 3 application
- Expand device configuration and assignment visualization in the WinUI interface

---

> **Maintainer:** Mark Young  
> **Last Updated:** April 25, 2025

