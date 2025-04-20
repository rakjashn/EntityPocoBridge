// File:    XrmMapper.cs
// Author:  Abdul Rafay Ali Khan
// Date:    2025-04-20
// Purpose: Static utility class to map Microsoft.Xrm.Sdk.Entity and EntityCollection objects
//          to custom POCO lists using reflection, caching, and CrmMapAttribute decorations.
// Notes:   Requires reference to Microsoft.Xrm.Sdk.dll.
//          Replace Console.WriteLine warnings with proper logging.
//          Does not handle OptionSetValueCollection (Multi-Select) OOTB.


using Microsoft.Xrm.Sdk;
using System.Collections.Concurrent;
using System.Reflection;

namespace EntityPocoBridge
{
    public static class XrmMapper
    {
        // Cache for reflected property mapping info (Type -> List<MappingInstructions>)
        private static readonly ConcurrentDictionary<Type, List<MappingInfo>> _mappingCache =
            new ConcurrentDictionary<Type, List<MappingInfo>>();

        // Internal class to hold cached reflection results
        private class MappingInfo
        {
            public PropertyInfo TargetProperty { get; set; }
            public CrmMapAttribute MapAttribute { get; set; }
        }

        /// <summary>
        /// Maps an EntityCollection to a list of specified POCO objects.
        /// </summary>
        /// <typeparam name="T">The target POCO type. Must have a parameterless constructor.</typeparam>
        /// <param name="collection">The EntityCollection returned from CRM.</param>
        /// <returns>A list of populated POCO objects.</returns>
        public static List<T> MapCollectionToPocoList<T>(EntityCollection collection) where T : new()
        {
            if (collection == null || collection.Entities == null || !collection.Entities.Any())
            {
                return new List<T>();
            }

            // Get mapping instructions (from cache or reflection)
            var mappingInstructions = GetMappingInstructions(typeof(T));

            var results = new List<T>(collection.Entities.Count);
            foreach (var entity in collection.Entities)
            {
                var poco = MapSingleEntityToPoco<T>(entity, mappingInstructions);
                if (poco != null) // MapSingleEntityToPoco handles null entity case
                {
                    results.Add(poco);
                }
            }
            return results;
        }

        /// <summary>
        /// Maps a single Entity to a specified POCO object.
        /// </summary>
        /// <typeparam name="T">The target POCO type. Must have a parameterless constructor.</typeparam>
        /// <param name="entity">The Entity object from CRM.</param>
        /// <returns>A populated POCO object, or null if the input entity is null.</returns>
        public static T MapEntityToPoco<T>(Entity entity) where T : new()
        {
            if (entity == null)
            {
                return default; // Or throw, depending on desired behavior
            }
            var mappingInstructions = GetMappingInstructions(typeof(T));
            return MapSingleEntityToPoco<T>(entity, mappingInstructions);
        }


        // Gets mapping instructions from cache or builds them using reflection
        private static List<MappingInfo> GetMappingInstructions(Type targetType)
        {
            return _mappingCache.GetOrAdd(targetType, type =>
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                     .Where(p => p.CanWrite); // Only consider properties we can set

                var instructions = new List<MappingInfo>();
                foreach (var prop in properties)
                {
                    var mapAttribute = prop.GetCustomAttribute<CrmMapAttribute>();
                    if (mapAttribute != null)
                    {
                        instructions.Add(new MappingInfo
                        {
                            TargetProperty = prop,
                            MapAttribute = mapAttribute
                        });
                    }
                }
                return instructions;
            });
        }

