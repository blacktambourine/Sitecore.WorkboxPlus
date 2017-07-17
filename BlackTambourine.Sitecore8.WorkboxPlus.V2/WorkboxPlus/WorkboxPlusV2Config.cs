using System;
using Sitecore.Mvc.Extensions;

namespace BlackTambourine.Sitecore8.WorkboxPlus.V2.WorkboxPlus
{
    public class WorkboxPlusV2Config
    {
        public Guid WorkFlowGuid { get; set; }
        public Guid DraftGuid { get; set; }
        public Guid RejectedGuid { get; set; }

        public WorkboxPlusV2Config(string workFlowGuid, string draftGuid, string rejectedGuid)
        {
            WorkFlowGuid = workFlowGuid.ToGuid();
            DraftGuid = draftGuid.ToGuid();
            RejectedGuid = rejectedGuid.ToGuid();
        }
    }
}