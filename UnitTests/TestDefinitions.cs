// File:    TestDefinitions.cs
// Author:  Abdul Rafay Ali Khan
// Date:    2025-04-20

using EntityPocoBridge;

namespace UnitTests
{
    public enum TestStatusReason { Unknown = 0, Active = 1, Inactive = 2 }

    public class BasicPoco
    {
        [CrmMap("name")] 
        public string Name { get; set; }
        [CrmMap("description")] 
        public string Description { get; set; } // Test null source
        [CrmMap("count")] 
        public int Count { get; set; } // Test non-nullable int
        [CrmMap("amount")] 
        public decimal? Amount { get; set; } // Test nullable decimal
        [CrmMap("isvalid")] 
        public bool IsValid { get; set; } // Test non-nullable bool
        [CrmMap("itemid")] 
        public Guid ItemId { get; set; } // Test non-nullable Guid
    }

    public class ComplexPoco
    {
        [CrmMap("recordid", CrmMapSourcePart.Id)] 
        public Guid Id { get; set; }

        // EntityReference Tests
        [CrmMap("primarycontact", CrmMapSourcePart.Id)] 
        public Guid? ContactId { get; set; }
        [CrmMap("primarycontact", CrmMapSourcePart.Name)] 
        public string ContactName { get; set; }
        [CrmMap("accountlookup")] 
        public Guid? AccountId_Default { get; set; } // Test Default mapping Guid
        [CrmMap("accountlookup")] 
        public string AccountName_Default { get; set; } // Test Default mapping string (should conflict without explicit name) -> Let's use explicit name
        [CrmMap("accountlookup", CrmMapSourcePart.Name)] 
        public string AccountName_Explicit { get; set; }

        // OptionSetValue Tests
        [CrmMap("statuscode", CrmMapSourcePart.Value)] 
        public int? StatusValue { get; set; }
        [CrmMap("statuscode", CrmMapSourcePart.FormattedValue)] 
        public string StatusLabel { get; set; }
        [CrmMap("statecode")] 
        public TestStatusReason StateEnumValue { get; set; } // Test Default mapping to Enum

        // Money Tests
        [CrmMap("revenue", CrmMapSourcePart.Value)] 
        public decimal? RevenueValue { get; set; }
        [CrmMap("estimatedvalue")] 
        public decimal? EstimatedValue_Default { get; set; } // Test Default mapping

        // DateTime Tests
        [CrmMap("createdon")] 
        public DateTime? CreatedOnDate { get; set; }
        [CrmMap("modifiedon", DateTimeFormat = "yyyy-MM-dd")] 
        public string ModifiedString { get; set; }
        [CrmMap("closedate", DateTimeFormat = "dd/MM/yyyy HH:mm")] 
        public string CloseString { get; set; }
        [CrmMap("requestdeliveryby")] 
        public string DeliveryString_DefaultFormat { get; set; } // Map DateTime to string default

        // Aliased Value Tests
        [CrmMap("contactAlias.emailaddress1")] 
        public string ContactEmailAliased { get; set; }
        [CrmMap("accountAlias.revenue", CrmMapSourcePart.Value)] 
        public decimal? AccountRevenueAliased { get; set; }
        [CrmMap("userAlias.systemuserid", CrmMapSourcePart.Id)]
        public Guid? OwnerIdAliased { get; set; }
    }

    public class PocoWithMissingMappings
    {
        [CrmMap("field_exists")] 
        public string Exists { get; set; }
        [CrmMap("field_does_not_exist")] 
        public string DoesNotExist { get; set; }
    }
}