        // Internal helper to map a single entity using pre-calculated instructions
        private static T MapSingleEntityToPoco<T>(Entity entity, List<MappingInfo> mappingInstructions) where T : new()
        {
            if (entity == null) return default;

            var pocoInstance = new T();

            foreach (var instruction in mappingInstructions)
            {
                string logicalName = instruction.MapAttribute.LogicalName;
                object crmValue = null;

                // Check if the attribute exists in the entity
                if (entity.Contains(logicalName))
                {
                    crmValue = entity[logicalName];
                }
                // Special handling for FormattedValues
                if (instruction.MapAttribute.SourcePart == CrmMapSourcePart.FormattedValue && entity.FormattedValues.Contains(logicalName))
                {
                    crmValue = entity.FormattedValues[logicalName]; // Already a string
                }

                if (crmValue != null)
                {
                    try
                    {
                        // Handle AliasedValue by unwrapping it
                        if (crmValue is AliasedValue aliasedValue)
                        {
                            crmValue = aliasedValue.Value;
                        }

                        // Perform conversion based on CRM type and SourcePart hint
                        object convertedValue = ConvertCrmValue(crmValue, instruction.MapAttribute, instruction.TargetProperty.PropertyType);

                        // Set the value on the POCO property
                        if (convertedValue != null || IsNullable(instruction.TargetProperty.PropertyType))
                        {
                            instruction.TargetProperty.SetValue(pocoInstance, convertedValue);
                        }
                    }
                    catch (Exception ex) // Catch potential conversion/setting errors
                    {
                        // Log the error: Which property, which entity ID, what exception?
                        Console.WriteLine($"Error mapping property '{instruction.TargetProperty.Name}' for entity '{entity.Id}': {ex.Message}");
                        // Depending on requirements, you might throw, continue, or set to default.
                    }
                }
            }
            return pocoInstance;
        }

        // Core conversion logic
        private static object ConvertCrmValue(object crmValue, CrmMapAttribute mapAttribute, Type targetType)
        {
            if (crmValue == null) return null;

            Type underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            CrmMapSourcePart sourcePart = mapAttribute.SourcePart; // Get from attribute

            // Specific handling for DateTime -> string with format ***
            if (crmValue is DateTime dateTimeValue && underlyingTargetType == typeof(string))
            {
                // Check if a specific format string is provided in the attribute
                if (!string.IsNullOrEmpty(mapAttribute.DateTimeFormat))
                {
                    // Use the specified format. Consider CultureInfo if needed: .ToString(format, CultureInfo.InvariantCulture)
                    return dateTimeValue.ToString(mapAttribute.DateTimeFormat);
                }
                // If no format is specified, DO NOT fall through to ChangeType for DateTime->string
                // Instead, choose a sensible default or let ChangeType handle it if that's preferred.
                // Using ISO 8601 "o" format is often a good, unambiguous default.
                return dateTimeValue.ToString("o");
            }

            // --- Handle EntityReference ---
            if (crmValue is EntityReference entityRef)
            {
                switch (sourcePart)
                {
                    case CrmMapSourcePart.Id:
                    case CrmMapSourcePart.Default when underlyingTargetType == typeof(Guid):
                        return ChangeType(entityRef.Id, targetType); // Guid
                    case CrmMapSourcePart.Name:
                    case CrmMapSourcePart.Default when underlyingTargetType == typeof(string):
                        return ChangeType(entityRef.Name, targetType); // string
                    //case CrmMapSourcePart.LogicalName:
                    //    return ChangeType(entityRef.LogicalName, targetType); // string
                    default:
                        // Try Id if Guid, Name if string, otherwise null? Or throw?
                        if (underlyingTargetType == typeof(Guid)) return ChangeType(entityRef.Id, targetType);
                        if (underlyingTargetType == typeof(string)) return ChangeType(entityRef.Name, targetType);
                        Console.WriteLine($"Warning: Cannot map EntityReference with SourcePart '{sourcePart}' to target type '{targetType.Name}'.");
                        return null;
                }
            }

            // --- Handle OptionSetValue ---
            if (crmValue is OptionSetValue optionSet)
            {
                switch (sourcePart)
                {
                    case CrmMapSourcePart.Value:
                    case CrmMapSourcePart.Default when underlyingTargetType == typeof(int):
                    case CrmMapSourcePart.Default when underlyingTargetType.IsEnum:
                        // Convert int value to the target type (int or Enum)
                        return ChangeType(optionSet.Value, targetType);
                    case CrmMapSourcePart.FormattedValue: // Should have been handled earlier, but check just in case
                        return null; // Formatted value handled separately
                    default:
                        // Fallback to value if possible?
                        if (underlyingTargetType == typeof(int) || underlyingTargetType.IsEnum) return ChangeType(optionSet.Value, targetType);
                        Console.WriteLine($"Warning: Cannot map OptionSetValue with SourcePart '{sourcePart}' to target type '{targetType.Name}'.");
                        return null;
                }
            }

