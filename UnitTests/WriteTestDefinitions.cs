using EntityPocoBridge;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    public partial class XrmMapperWriteTests
    {
        public enum TestWriteStatus { Unknown = 0, Active = 1, Inactive = 2 } // Added Unknown for default

        public class SimpleWritePoco
        {
            [CrmMap("name")]
            public string Name { get; set; }

            [CrmMap("description")]
            public string Description { get; set; }

            [CrmMap("count")]
            public int? Count { get; set; }

            [CrmMap("isactive")] // Two Option Set (bool)
            public bool? IsActive { get; set; }

            [CrmMap("createdon")] // DateTime
            public DateTime? CreatedOn { get; set; }
        }

        public class ComplexWritePoco
        {
            [CrmMap("accountid")] // Standard Guid (e.g., Primary Key if used for Create)
            public Guid AccountId { get; set; }

            // Lookup mapping
            [CrmMap("primarycontactid", TargetEntityLogicalName = "contact")]
            public Guid? ContactLookupId { get; set; }

            // Option Set mapping (using int)
            [CrmMap("statuscode", CrmMapSourcePart.Value)]
            public int? StatusReasonInt { get; set; }

            // Option Set mapping (using enum)
            [CrmMap("statecode", CrmMapSourcePart.Value)]
            public TestWriteStatus? StatusEnum { get; set; }

            // Money mapping
            [CrmMap("revenue")] // Default hint often implies Money for decimal
            public decimal? Revenue { get; set; }

            // Guid property WITHOUT TargetEntityLogicalName (should be treated as standard Guid)
            [CrmMap("customguidfield")]
            public Guid? CustomGuid { get; set; }
        }

        // --- Test Setup/Helper Data ---
        private readonly Guid _testGuid = Guid.NewGuid();
        private readonly DateTime _testDate = new DateTime(2025, 4, 20, 14, 30, 0, DateTimeKind.Utc);

        // Helper to create a base entity for reuse
        private Entity CreateBaseMockEntity(string logicalName = "mockentity")
        {
            return new Entity(logicalName, Guid.NewGuid()); // Give entity an ID for context
        }
    }
}
