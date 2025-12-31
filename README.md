# BBI -- MDM Workflow (Core Library + WinUI 8.0)

## Enterprise-Scale Device Lifecycle Automation

Windows Desktop / WinUI 3 / C# / XAML / SOLID / MVVM / Dependency
Injection / xUnit / Moq

### Problem Space

Bloomin' Brands operates \~14,000+ managed restaurant tablets and iPads
used for POS, staging, telemetry, and compliance workflows.\
This toolset provides a modular, testable, Git-backed automation
framework to:

-   Inventory MDM artifacts (Workspace ONE + Microsoft Intune)
-   Manage device tags and SmartGroup hierarchies
-   Create, move, and validate device profiles
-   Analyze battery drain trends using telemetry heuristics
-   Correlate Wi‑Fi roaming + signal strength with POS performance
-   Provide a version‑controlled source‑of‑truth baseline

### Solution Architecture

Solution\
├── BBI -- MDM Workflow (WinUI 3 UI)\
│ ├── Views (NavigationView pages, login dialogs, dashboards)\
│ ├── ViewModels (MVVM + async command orchestration)\
│ └── Installer (WiX bundle -- not required for prototype/tests)\
│\
├── BBIHardwareSupport.MDM.Core (Core Library)\
│ ├── Models (Device, SmartGroup, Profile Create Request, etc.)\
│ ├── Services (API transport + business logic)\
│ └── Authentication abstractions (Graph + WS1 auth)\
│\
└── tests/BBIHardwareSupport.MDM.Tests (Unit Test Project)\
├── Transport contract validation (mocked HTTP via Moq.Protected)\
└── CreateProfile service tests (coverage WIP)

### Design Principles

-   SOLID architecture (interface‑based service boundaries)
-   MVVM (WinUI 3 NavigationView)
-   Async‑first HTTP transport
-   Dependency Injection for API clients and logging
-   Unit‑testable service layers for team handoff

### Restore & Build

``` powershell
dotnet restore
dotnet build
```

### Run Tests

``` powershell
dotnet test ./tests/BBIHardwareSupport.MDM.Tests/BBIHardwareSupport.MDM.Tests.csproj -v minimal
```

### Prototype Handoff Goals

This repo is structured to allow:

-   Transport contract validation (endpoint path, headers, JSON payload)
-   Mocked HTTP calls (Moq.Protected) instead of hitting real endpoints
-   Throw behavior verification on non‑success HTTP status
-   JSON serialization correctness for WS1 Version=2 media type
    contracts
-   Team ownership of full test coverage while prototype features
    continue to expand

### About the Author

Mark Young\
Founder, Seasoned Logic LLC\
Senior Technical Lead, Bloomin' Brands (2014--Present)\
Enterprise tooling engineer specializing in MDM automation, API
integrations, WinUI desktop apps, and telemetry correlation at scale.
