# XrmMapper: Dynamics CRM/Dataverse Entity to POCO Mapper

## Overview

This utility provides a convenient way to map `Microsoft.Xrm.Sdk.Entity` and `Microsoft.Xrm.Sdk.EntityCollection` objects retrieved from Dynamics CRM / Dataverse (via the XRM SDK) to your custom Plain Old C# Objects (POCOs).

It aims to simplify the data access layer by replacing cumbersome late-bound attribute access (e.g., `(string)entity["name"]`, `((OptionSetValue)entity["statuscode"]).Value`) with a clean, strongly-typed POCO representation, driven by declarative attributes on your POCO properties. The mapping process is analogous to deserializing JSON into C# objects.

## Features

* **Attribute-Based Mapping:** Define mapping rules declaratively using the `[CrmMap]` attribute on your POCO properties.
* **Handles Common CRM Types:** Supports mapping for `EntityReference` (Lookups), `OptionSetValue` (Option Sets, Statuses), `Money`, `DateTime`, Booleans, Numbers, Strings, etc.
* **Flexible Extraction:** Extract specific parts of complex types (e.g., `Id` or `Name` from `EntityReference`, `Value` or `FormattedValue` from `OptionSetValue`).
* **Aliased Value Support:** Maps data retrieved from linked entities using the `alias.attribute` convention.
* **Custom DateTime Formatting:** Specify exact string formats when mapping CRM `DateTime` values to POCO `string` properties.
* **Performance Optimized:** Uses a thread-safe cache (`ConcurrentDictionary`) to store reflection results, minimizing performance impact after the first mapping of a specific POCO type.
* **Simplified Code:** Leads to cleaner, more readable, and more testable application code by working with POCOs instead of `Entity` objects directly.

## Requirements

* .NET Framework or .NET Core/5+ (Compatible with your `Microsoft.Xrm.Sdk` version)
* Reference to `Microsoft.Xrm.Sdk.dll`

## Setup

1.  **Clone the project:** Clone or Download the following project into your machine. You can place them in separate files (recommended) or combine them into one, adjusting namespaces as needed.
    * `CrmMapAttribute.cs` (Contains `CrmMapAttribute` class and `CrmMapSourcePart` enum)
    * `XrmMapper.cs` (Contains the static `XrmMapper` class and its logic)
2.  **Adjust Namespace:** Ensure the namespace used in the files (e.g., `XrmMapping`) matches your project structure or update the `using` statements where you call the mapper.
