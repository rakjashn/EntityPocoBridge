// File:    XrmMapper.cs
// Author:  Abdul Rafay Ali Khan
// Date:    2025-04-25
// Purpose: Static utility class and supporting types to map Microsoft.Xrm.Sdk.Entity/EntityCollection objects
//          to/from custom POCOs using reflection, caching, and CrmMapAttribute decorations.
// Notes:   Requires reference to Microsoft.Xrm.Sdk.dll.
//          Replace Console.WriteLine warnings with proper logging.
//          Write logic does not handle OptionSetValueCollection (Multi-Select) OOTB.


using Microsoft.Xrm.Sdk;
using System.Collections.Concurrent;
using System.Reflection;

namespace EntityPocoBridge
{
    public static partial class XrmMapper
    {
        #region Writing / Serialization Logic (From POCO -> Entity)

        // Cache for write mapping instructions (Type -> List<WriteMappingInfo>)
        private static readonly ConcurrentDictionary<Type, List<WriteMappingInfo>> _writeMappingCache =
            new ConcurrentDictionary<Type, List<WriteMappingInfo>>();

        // Internal class to hold cached reflection results for writing
        private class WriteMappingInfo
        {
            public PropertyInfo SourceProperty { get; set; } // POCO Property
            public CrmMapAttribute MapAttribute { get; set; }
        }

        /// <summary>
        /// Maps a POCO object to a new or existing CRM Entity object, ready for Create or Update.
        /// Only maps non-null properties from the POCO by default.
        /// </summary>
        /// <typeparam name="T">The type of the source POCO object.</typeparam>
        /// <param name="pocoObject">The POCO object containing data to map.</param>
        /// <param name="targetEntityLogicalName">The logical name of the target CRM entity (e.g., "account", "contact").</param>
        /// <param name="recordId">Optional: The Guid of the record to update. If null, an Entity for creation is returned.</param>
        /// <param name="mapNullProperties">Optional: If true, explicitly map null POCO properties to null in the Entity (useful for clearing fields). Defaults to false.</param>
        /// <returns>A populated Entity object, or null if the input POCO is null.</returns>
        /// <exception cref="ArgumentNullException">Thrown if pocoObject or targetEntityLogicalName is null or empty.</exception>
        public static Entity MapPocoToEntity<T>(
            T pocoObject,
            string targetEntityLogicalName,
            Guid? recordId = null,
            bool mapNullProperties = false) where T : class // Ensure T is a reference type
        {
            if (pocoObject == null)
            {
                // Or return null? Throwing might be better as it indicates invalid input.
                throw new ArgumentNullException(nameof(pocoObject));
            }
            if (string.IsNullOrWhiteSpace(targetEntityLogicalName))
            {
                throw new ArgumentNullException(nameof(targetEntityLogicalName));
            }

            // Get mapping instructions (from cache or reflection)
            var mappingInstructions = GetWriteMappingInstructions(typeof(T));

            // Create the target Entity object
            Entity targetEntity;
            if (recordId.HasValue && recordId.Value != Guid.Empty)
            {
                // Prepare for Update
                targetEntity = new Entity(targetEntityLogicalName, recordId.Value);
            }
            else
            {
                // Prepare for Create
                targetEntity = new Entity(targetEntityLogicalName);
            }

            // Iterate through the POCO properties that have the CrmMap attribute
            foreach (var instruction in mappingInstructions)
            {
                try
                {
                    // Get the value from the POCO property
                    object pocoValue = instruction.SourceProperty.GetValue(pocoObject);

                    // Skip null properties unless explicitly told to map them
                    if (pocoValue == null && !mapNullProperties)
                    {
                        continue;
                    }

                    // Convert the POCO value to the appropriate CRM SDK type
                    object crmValue = ConvertPocoValueToCrm(pocoValue, instruction.MapAttribute, instruction.SourceProperty.PropertyType);

                    // Add the attribute to the Entity's collection
                    // Using Add allows overwriting if the key exists (shouldn't happen with unique instructions)
                    // Using targetEntity[logicalName] = value; is also common and safe.
                    targetEntity.Attributes.Add(instruction.MapAttribute.LogicalName, crmValue);

                }
                catch (Exception ex)
                {
                    // TODO: Replace Console.WriteLine with proper logging
                    Console.WriteLine($"Error mapping POCO property '{instruction.SourceProperty.Name}' to CRM Field '{instruction.MapAttribute.LogicalName}': {ex.Message}");
                    // Depending on requirements, you might throw, continue (skip property), or collect errors.
                    // Consider adding more context like targetEntityLogicalName and recordId if available.
                }
            }

            return targetEntity;
        }

