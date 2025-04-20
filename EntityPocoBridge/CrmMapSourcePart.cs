// File:    CrmMapSourcePart.cs
// Author:  Abdul Rafay Ali Khan
// Date:    2025-04-20
// Purpose: Defines the CrmMapSourcePart enum used for mapping
//          Dataverse/CRM Entity fields to custom POCO properties via the XrmMapper utility.
// Notes:   Requires reference to Microsoft.Xrm.Sdk.dll.

namespace EntityPocoBridge
{
    /// <summary>
    /// Specifies which part of a CRM attribute value to extract. Simplified version.
    /// </summary>
    public enum CrmMapSourcePart
    {
        /// <summary>
        /// Let the mapper infer the most common part based on target property type
        /// (e.g., Guid -> Id, string -> Name, int -> Value, decimal -> Value).
        /// </summary>
        Default, // Kept for convenience
        /// <summary>
        /// Explicitly extract the Id property (Guid) from an EntityReference.
        /// </summary>
        Id,
        /// <summary>
        /// Explicitly extract the Name property (string) from an EntityReference.
        /// </summary>
        Name,
        /// <summary>
        /// Explicitly extract the underlying Value (e.g., int from OptionSetValue, decimal from Money).
        /// Also used for direct mapping of basic types.
        /// </summary>
        Value,
        /// <summary>
        /// Explicitly extract the user-friendly formatted string label from the FormattedValues collection.
        /// </summary>
        FormattedValue
    }
}
