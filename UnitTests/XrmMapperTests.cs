// File:    XrmMapperTests.cs
// Author:  Abdul Rafay Ali Khan
// Date:    2025-04-20

using EntityPocoBridge;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace UnitTests
{
    [TestClass]
    public class XrmMapperTests
    {
        private readonly Guid _testGuid = Guid.NewGuid();
        private readonly DateTime _testDate = new DateTime(2025, 4, 20, 14, 30, 0, DateTimeKind.Utc);

        // Helper to create a base entity for reuse
        private Entity CreateBaseMockEntity(string logicalName = "mockentity")
        {
            return new Entity(logicalName, Guid.NewGuid()); // Give entity an ID for context
        }

        #region General and Edge Case Tests

        [Test]
        public void MapEntityToPoco_NullEntity_ReturnsNull()
        {
            Entity nullEntity = null;
            var result = XrmMapper.MapEntityToPoco<BasicPoco>(nullEntity);
            Assert.IsNull(result);
        }

        [Test]
        public void MapCollectionToPocoList_NullCollection_ReturnsEmptyList()
        {
            EntityCollection nullCollection = null;
            var result = XrmMapper.MapCollectionToPocoList<BasicPoco>(nullCollection);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void MapCollectionToPocoList_EmptyCollection_ReturnsEmptyList()
        {
            var emptyCollection = new EntityCollection();
            var result = XrmMapper.MapCollectionToPocoList<BasicPoco>(emptyCollection);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void MapEntityToPoco_AttributeMissingInEntity_MapsToNullOrDefault()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            entity["field_exists"] = "Data Here";
            // "field_does_not_exist" is missing from entity

            // Act
            var result = XrmMapper.MapEntityToPoco<PocoWithMissingMappings>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Data Here", result.Exists);
            Assert.IsNull(result.DoesNotExist); // String default is null
        }

        [Test]
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
            Assert.IsNotNull(result);
            Assert.IsNull(result.Name);
            Assert.IsNull(result.Amount);
            Assert.AreEqual(1, result.Count); // Ensure non-nullables were mapped if present
            Assert.AreEqual(false, result.IsValid);
            Assert.AreEqual(Guid.Empty, result.ItemId);
        }

        #endregion

        #region Basic Type Mapping Tests

        [Test]
        public void MapEntityToPoco_BasicTypes_AllPresent_MapsCorrectly()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            entity["name"] = "Test Name";
            entity["description"] = "Desc";
            entity["count"] = 123;
            entity["amount"] = 45.67m;
            entity["isvalid"] = true;
            entity["itemid"] = _testGuid;

            // Act
            var result = XrmMapper.MapEntityToPoco<BasicPoco>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Test Name", result.Name);
            Assert.AreEqual("Desc", result.Description);
            Assert.AreEqual(123, result.Count);
            Assert.AreEqual(45.67m, result.Amount);
            Assert.AreEqual(true, result.IsValid);
            Assert.AreEqual(_testGuid, result.ItemId);
        }

        #endregion

        #region DateTime Mapping Tests

        [Test]
        public void MapEntityToPoco_DateTime_MapsDirectAndFormatted()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            entity["createdon"] = _testDate;
            entity["modifiedon"] = _testDate;
            entity["closedate"] = _testDate;
            entity["requestdeliveryby"] = _testDate;

            // Act
            var result = XrmMapper.MapEntityToPoco<ComplexPoco>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testDate, result.CreatedOnDate);
            Assert.AreEqual("2025-04-20", result.ModifiedString); // Custom format
            Assert.AreEqual("20/04/2025 14:30", result.CloseString); // Custom format
            Assert.AreEqual("2025-04-20T14:30:00.0000000Z", result.DeliveryString_DefaultFormat); // ISO 8601 "o"
        }

        [Test]
        public void MapEntityToPoco_DateTime_HandlesNulls()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            // createdon, modifiedon, closedate, requestdeliveryby are MISSING

            // Act
            var result = XrmMapper.MapEntityToPoco<ComplexPoco>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.CreatedOnDate);
            Assert.IsNull(result.ModifiedString);
            Assert.IsNull(result.CloseString);
            Assert.IsNull(result.DeliveryString_DefaultFormat);
        }

        #endregion

        #region EntityReference Mapping Tests

        [Test]
        public void MapEntityToPoco_EntityReference_MapsIdAndName()
        {
            // Arrange
            var contactRef = new EntityReference("contact", _testGuid) { Name = "Test Contact" };
            var accountRef = new EntityReference("account", Guid.NewGuid()) { Name = "Test Account" };
            var entity = CreateBaseMockEntity();
            entity["primarycontact"] = contactRef;
            entity["accountlookup"] = accountRef;

            // Act
            var result = XrmMapper.MapEntityToPoco<ComplexPoco>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testGuid, result.ContactId);
            Assert.AreEqual("Test Contact", result.ContactName);
            Assert.AreEqual(accountRef.Id, result.AccountId_Default);
            Assert.AreEqual("Test Account", result.AccountName_Explicit);
        }

        [Test]
        public void MapEntityToPoco_EntityReference_HandlesNull()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            entity["primarycontact"] = null; // Explicit null
                                             // accountlookup is missing

            // Act
            var result = XrmMapper.MapEntityToPoco<ComplexPoco>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.ContactId);
            Assert.IsNull(result.ContactName);
            Assert.IsNull(result.AccountId_Default);
            Assert.IsNull(result.AccountName_Explicit);
        }

        #endregion

        #region OptionSetValue Mapping Tests

        [Test]
        public void MapEntityToPoco_OptionSet_MapsValueLabelEnum()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            entity["statuscode"] = new OptionSetValue(2); // e.g., Inactive
            entity.FormattedValues.Add("statuscode", "Inactive Label");
            entity["statecode"] = new OptionSetValue(1); // e.g., Active

            // Act
            var result = XrmMapper.MapEntityToPoco<ComplexPoco>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.StatusValue);
            Assert.AreEqual("Inactive Label", result.StatusLabel);
            Assert.AreEqual(TestStatusReason.Active, result.StateEnumValue); // Assumes 1 maps to Active
        }

        [Test]
        public void MapEntityToPoco_OptionSet_HandlesNullAndMissingFormatted()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            entity["statuscode"] = null; // Explicit null
            entity["statecode"] = new OptionSetValue(99); // Value not in enum definition
                                                          // FormattedValue for statuscode is MISSING

            // Act
            var result = XrmMapper.MapEntityToPoco<ComplexPoco>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.StatusValue);
            Assert.IsNull(result.StatusLabel); // Should be null if formatted value missing
            Assert.AreEqual(TestStatusReason.Unknown, result.StateEnumValue); // Should default if value not defined
        }

        #endregion

        #region Money Mapping Tests

        [Test]
        public void MapEntityToPoco_Money_MapsValueCorrectly()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            entity["revenue"] = new Money(12345.99m);
            entity["estimatedvalue"] = new Money(1000m);

            // Act
            var result = XrmMapper.MapEntityToPoco<ComplexPoco>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(12345.99m, result.RevenueValue);
            Assert.AreEqual(1000m, result.EstimatedValue_Default);
        }

        #endregion

        #region AliasedValue Mapping Tests

        [Test]
        public void MapEntityToPoco_AliasedValues_MapCorrectly()
        {
            // Arrange
            var entity = CreateBaseMockEntity();
            var ownerId = Guid.NewGuid();
            entity["contactAlias.emailaddress1"] = new AliasedValue("contact", "emailaddress1", "test@example.com");
            entity["accountAlias.revenue"] = new AliasedValue("account", "revenue", new Money(987.65m));
            entity["userAlias.systemuserid"] = new AliasedValue("systemuser", "systemuserid", ownerId); // Simulate aliased lookup ID

            // Act
            var result = XrmMapper.MapEntityToPoco<ComplexPoco>(entity);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test@example.com", result.ContactEmailAliased);
            Assert.AreEqual(987.65m, result.AccountRevenueAliased);
            Assert.AreEqual(ownerId, result.OwnerIdAliased);
        }

        #endregion

        #region Collection Mapping Tests

        [Test]
        public void MapCollectionToPocoList_MapsMultipleEntities()
        {
            // Arrange
            var entity1 = CreateBaseMockEntity();
            entity1["name"] = "Entity 1";
            entity1["count"] = 1;

            var entity2 = CreateBaseMockEntity();
            entity2["name"] = "Entity 2";
            entity2["count"] = 2;

            var collection = new EntityCollection(new List<Entity> { entity1, entity2 });

            // Act
            var result = XrmMapper.MapCollectionToPocoList<BasicPoco>(collection);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Entity 1", result[0].Name);
            Assert.AreEqual(1, result[0].Count);
            Assert.AreEqual("Entity 2", result[1].Name);
            Assert.AreEqual(2, result[1].Count);
        }

        #endregion
    }
}