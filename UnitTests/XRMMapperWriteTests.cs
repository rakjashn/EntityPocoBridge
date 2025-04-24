using EntityPocoBridge;
using Microsoft.Xrm.Sdk;

namespace UnitTests
{
    [TestFixture]
    public partial class XrmMapperWriteTests
    {

        #region General and Edge Case Tests

        [Test] // NUnit attribute
        public void MapEntityToPoco_NullEntity_ReturnsNull()
        {
            Entity nullEntity = null;
            var result = XrmMapper.MapEntityToPoco<BasicPoco>(nullEntity);
            // NUnit assertion
            Assert.That(result, Is.Null);
        }

        [Test] // NUnit attribute
        public void MapCollectionToPocoList_NullCollection_ReturnsEmptyList()
        {
            EntityCollection nullCollection = null;
            var result = XrmMapper.MapCollectionToPocoList<BasicPoco>(nullCollection);
            // NUnit assertions
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test] // NUnit attribute
        public void MapCollectionToPocoList_EmptyCollection_ReturnsEmptyList()
        {
            var emptyCollection = new EntityCollection();
            var result = XrmMapper.MapCollectionToPocoList<BasicPoco>(emptyCollection);
            // NUnit assertions
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test] // NUnit attribute
        public void MapEntityToPoco_AttributeMissingInEntity_MapsToNullOrDefault()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            entity["field_exists"] = "Data Here";
            // "field_does_not_exist" is missing from entity

