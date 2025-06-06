﻿# XrmMapper: Dynamics CRM/Dataverse Entity to POCO Mapper

## Overview

This utility provides a convenient way to map `Microsoft.Xrm.Sdk.Entity` and `Microsoft.Xrm.Sdk.EntityCollection` objects retrieved from Dynamics CRM / Dataverse (via the XRM SDK) to your custom Plain Old C# Objects (POCOs).

It aims to simplify the data access layer by replacing cumbersome late-bound attribute access (e.g., `(string)entity["name"]`, `((OptionSetValue)entity["statuscode"]).Value`) with a clean, strongly-typed POCO representation, driven by declarative attributes on your POCO properties. The mapping process is analogous to deserializing JSON into C# objects.

## Features

* **Attribute-Based Mapping:** Define mapping rules declaratively using the `[CrmMap]` attribute on your POCO properties for both reading and writing.
* **Read Functionality (`Entity` -> POCO):**
    * Handles Common CRM Types: `EntityReference`, `OptionSetValue`, `Money`, `DateTime`, Booleans, Numbers, Strings, `BooleanManagedProperty`, etc.
    * Flexible Extraction: Extract specific parts (`Id`, `Name`, `Value`, `FormattedValue`).
    * Aliased Value Support: Maps data retrieved from linked entities.
    * Custom DateTime Formatting: Specify string formats when mapping `DateTime` to `string`.
* **Write Functionality (POCO -> `Entity`):**
    * Map POCOs to `Entity` objects ready for `IOrganizationService.Create()` or `Update()`.
    * Handles conversion of .NET types to SDK types (`Guid` -> `EntityReference`, `int`/`Enum` -> `OptionSetValue`, `decimal` -> `Money`, `bool` -> `bool`).
    * Requires `TargetEntityLogicalName` attribute property for mapping `Guid` to lookups.
    * Optionally map `null` POCO properties to clear fields during updates.
* **Performance Optimized:** Uses thread-safe caches (`ConcurrentDictionary`) for reflection results, minimizing performance impact after the first mapping of a specific POCO type (separate caches for read and write).
* **Simplified Code:** Leads to cleaner, more readable, and more testable application code by working with POCOs instead of `Entity` objects directly.

## Requirements

* .NET Framework or .NET Core/5+ (Compatible with your `Microsoft.Xrm.Sdk` version)
* Reference to `Microsoft.Xrm.Sdk.dll`

## Setup

1.  **Clone the project:** Clone or Download the following project into your machine. You can place them in separate files (recommended) or combine them into one, adjusting namespaces as needed.
    * `CrmMapAttribute.cs` (Contains `CrmMapAttribute` class and `CrmMapSourcePart` enum)
    * `XrmMapper.cs` (Contains the static `XrmMapper` class and its logic)
2.  **Adjust Namespace:** Ensure the namespace used in the files (e.g., `XrmMapping`) matches your project structure or update the `using` statements where you call the mapper.

## Project Structure

The solution is organized as follows:

```plaintext
.
├── EntityPocoBridge/         # Main class library project (.NET Standard 2.0)
│   ├── CrmMapAttribute.cs    # Contains CrmMapAttribute and CrmMapSourcePart enum
│   ├── XrmMapper.cs          # Contains the static XrmMapper class and logic
│   └── EntityPocoBridge.csproj # Project file with package metadata
├── UnitTests/                # Unit test project (using NUnit)
│   ├── XrmMapperReadTests.cs # Contains unit tests for read logic (Entity->POCO)
│   ├── XrmMapperWriteTests.cs# Contains unit tests for write logic (POCO->Entity)
│   └── UnitTests.csproj      # Test project file
├── .gitignore                # Git ignore file for C#/.NET development
├── EntityPocoBridge.sln      # Visual Studio Solution file
├── LICENSE                   # MIT License file
└── README.md                 # This documentation file

```markdown
## Contributing & Feedback

This project is open-source under the MIT License, and your input is highly valued!

Whether you're using this mapper, reviewing the code, or just have ideas, feedback is crucial for improvement and helps guide future development towards a potential NuGet package release.

Please feel free to:

* **Report Bugs:** If you encounter any issues or unexpected behavior, please check the existing issues and open a new one if necessary: [[Link to Your GitHub Issues Tab]](https://github.com/rakjashn/EntityPocoBridge/issues)
* **Suggest Enhancements:** Have ideas for improvements or features (like handling `OptionSetValueCollection`, adding more configuration)? Please open an issue to discuss them.
* **Provide General Feedback:** Does this approach solve a problem for you? Is the usage clear? All feedback is welcome via GitHub Issues.
* **Contribute Code:** Pull Requests (PRs) addressing bugs or approved enhancements are welcome. Please consider opening an issue to discuss significant changes beforehand.

Let's work together to make working with Dataverse/CRM data in .NET even easier!