            // --- Handle Money ---
            if (crmValue is Money money)
            {
                switch (sourcePart)
                {
                    case CrmMapSourcePart.Value:
                    case CrmMapSourcePart.Default when underlyingTargetType == typeof(decimal):
                        return ChangeType(money.Value, targetType); // decimal
                    default:
                        if (underlyingTargetType == typeof(decimal)) return ChangeType(money.Value, targetType);
                        Console.WriteLine($"Warning: Cannot map Money with SourcePart '{sourcePart}' to target type '{targetType.Name}'.");
                        return null;
                }
            }

            // --- Handle Multi-Select Optionset ---
            if (crmValue is OptionSetValueCollection collection)
            {
                if (underlyingTargetType == typeof(List<int>))
                {
                    return collection.Select(osv => osv.Value).ToList();
                }
                // Add handling for List<Enum>, int[], etc. if needed
                else
                {
                    Console.WriteLine($"Warning: Cannot map OptionSetValueCollection to target type '{targetType.Name}'. Expected List<int> or similar.");
                    return null;
                }
            }

            // --- Handle FormattedValue (string direct mapping) ---
            if (sourcePart == CrmMapSourcePart.FormattedValue && crmValue is string formattedString)
            {
                return ChangeType(formattedString, targetType);
            }


            // --- Handle basic types (string, int, double, decimal, DateTime, Guid, bool) ---
            // If no specific type handled above, attempt direct conversion
            // This covers cases where sourcePart is Default and crmValue is already a primitive/string/datetime
            return ChangeType(crmValue, targetType);
        }

        // Helper for type conversion, handling nullables and enums
        private static object ChangeType(object value, Type targetType)
        {
            if (value == null) return null;

            Type underlyingTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // --- Enum Handling ---
            if (underlyingTargetType.IsEnum)
            {
                object enumValue = default; // Start with the default value for the enum (e.g., Unknown = 0)

                if (value is int intValue)
                {
                    // Check if the integer value is explicitly defined in the enum
                    if (Enum.IsDefined(underlyingTargetType, intValue))
                    {
                        enumValue = Enum.ToObject(underlyingTargetType, intValue);
                    }
                    else
                    {
                        // Value is not defined, log warning and keep the default value
                        Console.WriteLine($"Warning: Integer value '{intValue}' is not defined in Enum type '{underlyingTargetType.Name}'. Returning default value '{enumValue}'. Mapping from CRM field.");
                    }
                }
                else if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    // Attempt to parse string to enum (case-insensitive)
                    try
                    {
                        object parsedEnum = Enum.Parse(underlyingTargetType, stringValue, ignoreCase: true);
                        // Optional: Check if the parsed enum value corresponds to a defined member
                        // if (Enum.IsDefined(underlyingTargetType, parsedEnum)) { enumValue = parsedEnum; }
                        // else { Console.WriteLine($"Warning: Parsed string '{stringValue}' to enum value '{parsedEnum}' which is not defined in Enum type '{underlyingTargetType.Name}'. Returning default value."); }
                        // Simpler: Just accept the parsed value if successful
                        enumValue = parsedEnum;
                    }
                    catch (ArgumentException ex)
                    {
                        // String value doesn't match any enum member name
                        Console.WriteLine($"Warning: Cannot parse string '{stringValue}' to Enum type '{underlyingTargetType.Name}'. {ex.Message}. Returning default value '{enumValue}'.");
                    }
                }
                else
                {
                    // Source value type (e.g., double) cannot be directly converted
                    Console.WriteLine($"Warning: Cannot convert value of type '{value.GetType().Name}' to Enum type '{underlyingTargetType.Name}'. Returning default value '{enumValue}'.");
                }

                // Return the determined enum value (which will be the default if conversion failed or value was undefined)
                return enumValue;
                // We explicitly handle enums here and return, so the code below won't execute for enums.
            }

            // Handle Guid specifically if needed (e.g., converting from string)
            if (underlyingTargetType == typeof(Guid) && value is string guidString)
            {
                if (Guid.TryParse(guidString, out Guid parsedGuid))
                {
                    return parsedGuid;
                }
                else
                {
                    Console.WriteLine($"Warning: Cannot parse string '{guidString}' as Guid.");
                    return null; // Or throw?
                }
            }


            try
            {
                return Convert.ChangeType(value, underlyingTargetType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Convert.ChangeType failed for value type '{value.GetType().Name}' to target type '{underlyingTargetType.Name}'. Exception: {ex.Message}");
                return null; // Or throw?
            }
        }

        // Helper to check if a type is nullable
        private static bool IsNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}
