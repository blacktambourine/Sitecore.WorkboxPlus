using System;
using BlackTambourine.Sitecore8.WorkboxPlus.V2.WorkboxPlus;
using Sitecore.Configuration;
using SitecoreContext = Sitecore.Context;

namespace BlackTambourine.Sitecore8.WorkboxPlus.V2.sitecore.shell.Applications.WorkboxPlusV2
{
    public partial class wbp : System.Web.UI.Page
    {
        /// <summary>
        /// Return WorkboxPlus.config settings
        /// </summary>
        /// <returns></returns>
        private readonly WorkboxPlusV2Config _workboxPlusConfigSettings = Factory.CreateObject("WorkboxPlusV2/configuration", true) as WorkboxPlusV2Config;

        public string _workflowGuid { get { return _workboxPlusConfigSettings.WorkFlowGuid.ToString(); } }

        public string _currentUserName { get { return SitecoreContext.GetUserName(); } }

        public string _currentUserEmail { get { return SitecoreContext.User.Profile.Email; } }

        public string _draftGuid { get { return _workboxPlusConfigSettings.DraftGuid.ToString(); } }

        public string _rejGuid { get { return _workboxPlusConfigSettings.RejectedGuid.ToString(); } }

        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}