using System;
using System.Collections.Generic;
using Sitecore.Xml;

namespace BlackTambourine.Sitecore8.WorkboxPlus.Config
{
    public class WorkboxPlusConfig
    {
        public Dictionary<Guid, string> ItemTemplateNames { get; private set; }
        public Dictionary<Guid, string> FilterableWorkflowStates { get; private set; }
        public bool EnablePageLevelApproval { get; set; }

        public WorkboxPlusConfig(string enablePageLevelApproval)
        {
            this.ItemTemplateNames = new Dictionary<Guid, string>();
            this.FilterableWorkflowStates = new Dictionary<Guid, string>();
            EnablePageLevelApproval = true; //default to true;
            EnablePageLevelApproval = bool.Parse(enablePageLevelApproval);
        }

        #region Child Item Templates
        public void AddWorkboxPlusItemTemplateName(string key, System.Xml.XmlNode node)
        {
            AddWorkboxPlusItemTemplateName(node);
        }

        public void AddWorkboxPlusItemTemplateName(System.Xml.XmlNode node)
        {
            var guid = XmlUtil.GetValue(node);
            var name = XmlUtil.GetAttribute("name", node);
            this.ItemTemplateNames.Add(new Guid(guid), name);
        }
        #endregion

        #region Filterable Workflow States
        public void AddWorkboxPlusFilterableState(string key, System.Xml.XmlNode node)
        {
            AddWorkboxPlusFilterableState(node);
        }

        public void AddWorkboxPlusFilterableState(System.Xml.XmlNode node)
        {
            var guid = XmlUtil.GetValue(node);
            var name = XmlUtil.GetAttribute("name", node);
            this.FilterableWorkflowStates.Add(new Guid(guid), name);
        }
        #endregion
    }
}
