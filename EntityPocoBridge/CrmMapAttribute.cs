// File:    CrmMapAttribute.cs
// Author:  Abdul Rafay Ali Khan
// Date:    2025-04-20
// Purpose: Defines the CrmMapAttribute used for mapping
//          Dataverse/CRM Entity fields to custom POCO properties via the XrmMapper utility.
// Notes:   Requires reference to Microsoft.Xrm.Sdk.dll.

namespace EntityPocoBridge
{
    /// <summary>
    /// Maps a POCO property to a CRM Entity attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class CrmMapAttribute : Attribute
    {
        /// <summary>
        /// The logical name of the CRM attribute. This can include an entity alias prefix
        /// for linked entity values (e.g., "contact_alias.emailaddress1").
        /// </summary>
        public string LogicalName { get; }

        /// <summary>
        /// Specifies which part of the CRM attribute value to extract, especially for complex types.
        /// </summary>
        public CrmMapSourcePart SourcePart { get; }

        /// <summary>
        /// Optional: Specifies the exact format string (e.g., "yyyy-MM-dd HH:mm")
        /// to use ONLY when converting a DateTime CRM value to a string POCO property.
        /// Ignored otherwise.
        /// </summary>
        public string DateTimeFormat { get; set; } // Make it settable property

        /// <summary>
        /// Optional: Specifies the target entity logical name when mapping a Guid POCO property
        /// to a CRM EntityReference (lookup) field (writing only). Required for writing lookups via Guid.
        /// </summary>
        public string TargetEntityLogicalName { get; set; }

        /// <summary>
        /// Maps a POCO property using default source part extraction based on target type.
        /// </summary>
        /// <param name="logicalName">The logical name of the CRM attribute.</param>
        public CrmMapAttribute(string logicalName)
        {
            LogicalName = logicalName ?? throw new ArgumentNullException(nameof(logicalName));
            SourcePart = CrmMapSourcePart.Default;
        }

        /// <summary>
        /// Maps a POCO property, explicitly specifying which part of the CRM value to extract.
        /// </summary>
        /// <param name="logicalName">The logical name of the CRM attribute.</param>
        /// <param name="sourcePart">The specific part of the CRM value to extract.</param>
        public CrmMapAttribute(string logicalName, CrmMapSourcePart sourcePart)
        {
            LogicalName = logicalName ?? throw new ArgumentNullException(nameof(logicalName));
            SourcePart = sourcePart;
        }
    }
}
