using System.Collections.Generic;

namespace SuccessStory.PlayniteResources.OriginLibrary.Models
{
    public class AccountEntitlementsResponse
    {
        public class Entitlement
        {
            public long entitlementId;
            public string offerId;
            public string offerPath;
            public string status;
            public string offerType;
            public string originDisplayType;
            public string masterTitleId;
            public string gameDistributionSubType;
        }

        public string error;
        public List<Entitlement> entitlements;
    }
}