            // Act
            var result = XrmMapper.MapEntityToPoco<PocoWithMissingMappings>(entity);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Exists, Is.EqualTo("Data Here"));
            Assert.That(result.DoesNotExist, Is.Null); // String default is null
        }

        [Test] // NUnit attribute
        public void MapEntityToPoco_AttributeValueIsNull_MapsToNullOrDefault()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            entity["name"] = null; // Explicit null value
            entity["count"] = 1;   // Provide non-null for non-nullable target test
            entity["amount"] = null;
            entity["isvalid"] = false; // Provide non-null for non-nullable target test
            entity["itemid"] = Guid.Empty; // Provide non-null for non-nullable target test

            // Act
            var result = XrmMapper.MapEntityToPoco<BasicPoco>(entity);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.Null);
            Assert.That(result.Amount, Is.Null);
            Assert.That(result.Count, Is.EqualTo(1)); // Ensure non-nullables were mapped if present
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ItemId, Is.EqualTo(Guid.Empty));
        }

        #endregion

        #region Basic Type Mapping Tests (Create Scenario)

        [Test] // NUnit attribute
        public void MapPocoToEntity_Create_MapsSimpleTypesCorrectly()
        {
            // Arrange
            var poco = new SimpleWritePoco
            {
                Name = "Test Account",
                Description = "Test Description",
                Count = 10, // Uses corrected POCO attribute
                IsActive = true,
                CreatedOn = new DateTime(2025, 01, 01, 12, 0, 0, DateTimeKind.Utc)
            };
            string targetEntity = "account";

            // Act
            Entity result = XrmMapper.MapPocoToEntity(poco, targetEntity);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.LogicalName, Is.EqualTo(targetEntity));
            Assert.That(result.Id, Is.EqualTo(Guid.Empty), "ID should be empty for create");
            Assert.That(result.Attributes.Count, Is.EqualTo(5), "Should map all 5 properties");
            Assert.That(result.Attributes["name"], Is.EqualTo("Test Account"));
            Assert.That(result.Attributes["description"], Is.EqualTo("Test Description"));
            Assert.That(result.Attributes["count"], Is.EqualTo(10)); // Should be int, not OptionSetValue
            Assert.That(result.Attributes["isactive"], Is.EqualTo(true));
            Assert.That(result.Attributes["createdon"], Is.EqualTo(poco.CreatedOn));
        }

        [Test] // NUnit attribute
        public void MapPocoToEntity_Create_MapsComplexTypesCorrectly()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var customGuid = Guid.NewGuid();
            var poco = new ComplexWritePoco
            {
                AccountId = Guid.NewGuid(), // Simulate setting ID for create (though unusual)
                ContactLookupId = contactId,
                StatusReasonInt = 2, // Inactive
                StatusEnum = TestWriteStatus.Active, // Active
                Revenue = 12345.67m,
                CustomGuid = customGuid
            };
            string targetEntity = "account";

            // Act
            Entity result = XrmMapper.MapPocoToEntity(poco, targetEntity);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.LogicalName, Is.EqualTo(targetEntity));
            Assert.That(result.Attributes.Count, Is.EqualTo(6));

            // Check specific types
            Assert.That(result.Attributes["primarycontactid"], Is.InstanceOf<EntityReference>());
            var contactRef = (EntityReference)result.Attributes["primarycontactid"];
            Assert.That(contactRef.LogicalName, Is.EqualTo("contact"));
            Assert.That(contactRef.Id, Is.EqualTo(contactId));

            Assert.That(result.Attributes["statuscode"], Is.InstanceOf<OptionSetValue>());
            Assert.That(((OptionSetValue)result.Attributes["statuscode"]).Value, Is.EqualTo(2));

            Assert.That(result.Attributes["statecode"], Is.InstanceOf<OptionSetValue>());
            Assert.That(((OptionSetValue)result.Attributes["statecode"]).Value, Is.EqualTo((int)TestWriteStatus.Active));

            Assert.That(result.Attributes["revenue"], Is.InstanceOf<Money>());
            Assert.That(((Money)result.Attributes["revenue"]).Value, Is.EqualTo(12345.67m));

            // Check standard Guids are passed directly
            Assert.That(result.Attributes["accountid"], Is.EqualTo(poco.AccountId));
            Assert.That(result.Attributes["customguidfield"], Is.EqualTo(customGuid));
        }

        #endregion

        #region Update Scenario Tests (Null Handling)

        [Test] // NUnit attribute
        public void MapPocoToEntity_Update_Default_SkipsNullProperties()
        {
            // Arrange
            var recordId = Guid.NewGuid();
            var poco = new SimpleWritePoco
            {
                Name = "Updated Name",
                Description = null, // Should be skipped
                Count = null,       // Should be skipped
                IsActive = false,
                CreatedOn = null    // Should be skipped
            };
            string targetEntity = "account";

            // Act
            Entity result = XrmMapper.MapPocoToEntity(poco, targetEntity, recordId); // mapNullProperties is false (default)

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.LogicalName, Is.EqualTo(targetEntity));
            Assert.That(result.Id, Is.EqualTo(recordId), "ID should be set for update");
            Assert.That(result.Attributes.Count, Is.EqualTo(2), "Should only map Name and IsActive");
            Assert.That(result.Attributes.ContainsKey("name"), Is.True);
            Assert.That(result.Attributes["name"], Is.EqualTo("Updated Name"));
            Assert.That(result.Attributes.ContainsKey("isactive"), Is.True);
            Assert.That(result.Attributes["isactive"], Is.EqualTo(false));
            Assert.That(result.Attributes.ContainsKey("description"), Is.False);
            Assert.That(result.Attributes.ContainsKey("count"), Is.False);
            Assert.That(result.Attributes.ContainsKey("createdon"), Is.False);
        }

        [Test] // NUnit attribute
        public void MapPocoToEntity_Update_MapNullPropertiesTrue_IncludesNulls()
        {
            // Arrange
            var recordId = Guid.NewGuid();
            var poco = new SimpleWritePoco
            {
                Name = "Updated Name Again",
                Description = null, // Should be included as null
                Count = null,       // Should be included as null
                IsActive = true,
                CreatedOn = null    // Should be included as null
            };
            string targetEntity = "account";

            // Act
            Entity result = XrmMapper.MapPocoToEntity(poco, targetEntity, recordId, mapNullProperties: true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.LogicalName, Is.EqualTo(targetEntity));
            Assert.That(result.Id, Is.EqualTo(recordId));
            Assert.That(result.Attributes.Count, Is.EqualTo(5), "Should map all 5 properties");
            Assert.That(result.Attributes.ContainsKey("name"), Is.True);
            Assert.That(result.Attributes["name"], Is.EqualTo("Updated Name Again"));
            Assert.That(result.Attributes.ContainsKey("description"), Is.True);
            Assert.That(result.Attributes["description"], Is.Null); // Explicitly null
            Assert.That(result.Attributes.ContainsKey("count"), Is.True);
            Assert.That(result.Attributes["count"], Is.Null);       // Explicitly null
            Assert.That(result.Attributes.ContainsKey("isactive"), Is.True);
            Assert.That(result.Attributes["isactive"], Is.EqualTo(true));
            Assert.That(result.Attributes.ContainsKey("createdon"), Is.True);
            Assert.That(result.Attributes["createdon"], Is.Null);   // Explicitly null
        }

        #endregion

        #region Specific Type Handling Tests (Write)

        [Test] // NUnit attribute
        public void MapPocoToEntity_EntityReference_HandlesEmptyAndNullGuid_WithMapNullsFalse()
        {
            // Arrange
            var pocoWithEmptyGuid = new ComplexWritePoco { ContactLookupId = Guid.Empty }; // Value is Guid.Empty (not null)
            var pocoWithNullGuid = new ComplexWritePoco { ContactLookupId = null };       // Value is null
            string targetEntity = "account";
            bool mapNullProperties = false; // Explicitly state the default for clarity

            // Act
            Entity resultEmpty = XrmMapper.MapPocoToEntity(pocoWithEmptyGuid, targetEntity, mapNullProperties: mapNullProperties);
            Entity resultNull = XrmMapper.MapPocoToEntity(pocoWithNullGuid, targetEntity, mapNullProperties: mapNullProperties);

            // Assert
            // Empty Guid should still result in the key being added with a null value
            Assert.That(resultEmpty.Attributes.ContainsKey("primarycontactid"), Is.True, "Key should exist for Guid.Empty source.");
            Assert.That(resultEmpty.Attributes["primarycontactid"], Is.Null, "Value should be null for Guid.Empty source.");

            // Null Guid? should result in the key being SKIPPED when mapNullProperties is false
            Assert.That(resultNull.Attributes.ContainsKey("primarycontactid"), Is.False, "Key should NOT exist for null source when mapNullProperties is false.");
        }

        [Test] // NUnit attribute
        public void MapPocoToEntity_EntityReference_HandlesNullGuid_WithMapNullsTrue()
        {
            // Arrange
            var pocoWithNullGuid = new ComplexWritePoco { ContactLookupId = null };       // Value is null
            string targetEntity = "account";
            bool mapNullProperties = true;

            // Act
            Entity resultNull = XrmMapper.MapPocoToEntity(pocoWithNullGuid, targetEntity, mapNullProperties: mapNullProperties);

            // Assert
            // Null Guid? should result in the key being ADDED with a null value when mapNullProperties is true
            Assert.That(resultNull.Attributes.ContainsKey("primarycontactid"), Is.True, "Key should exist for null source when mapNullProperties is true.");
            Assert.That(resultNull.Attributes["primarycontactid"], Is.Null, "Value should be null for null source when mapNullProperties is true.");
        }


        [Test] // NUnit attribute
        public void MapPocoToEntity_Guid_MapsDirectlyWhenNoTargetEntityName()
        {
            // Arrange
            var customGuid = Guid.NewGuid();
            var poco = new ComplexWritePoco { CustomGuid = customGuid }; // CustomGuid has no TargetEntityLogicalName
            string targetEntity = "account";

            // Act
            Entity result = XrmMapper.MapPocoToEntity(poco, targetEntity);

            // Assert
            Assert.That(result.Attributes.ContainsKey("customguidfield"), Is.True);
            Assert.That(result.Attributes["customguidfield"], Is.InstanceOf<Guid>());
            Assert.That(result.Attributes["customguidfield"], Is.EqualTo(customGuid));
        }

        [Test] // NUnit attribute
        public void MapPocoToEntity_OptionSet_HandlesNullIntAndEnum()
        {
            // Arrange
            var poco = new ComplexWritePoco
            {
                StatusReasonInt = null,
                StatusEnum = null // Assuming TestWriteStatus is nullable enum
            };
            string targetEntity = "account";

            // Act
            Entity result = XrmMapper.MapPocoToEntity(poco, targetEntity, mapNullProperties: true); // Map nulls

            // Assert
            Assert.That(result.Attributes.ContainsKey("statuscode"), Is.True);
            Assert.That(result.Attributes["statuscode"], Is.Null); // Null int? maps to null OptionSetValue

            Assert.That(result.Attributes.ContainsKey("statecode"), Is.True);
            Assert.That(result.Attributes["statecode"], Is.Null); // Null enum? maps to null OptionSetValue
        }

        [Test] // NUnit attribute
        public void MapPocoToEntity_Money_HandlesNullDecimal()
        {
            // Arrange
            var poco = new ComplexWritePoco { Revenue = null };
            string targetEntity = "account";

            // Act
            Entity result = XrmMapper.MapPocoToEntity(poco, targetEntity, mapNullProperties: true); // Map nulls

            // Assert
            Assert.That(result.Attributes.ContainsKey("revenue"), Is.True);
            Assert.That(result.Attributes["revenue"], Is.Null); // Null decimal? maps to null Money
        }

        #endregion

        #region Validation/Edge Case Tests (Write)

        [Test] // NUnit attribute
        public void MapPocoToEntity_NullPoco_ThrowsArgumentNullException_NUnit()
        {
            // Arrange
            SimpleWritePoco nullPoco = null;
            string targetEntity = "account";
            string expectedParamName = "pocoObject"; // Parameter name from the exception

            // Act & Assert
            // Use NUnit's Assert.Throws<T>
            var ex = Assert.Throws<ArgumentNullException>(() =>
                XrmMapper.MapPocoToEntity(nullPoco, targetEntity)
            , "Test should throw ArgumentNullException for null POCO input.");

            // Optional but recommended: Verify the parameter name in the exception
            Assert.That(ex.ParamName, Is.EqualTo(expectedParamName), "Incorrect parameter name in exception.");
        }

        [Test] // NUnit attribute
        public void MapPocoToEntity_NullOrEmptyLogicalName_ThrowsArgumentNullException_NUnit()
        {
            // Arrange
            var poco = new SimpleWritePoco { Name = "Test" };
            string expectedParamName = "targetEntityLogicalName"; // Expected parameter name in the exception

            // Act & Assert for null input
            var exNull = Assert.Throws<ArgumentNullException>(() =>
                XrmMapper.MapPocoToEntity(poco, null) // Call with null
            , "Test should throw ArgumentNullException for null logical name.");
            Assert.That(exNull.ParamName, Is.EqualTo(expectedParamName), "ParamName mismatch for null input.");

            // Act & Assert for empty string input
            var exEmpty = Assert.Throws<ArgumentNullException>(() =>
                XrmMapper.MapPocoToEntity(poco, "") // Call with empty string
            , "Test should throw ArgumentNullException for empty logical name.");
            Assert.That(exEmpty.ParamName, Is.EqualTo(expectedParamName), "ParamName mismatch for empty string input.");

            // Act & Assert for whitespace input
            var exWhitespace = Assert.Throws<ArgumentNullException>(() =>
                XrmMapper.MapPocoToEntity(poco, "   ") // Call with whitespace
            , "Test should throw ArgumentNullException for whitespace logical name.");
            Assert.That(exWhitespace.ParamName, Is.EqualTo(expectedParamName), "ParamName mismatch for whitespace input.");
        }

        #endregion

    }
}