        // Gets write mapping instructions from cache or builds them using reflection
        private static List<WriteMappingInfo> GetWriteMappingInstructions(Type sourceType)
        {
            // Use separate cache for writing instructions
            return _writeMappingCache.GetOrAdd(sourceType, type =>
            {
                // Get public instance properties that have a getter
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                     .Where(p => p.CanRead); // POCO properties need a getter
                var instructions = new List<WriteMappingInfo>();
                foreach (var prop in properties)
                {
                    var mapAttribute = prop.GetCustomAttribute<CrmMapAttribute>();
                    if (mapAttribute != null)
                    {
                        // Basic validation for write mapping
                        if (string.IsNullOrWhiteSpace(mapAttribute.LogicalName))
                        {
                            Console.WriteLine($"Warning: Skipping property '{prop.Name}' for write mapping due to missing LogicalName in CrmMapAttribute.");
                            continue;
                        }
                        // Add more validation? e.g., check TargetEntityLogicalName if type is Guid?

                        instructions.Add(new WriteMappingInfo
                        {
                            SourceProperty = prop, // POCO Property
                            MapAttribute = mapAttribute
                        });
                    }
                }
                return instructions;
            });
        }

        /// <summary>
        /// Converts a POCO property value to its corresponding CRM SDK attribute type.
        /// </summary>
        private static object ConvertPocoValueToCrm(object pocoValue, CrmMapAttribute mapAttribute, Type sourcePocoType)
        {
            // Handle explicit null from POCO
            if (pocoValue == null)
            {
                return null;
            }

            // --- Handle Guid -> EntityReference ---
            // Requires TargetEntityLogicalName to be set on the attribute
            if ((sourcePocoType == typeof(Guid) || sourcePocoType == typeof(Guid?)) && !string.IsNullOrWhiteSpace(mapAttribute.TargetEntityLogicalName))
            {
                // Ensure the Guid is not empty if it's required (CRM often treats empty Guid as null lookup)
                if (pocoValue is Guid guidValue && guidValue != Guid.Empty)
                {
                    return new EntityReference(mapAttribute.TargetEntityLogicalName, guidValue);
                }
                else
                {
                    // Map empty Guid or null Guid? to null for the lookup field
                    return null;
                }
            }

            // --- Handle int/Enum -> OptionSetValue ---
            // Use SourcePart hint or infer if target type is OptionSetValue (less direct)
            // Simple approach: Assume int maps to OptionSetValue if SourcePart is Value or Default?
            // More robust: Could add a TargetCrmType hint to the attribute if needed.
            // Let's assume int maps to OptionSetValue if SourcePart is Value.
            Type underlyingPocoType = Nullable.GetUnderlyingType(sourcePocoType) ?? sourcePocoType;
            if ((underlyingPocoType == typeof(int) || underlyingPocoType.IsEnum) // Check underlying type
                && mapAttribute.SourcePart == CrmMapSourcePart.Value)
            {
                // pocoValue will be the actual enum value (e.g., TestWriteStatus.Active) or an int
                if (pocoValue is int intValue)
                {
                    return new OptionSetValue(intValue);
                }
                else if (pocoValue is Enum enumValue)
                { // Check if the actual value is an Enum
                    return new OptionSetValue(Convert.ToInt32(enumValue));
                }
                else
                {
                    // This case handles if pocoValue was null (for int? or Enum?)
                    return null;
                }
            }

            // --- Handle decimal -> Money ---
            // Assume decimal maps to Money if SourcePart is Value or Default?
            if ((sourcePocoType == typeof(decimal) || sourcePocoType == typeof(decimal?)) && (mapAttribute.SourcePart == CrmMapSourcePart.Value || mapAttribute.SourcePart == CrmMapSourcePart.Default))
            {
                if (pocoValue is decimal decValue)
                {
                    return new Money(decValue);
                }
                else
                {
                    // Null decimal? maps to null
                    return null;
                }
            }

            // --- Handle bool -> bool (Two Option Set) ---
            // CRM SDK uses bool directly for Two Option sets
            if (sourcePocoType == typeof(bool) || sourcePocoType == typeof(bool?))
            {
                if (pocoValue is bool boolValue)
                {
                    return boolValue;
                }
                else
                {
                    // Null bool? maps to null
                    return null;
                }
            }

            // --- Handle other basic types (string, DateTime, Guid (non-lookup), int (non-optionset), decimal (non-money), double, byte[]) ---
            // These types often map directly or with simple conversion handled by the SDK implicitly
            // or don't require special SDK types. Return the value as-is.
            // Guid used as a primary key doesn't need EntityReference conversion.
            // We can let the SDK handle direct assignment for these.
            if (pocoValue is string || pocoValue is DateTime || pocoValue is Guid || pocoValue is double || pocoValue is byte[] || pocoValue is int || pocoValue is decimal)
            {
                return pocoValue;
            }

            // --- Fallback / Unhandled Type ---
            // If we reach here, the POCO type wasn't explicitly handled above.
            // We could try a direct assignment, but it might fail in the SDK call.
            // Logging a warning is safer.
            Console.WriteLine($"Warning: Unhandled POCO type '{sourcePocoType.Name}' for CRM attribute '{mapAttribute.LogicalName}'. Returning raw value. SDK assignment might fail.");
            return pocoValue;
        }

        #endregion
    }
}
