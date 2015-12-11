using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Globalization;
using Sitecore.Pipelines.GetWorkflowCommentsDisplay;
using Sitecore.Resources;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Data;
using Sitecore.Shell.Feeds;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.CommandBuilders;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls.Ribbons;
using Sitecore.Web.UI.XmlControls;
using Sitecore.Workflows;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BlackTambourine.Sitecore8.WorkboxPlus.Config;

namespace BlackTambourine.Sitecore8.WorkboxPlus.Workbox
{
    public class WorkboxFormPlus : BaseForm
    {
        #region Properties
        /// <summary>
        /// Gets or sets the offset(what page we are on).
        /// 
        /// </summary>
        /// 
        /// <value>
        /// The size of the offset.
        /// </value>
        private readonly WorkboxFormPlus.OffsetCollection Offset = new WorkboxFormPlus.OffsetCollection();
        private class OffsetCollection
        {
            public int this[string key]
            {
                get
                {
                    if (Context.ClientPage.ServerProperties[key] != null)
                        return (int)Context.ClientPage.ServerProperties[key];
                    UrlString urlString = new UrlString(WebUtil.GetRawUrl());
                    int result;
                    if (urlString[key] != null && int.TryParse(urlString[key], out result))
                        return result;
                    return 0;
                }
                set
                {
                    Context.ClientPage.ServerProperties[key] = (object)value;
                }
            }
        }
        /// <summary>
        /// The pager.
        /// 
        /// </summary>
        protected Border Pager;
        /// <summary>
        /// The ribbon panel.
        /// 
        /// </summary>
        protected Border RibbonPanel;
        /// <summary>
        /// The states.
        /// 
        /// </summary>
        protected Border States;
        /// <summary>
        /// The view menu.
        /// 
        /// </summary>
        protected Toolmenubutton ViewMenu;
        /// <summary>
        /// The _state names.
        /// 
        /// </summary>
        private NameValueCollection stateNames;

        /// <summary>
        /// Gets or sets the size of the page.
        /// 
        /// </summary>
        /// 
        /// <value>
        /// The size of the page.
        /// </value>
        public int PageSize
        {
            get
            {
                return Registry.GetInt("/Current_User/Workbox/Page Size", 10);
            }
            set
            {
                Registry.SetInt("/Current_User/Workbox/Page Size", value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether page is reloads by reload button on the ribbon.
        /// 
        /// </summary>
        /// 
        /// <value>
        /// <c>true</c> if this instance is reload; otherwise, <c>false</c>.
        /// </value>
        protected virtual bool IsReload
        {
            get
            {
                return new UrlString(WebUtil.GetRawUrl())["reload"] == "1";
            }
        }

        /// <summary>
        /// Gets or sets User Filter.
        /// 
        /// </summary>
        /// 
        /// <value>
        /// bool
        /// </value>
        public bool IsUserFiltered
        {
            get
            {
                return Registry.GetBool("/Current_User/IsUserFiltered", true);
            }
            set
            {
                Registry.SetBool("/Current_User/IsUserFiltered", value);
            }
        }

        /// <summary>
        /// Return WorkboxPlus.config settings
        /// </summary>
        /// <returns></returns>
        private static readonly WorkboxPlusConfig WorkboxPlusConfigSettings = Factory.CreateObject("WorkboxPlus/configuration", true) as WorkboxPlusConfig;

        /// <summary>
        /// ParentPageId's of Dummy Rows
        /// </summary>
        private static readonly List<string> DummyParentPageIdList = new List<string>();

        /// <summary>
        /// List of Commands that have the "With Children" Action
        /// </summary>
        private static readonly List<string> CommandsWithChildrenActionList = new List<string>();
        private static readonly List<string> CommandsWithOutChildrenActionList = new List<string>();

        #endregion

        #region Workbox Plus Methods

        /// <summary>
        /// Does this command use the "With Children" Action
        /// </summary>
        /// <param name="commandId"></param>
        /// <returns></returns>
        private static bool WorkflowCommandUsesWithChildrenAction(string commandId)
        {
            var result = false;

            //handle any Commands already checked
            if (CommandsWithChildrenActionList.Any(x => x.Equals(commandId)))
            {
                return true;
            }
            if (CommandsWithOutChildrenActionList.Any(x => x.Equals(commandId)))
            {
                return false;
            }

            var commandItem = Factory.GetDatabase("master").GetItem(new ID(commandId));
            if (commandItem == null)
                return false;

            foreach (Item child in commandItem.Children)
            {
                if (child.Fields["Type"].Value.Contains("ExecuteCommandOnItemAndChildrenAction"))
                {
                    CommandsWithChildrenActionList.Add(commandId);
                    result = true;
                    break;
                }
                CommandsWithOutChildrenActionList.Add(commandId);
            }
            return result;
        }

        /// <summary>
        /// Add Dummy Parent if it does not already exist
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="workflowItems"></param>
        /// <param name="wbpItemTemplateNames"></param>
        /// <returns></returns>
        private static Item AddDummyParent(Item parentItem, IEnumerable<DataUri> workflowItems, IReadOnlyDictionary<Guid, string> wbpItemTemplateNames)
        {
            Item result = null;
            if (IsChildItem(parentItem, wbpItemTemplateNames))
                return null;

            //if the parent page is not present in workflow add it to the list
            if (!workflowItems.Any(x => x.ItemID.Equals(parentItem.ID)))
            {
                //check if already in Dummy Parent List
                if (!DummyParentPageIdList.Any(x => x.Contains(parentItem.ID.ToString())))
                {
                    DummyParentPageIdList.Add(parentItem.ID.ToString());
                }
                result = parentItem;
            }
            return result;
        }

        /// <summary>
        /// IsWorkboxPlusChildItemTemplate
        /// </summary>
        /// <param name="item"></param>
        /// <param name="wbpItemTemplateNames"></param>
        /// <returns></returns>
        private static bool IsChildItem(Item item, IReadOnlyDictionary<Guid, string> wbpItemTemplateNames)
        {
            return wbpItemTemplateNames.ContainsKey(item.TemplateID.ToGuid());
        }

        /// <summary>
        /// Permissions / Filters check
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="state"></param>
        /// <param name="isDummyRow"></param>
        /// <returns></returns>
        private static bool UserCanViewItem(Item obj, WorkflowState state, bool isDummyRow)
        {
            bool result;
            var isUserVisible = IsVisibleByUserFilter(obj, state.DisplayName, isDummyRow);

            if (!isDummyRow)
            {
                result = obj != null && obj.Access.CanRead() &&
                         (obj.Access.CanReadLanguage() && obj.Access.CanWriteLanguage()) &&
                         (Context.IsAdministrator || obj.Locking.CanLock() || obj.Locking.HasLock()) &&
                         isUserVisible;
            }
            else
            {
                result = obj != null && obj.Access.CanRead() && isUserVisible;
            }
            return result;
        }

        /// <summary>
        /// Return whether the item is visible using the current User Filter
        /// </summary>
        /// <param name="item"></param>
        /// <param name="stateId">Only filter for Draft and Rejected States</param>
        /// <param name="isDummyRow"></param>
        /// <returns></returns>
        private static bool IsVisibleByUserFilter(Item item, string stateId, bool isDummyRow)
        {
            if (item == null)
            {
                return false;
            }

            //if not checked 
            var isChecked = Registry.GetBool("/Current_User/IsUserFiltered");
            if (!isChecked)
            {
                return true;
            }

            //if not in Draft/Rejected            
            if (WorkboxPlusConfigSettings.FilterableWorkflowStates.All(x => x.Value != stateId))
            {
                return true;
            }

            if (isDummyRow)
            {
                return true;
            }

            //else if Is Checked
            var currentUserName = Context.GetUserName();
            var lastSubmitterUserName = GetLastSubmitterUserName(item);
            var userCheck = lastSubmitterUserName.Equals(currentUserName);
            return userCheck;
        }

        #endregion

        #region Workbox Page Events

        /// <summary>
        /// Raises the load event.
        /// 
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.
        ///             </param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        ///             request for the page it is associated with, such as setting up a database query. At this
        ///             stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        ///             view state is restored, and form controls reflect client-side data. Use the IsPostBack
        ///             property to determine whether the page is being loaded in response to a client postback,
        ///             or if it is being loaded and accessed for the first time.
        /// 
        /// </remarks>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull((object)e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
                if (workflowProvider != null)
                {
                    IWorkflow[] workflows = workflowProvider.GetWorkflows();
                    foreach (IWorkflow workflow in workflows)
                    {
                        string str = "P" + Regex.Replace(workflow.WorkflowID, "\\W", string.Empty);
                        if (!this.IsReload && workflows.Length == 1 && string.IsNullOrEmpty(Registry.GetString("/Current_User/Panes/" + str)))
                            Registry.SetString("/Current_User/Panes/" + str, "visible");
                        if ((Registry.GetString("/Current_User/Panes/" + str) ?? string.Empty) == "visible")
                            this.DisplayWorkflow(workflow);
                    }
                }
                this.UpdateRibbon();
            }
            this.WireUpNavigators((System.Web.UI.Control)Context.ClientPage);
        }

        /// <summary>
        /// Called when the view menu is clicked.
        /// 
        /// </summary>
        protected void OnViewMenuClick()
        {
            Menu menu = new Menu();
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider != null)
            {
                foreach (IWorkflow workflow in workflowProvider.GetWorkflows())
                {
                    string paneId = GetPaneID(workflow);
                    string @string = Registry.GetString("/Current_User/Panes/" + paneId);
                    string str = @string != "hidden" ? "workbox:hide" : "workbox:show";
                    menu.Add(Control.GetUniqueID("ctl"), workflow.Appearance.DisplayName, workflow.Appearance.Icon, string.Empty, str + "(id=" + paneId + ")", @string != "hidden", string.Empty, MenuItemType.Check);
                }
                if (menu.Controls.Count > 0)
                    menu.AddDivider();
                menu.Add("Refresh", "Office/16x16/refresh.png", "Refresh");
            }
            Context.ClientPage.ClientResponse.ShowPopup("ViewMenu", "below", (System.Web.UI.Control)menu);
        }

        /// <summary>
        /// Opens the specified item.
        /// 
        /// </summary>
        /// <param name="id">The id.
        ///             </param><param name="language">The language.
        ///             </param><param name="version">The version.
        ///             </param>
        protected void Open(string id, string language, string version)
        {
            Assert.ArgumentNotNull((object)id, "id");
            Assert.ArgumentNotNull((object)language, "language");
            Assert.ArgumentNotNull((object)version, "version");
            string sectionId = RootSections.GetSectionID(id);
            var urlString = new UrlString();
            urlString.Append("ro", sectionId);
            urlString.Append("fo", id);
            urlString.Append("id", id);
            urlString.Append("la", language);
            urlString.Append("vs", version);
            Windows.RunApplication("Content editor", urlString.ToString());
        }

        /// <summary>
        /// Called with the pages size changes.
        /// 
        /// </summary>
        protected void PageSize_Change()
        {
            this.PageSize = MainUtil.GetInt(Context.ClientPage.ClientRequest.Form["PageSize"], 10);
            this.Refresh();
        }

        /// <summary>
        /// Toggles the pane.
        /// 
        /// </summary>
        /// <param name="id">The id.
        ///             </param>
        protected void Pane_Toggle(string id)
        {
            Assert.ArgumentNotNull((object)id, "id");
            string id1 = "P" + Regex.Replace(id, "\\W", string.Empty);
            string @string = Registry.GetString("/Current_User/Panes/" + id1);
            if (Context.ClientPage.FindControl(id1) == null)
            {
                IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
                if (workflowProvider == null)
                    return;
                this.DisplayWorkflow(workflowProvider.GetWorkflow(id));
            }
            if (string.IsNullOrEmpty(@string) || @string == "hidden")
            {
                Registry.SetString("/Current_User/Panes/" + id1, "visible");
                Context.ClientPage.ClientResponse.SetStyle(id1, "display", string.Empty);
            }
            else
            {
                Registry.SetString("/Current_User/Panes/" + id1, "hidden");
                Context.ClientPage.ClientResponse.SetStyle(id1, "display", "none");
            }
            SheerResponse.SetReturnValue(true);
        }

        /// <summary>
        /// Previews the specified item.
        /// 
        /// </summary>
        /// <param name="id">The id.
        ///             </param><param name="language">The language.
        ///             </param><param name="version">The version.
        ///             </param>
        protected void Preview(string id, string language, string version)
        {
            Assert.ArgumentNotNull((object)id, "id");
            Assert.ArgumentNotNull((object)language, "language");
            Assert.ArgumentNotNull((object)version, "version");
            Context.ClientPage.SendMessage((object)this, "item:preview(id=" + id + ",language=" + language + ",version=" + version + ")");
        }

        /// <summary>
        /// Refreshes the page.
        /// 
        /// </summary>
        protected void Refresh()
        {
            this.Refresh((Dictionary<string, string>)null);
        }

        /// <summary>
        /// Refreshes the page.
        /// 
        /// </summary>
        /// <param name="urlArguments">The URL arguments.</param>
        protected void Refresh(Dictionary<string, string> urlArguments)
        {
            var urlString = new UrlString(WebUtil.GetRawUrl());
            urlString["reload"] = "1";
            if (urlArguments != null)
            {
                foreach (KeyValuePair<string, string> keyValuePair in urlArguments)
                    urlString[keyValuePair.Key] = keyValuePair.Value;
            }
            Context.ClientPage.ClientResponse.SetLocation(WebUtil.GetFullUrl(urlString.ToString()));
        }

        /// <summary>
        /// Comments the specified args.
        /// 
        /// </summary>
        /// <param name="args">The arguments.
        ///             </param>
        public void Comment(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull((object)args, "args");
            ID result1 = ID.Null;
            if (Context.ClientPage.ServerProperties["command"] != null)
                ID.TryParse(Context.ClientPage.ServerProperties["command"] as string, out result1);
            ItemUri itemUri = new ItemUri((Context.ClientPage.ServerProperties["id"] ?? (object)string.Empty).ToString(), Language.Parse(Context.ClientPage.ServerProperties["language"] as string), Sitecore.Data.Version.Parse(Context.ClientPage.ServerProperties["version"] as string), Context.ContentDatabase);
            bool flag = args.Parameters["ui"] != null && args.Parameters["ui"] == "1" || args.Parameters["suppresscomment"] != null && args.Parameters["suppresscomment"] == "1";
            if (!args.IsPostBack && result1 != (ID)null && !flag)
            {
                WorkflowUIHelper.DisplayCommentDialog(itemUri, result1);
                args.WaitForPostBack();
            }
            else if (args.Result != null && args.Result.Length > 2000)
            {
                Context.ClientPage.ClientResponse.ShowError(new Exception(string.Format("The comment is too long.\n\nYou have entered {0} characters.\nA comment cannot contain more than 2000 characters.", (object)args.Result.Length)));
                WorkflowUIHelper.DisplayCommentDialog(itemUri, result1);
                args.WaitForPostBack();
            }
            else
            {
                if ((args.Result == null || !(args.Result != "null") || (!(args.Result != "undefined") || !(args.Result != "cancel"))) && !flag)
                    return;
                string result2 = args.Result;
                Sitecore.Collections.StringDictionary commentFields = string.IsNullOrEmpty(result2) ? new Sitecore.Collections.StringDictionary() : WorkflowUIHelper.ExtractFieldsFromFieldEditor(result2);
                try
                {
                    IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
                    if (workflowProvider == null)
                        return;
                    IWorkflow workflow = workflowProvider.GetWorkflow(Context.ClientPage.ServerProperties["workflowid"] as string);
                    if (workflow == null)
                        return;
                    Item obj = Database.GetItem(itemUri);
                    if (obj == null)
                        return;
                    string currentPanelWorkFlowId = obj.State.GetWorkflowState().StateID;
                    WorkflowUIHelper.ExecuteCommand(obj, workflow, Context.ClientPage.ServerProperties["command"] as string, commentFields, (System.Action)(() =>
                    {
                        int itemCount = workflow.GetItemCount(currentPanelWorkFlowId);
                        if (this.PageSize > 0 && itemCount % this.PageSize == 0)
                            this.Offset[currentPanelWorkFlowId] = itemCount / this.PageSize <= 1 ? 0 : this.Offset[currentPanelWorkFlowId] - 1;
                        this.Refresh(Enumerable.ToDictionary<WorkflowState, string, string>((IEnumerable<WorkflowState>)workflow.GetStates(), (Func<WorkflowState, string>)(state => state.StateID), (Func<WorkflowState, string>)(state => this.Offset[state.StateID].ToString())));
                    }));
                }
                catch (WorkflowStateMissingException ex)
                {
                    SheerResponse.Alert("One or more items could not be processed because their workflow state does not specify the next step.");
                }
            }
        }

        /// <summary>
        /// Diffs the specified id.
        /// 
        /// </summary>
        /// <param name="id">The id.
        ///             </param><param name="language">The language.
        ///             </param><param name="version">The version.
        ///             </param>
        protected void Diff(string id, string language, string version)
        {
            Assert.ArgumentNotNull((object)id, "id");
            Assert.ArgumentNotNull((object)language, "language");
            Assert.ArgumentNotNull((object)version, "version");
            var urlString = new UrlString(UIUtil.GetUri("control:Diff"));
            urlString.Append("id", id);
            urlString.Append("la", language);
            urlString.Append("vs", version);
            urlString.Append("wb", "1");
            Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString());
        }

        #endregion

        #region Display/Build XML UI Controls

        /// <summary>
        /// Displays the workflow.
        /// 
        /// </summary>
        /// <param name="workflow">The workflow.
        ///             </param>
        protected virtual void DisplayWorkflow(IWorkflow workflow)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Context.ClientPage.ServerProperties["WorkflowID"] = (object)workflow.WorkflowID;
            var xmlControl = Resource.GetWebControl("Pane") as XmlControl;
            Error.AssertXmlControl(xmlControl, "Pane");
            this.States.Controls.Add((System.Web.UI.Control)xmlControl);
            Assert.IsNotNull((object)xmlControl, "pane");
            xmlControl["PaneID"] = (object)GetPaneID(workflow);
            xmlControl["Header"] = (object)workflow.Appearance.DisplayName;
            xmlControl["Icon"] = (object)workflow.Appearance.Icon;
            var feedUrlOptions = new FeedUrlOptions("/sitecore/shell/~/feed/workflow.aspx")
            {
                UseUrlAuthentication = true
            };
            feedUrlOptions.Parameters["wf"] = workflow.WorkflowID;
            xmlControl["FeedLink"] = (object)feedUrlOptions.ToString();
            this.DisplayStates(workflow, xmlControl);
            if (!Context.ClientPage.IsEvent)
                return;
            SheerResponse.Insert(this.States.ClientID, "append", HtmlUtil.RenderControl((System.Web.UI.Control)xmlControl));
        }

        /// <summary>
        /// Displays the states.
        /// 
        /// </summary>
        /// <param name="workflow">The workflow.
        ///             </param><param name="placeholder">The placeholder.
        ///             </param>
        protected virtual void DisplayStates(IWorkflow workflow, XmlControl placeholder)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)placeholder, "placeholder");
            this.stateNames = (NameValueCollection)null;
            var wbpItemTemplateIncludes = WorkboxPlusConfigSettings.ItemTemplateNames;
            foreach (WorkflowState state in workflow.GetStates())
            {
                if (WorkflowFilterer.FilterVisibleCommands(workflow.GetCommands(state.StateID)).Length > 0)
                {
                    var items = GetItems(state, workflow, wbpItemTemplateIncludes);
                    Assert.IsNotNull((object)items, "items is null");
                    string str1 = ShortID.Encode(workflow.WorkflowID) + "_" + ShortID.Encode(state.StateID);
                    var section1 = new Sitecore.Web.UI.HtmlControls.Section
                    {
                        ID = str1 + "_section"
                    };
                    Sitecore.Web.UI.HtmlControls.Section section2 = section1;
                    placeholder.AddControl((System.Web.UI.Control)section2);
                    int length = items.Count;
                    string str2 = string.Format("<span style=\"font-weight:normal\"> - ({0})</span>", length > 0 ? (length != 1 ? (object)string.Format("{0} {1}", (object)length, (object)Translate.Text("items")) : (object)string.Format("1 {0}", (object)Translate.Text("item"))) : (object)Translate.Text("None"));
                    section2.Header = state.DisplayName + str2;
                    section2.Icon = state.Icon;
                    if (Settings.ClientFeeds.Enabled)
                    {
                        var feedUrlOptions = new FeedUrlOptions("/sitecore/shell/~/feed/workflowstate.aspx")
                        {
                            UseUrlAuthentication = true
                        };
                        feedUrlOptions.Parameters["wf"] = workflow.WorkflowID;
                        feedUrlOptions.Parameters["st"] = state.StateID;
                        section2.FeedLink = feedUrlOptions.ToString();
                    }
                    section2.Collapsed = length <= 0;
                    var border = new Border();
                    section2.Controls.Add((System.Web.UI.Control)border);
                    border.ID = str1 + "_content";
                    this.DisplayState(workflow, state, items, (System.Web.UI.Control)border, this.Offset[state.StateID], this.PageSize);
                    this.CreateNavigator(section2, str1 + "_navigator", length, this.Offset[state.StateID]);
                }
            }
        }

        /// <summary>
        /// Displays the state.
        /// 
        /// </summary>
        /// <param name="workflow">The workflow.
        ///             </param><param name="state">The state.
        ///             </param><param name="items">The items.
        ///             </param><param name="control">The control.
        ///             </param><param name="offset">The offset.
        ///             </param><param name="pageSize">Size of the page.
        ///             </param>
        protected void DisplayState(IWorkflow workflow, WorkflowState state, List<Item> items, System.Web.UI.Control control, int offset, int pageSize)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)state, "state");
            Assert.ArgumentNotNull((object)items, "items");
            Assert.ArgumentNotNull((object)control, "control");
            int length = items.Count;

            if (length <= 0)
                return;

            int num = offset + pageSize;
            if (num > length)
                num = length;

            for (int index = offset; index < num; ++index)
            {
                var obj = items[index];
                if (obj != null)
                {
                    //if a dummy Parent Row
                    var dummyParent = DummyParentPageIdList.FirstOrDefault(x => x.Contains(obj.ID.ToString()));
                    if (dummyParent != null)
                    {
                        this.CreateItem(workflow, obj, control, true);
                    }
                    else
                    {
                    this.CreateItem(workflow, obj, control, false);
                    }
                }
            }

            //Set selected count on filter            
            var str2 = string.Format("<span style=\"font-weight:normal\"> - ({0})</span>", length > 0 ? (length != 1 ? (object)string.Format("{0} {1}", (object)length, (object)Translate.Text("items")) : (object)string.Format("1 {0}", (object)Translate.Text("item"))) : (object)Translate.Text("None"));
            var parentControl = (Sitecore.Web.UI.HtmlControls.Section)control.Parent;
            if (parentControl != null)
            {
                parentControl.Header = state.DisplayName + str2;
            }

            var border1 = new Border { Background = "#fff" };
            var border2 = border1;
            control.Controls.Add((System.Web.UI.Control)border2);
            border2.Margin = "0 5px 10px 15px";
            border2.Padding = "5px 10px";
            border2.Class = "scWorkboxToolbarButtons";
            foreach (WorkflowCommand workflowCommand in WorkflowFilterer.FilterVisibleCommands(workflow.GetCommands(state.StateID)))
            {
                //Hide Workbox Plus "(All)" commands as these are too slow and Sitecore UI doesn't give an indication that anything is happening.
                if (!WorkflowCommandUsesWithChildrenAction(workflowCommand.CommandID))
                {
                    var xmlControl1 = Resource.GetWebControl("WorkboxCommand") as XmlControl;
                    Assert.IsNotNull((object)xmlControl1, "workboxCommand is null");
                    xmlControl1["Header"] = (object)(workflowCommand.DisplayName + " " + Translate.Text("(selected)"));
                    xmlControl1["Icon"] = (object)workflowCommand.Icon;
                    xmlControl1["Command"] = (object)("workflow:sendselected(command=" + workflowCommand.CommandID + ",ws=" + state.StateID + ",wf=" + workflow.WorkflowID + ")");
                    border2.Controls.Add((System.Web.UI.Control)xmlControl1);

                    var xmlControl2 = Resource.GetWebControl("WorkboxCommand") as XmlControl;
                    Assert.IsNotNull((object)xmlControl2, "workboxCommand is null");
                    xmlControl2["Header"] = (object)(workflowCommand.DisplayName + " " + Translate.Text("(all)"));
                    xmlControl2["Icon"] = (object)workflowCommand.Icon;
                    xmlControl2["Command"] = (object)("workflow:sendall(command=" + workflowCommand.CommandID + ",ws=" + state.StateID + ",wf=" + workflow.WorkflowID + ")");
                    border2.Controls.Add((System.Web.UI.Control)xmlControl2);
                }
            }
        }

        /// <summary>
        /// Creates the item.
        /// 
        /// </summary>
        /// <param name="workflow">The workflow.
        ///             </param><param name="item">The item.
        ///             </param><param name="control">The control.
        ///             </param>
        /// <param name="isDummyParent"></param>
        private void CreateItem(IWorkflow workflow, Item item, System.Web.UI.Control control, bool isDummyParent)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)item, "item");
            Assert.ArgumentNotNull((object)control, "control");
            var workboxItem = Resource.GetWebControl("WorkboxItem") as XmlControl;
            Assert.IsNotNull((object)workboxItem, "workboxItem is null");
            control.Controls.Add((System.Web.UI.Control)workboxItem);
            var stringBuilder = new StringBuilder(" - (");
            var language = item.Language;
            stringBuilder.Append(language.CultureInfo.DisplayName);
            stringBuilder.Append(", ");
            stringBuilder.Append(Translate.Text("version"));
            stringBuilder.Append(' ');
            stringBuilder.Append(item.Version);
            stringBuilder.Append(")");
            stringBuilder.Append(' ');

            Assert.IsNotNull((object)workboxItem, "workboxItem");
            WorkflowEvent[] history = workflow.GetHistory(item);
            workboxItem["Header"] = (object)item.DisplayName;
            workboxItem["Details"] = (object)stringBuilder.ToString();
            workboxItem["Icon"] = (object)item.Appearance.Icon;
            workboxItem["ShortDescription"] = (object)item.Help.ToolTip;
            workboxItem["History"] = (object)this.GetHistory(workflow, history);
            workboxItem["LastComments"] = (object)GetLastComments(history, item);
            workboxItem["HistoryMoreID"] = (object)Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("ctl");
            workboxItem["HistoryClick"] = (object)("workflow:showhistory(id=" + item.ID.ToString() + ",la=" + item.Language.Name + ",vs=" + item.Version.Number + ",wf=" + workflow.WorkflowID + ")");
            workboxItem["PreviewClick"] = (object)("Preview(\"" + item.ID.ToString() + "\", \"" + item.Language.Name + "\", \"" + item.Version.Number + "\")");
            workboxItem["Click"] = (object)("Open(\"" + item.ID.ToString() + "\", \"" + item.Language.Name + "\", \"" + item.Version.Number + "\")");
            workboxItem["DiffClick"] = (object)("Diff(\"" + item.ID.ToString() + "\", \"" + item.Language.Name + "\", \"" + item.Version.Number + "\")");
            workboxItem["Display"] = (object)"none";
            string uniqueId = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID(string.Empty);
            workboxItem["CheckID"] = (object)("check_" + uniqueId);
            workboxItem["HiddenID"] = (object)("hidden_" + uniqueId);
            workboxItem["CheckValue"] = (object)(item.ID.ToString() + (object)"," + item.Language.Name + "," + item.Version.Number);

            #region Workbox Plus

            var isChildItem = IsChildItem(item, WorkboxPlusConfigSettings.ItemTemplateNames);

            foreach (WorkflowCommand command in WorkflowFilterer.FilterVisibleCommands(workflow.GetCommands(item), item))
            {
                //don't show commands that use "With Children" Function.
                if (isChildItem && WorkflowCommandUsesWithChildrenAction(command.CommandID))
                {
                    continue;
                }
                CreateCommand(workflow, command, item, workboxItem);
            }

            workboxItem["ItemPath"] = (object)item.Paths.FullPath;
            //todo: way to submit children from the dummy row
            //workboxItem["SubmitChildrenClick"] = (object)("workflow:submitChildren(id=" + item.ID.ToString() + ",la=" + item.Language.Name + ",vs=" + item.Version.Number + ",wf=" + workflow.WorkflowID + ")");

            //defaults
            workboxItem["PageItemMargin"] = (object)"10px 0 15px 15px";
            workboxItem["PageItemPadding"] = (object)"5px 0 10px 10px";
            workboxItem["PageChildDisplay"] = (object)"block";

            if (WorkboxPlusConfigSettings.EnablePageLevelApproval && isChildItem)
            {
                workboxItem["PageItemMargin"] = (object)"5px 0 5px 80px";
                workboxItem["PageItemPadding"] = (object)"5px 0 5px 10px";
                workboxItem["PageChildDisplay"] = (object)"none";
            }

            if (isDummyParent)
            {
                workboxItem["IsDummyParentDisplay"] = (object)"block";
                workboxItem["PageChildDisplay"] = (object)"none";
            }
            else
            {
                workboxItem["IsDummyParentDisplay"] = (object)"none";
            }
            #endregion
        }

        /// <summary>
        /// Creates the command.
        /// 
        /// </summary>
        /// <param name="workflow">The workflow.
        ///             </param><param name="command">The command.
        ///             </param><param name="item">The item.
        ///             </param><param name="workboxItem">The workbox item.
        ///             </param>
        private static void CreateCommand(IWorkflow workflow, WorkflowCommand command, Item item, XmlControl workboxItem)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)command, "command");
            Assert.ArgumentNotNull((object)item, "item");
            Assert.ArgumentNotNull((object)workboxItem, "workboxItem");
            var xmlControl = Resource.GetWebControl("WorkboxCommand") as XmlControl;
            Assert.IsNotNull((object)xmlControl, "workboxCommand is null");
            xmlControl["Header"] = (object)command.DisplayName;
            xmlControl["Icon"] = (object)command.Icon;
            var commandBuilder = new CommandBuilder("workflow:send");
            commandBuilder.Add("id", item.ID.ToString());
            commandBuilder.Add("la", item.Language.Name);
            commandBuilder.Add("vs", item.Version.ToString());
            commandBuilder.Add("command", command.CommandID);
            commandBuilder.Add("wf", workflow.WorkflowID);
            commandBuilder.Add("ui", command.HasUI);
            commandBuilder.Add("suppresscomment", command.SuppressComment);
            xmlControl["Command"] = (object)commandBuilder.ToString();
            workboxItem.AddControl((System.Web.UI.Control)xmlControl);
        }

        /// <summary>
        /// Creates the navigator.
        /// 
        /// </summary>
        /// <param name="section">The section.</param><param name="id">The id.</param><param name="count">The count.</param><param name="offset">The offset.</param>
        private void CreateNavigator(Section section, string id, int count, int offset)
        {
            Assert.ArgumentNotNull((object)section, "section");
            Assert.ArgumentNotNull((object)id, "id");
            var navigator = new Navigator { ID = id, Offset = offset, Count = count, PageSize = this.PageSize };
            section.Controls.Add((System.Web.UI.Control)navigator);
        }

        /// <summary>
        /// Updates the ribbon.
        /// 
        /// </summary>
        private void UpdateRibbon()
        {
            var ribbon1 = new Ribbon { ID = "WorkboxRibbon", CommandContext = new CommandContext() };
            Ribbon ribbon2 = ribbon1;
            Item obj = Context.Database.GetItem("/sitecore/content/Applications/Workbox/Ribbon");
            Error.AssertItemFound(obj, "/sitecore/content/Applications/Workbox/Ribbon");
            ribbon2.CommandContext.RibbonSourceUri = obj.Uri;
            ribbon2.CommandContext.CustomData = (object)(this.IsReload ? true : false);
            this.RibbonPanel.Controls.Add((System.Web.UI.Control)ribbon2);
        }

        /// <summary>
        /// Wires the up navigators.
        /// 
        /// </summary>
        /// <param name="control">The control.
        ///             </param>
        private void WireUpNavigators(System.Web.UI.Control control)
        {
            foreach (System.Web.UI.Control control1 in control.Controls)
            {
                var navigator = control1 as Navigator;
                if (navigator != null)
                {
                    navigator.Jump += new Navigator.NavigatorDelegate(this.Jump);
                    navigator.Previous += new Navigator.NavigatorDelegate(this.Jump);
                    navigator.Next += new Navigator.NavigatorDelegate(this.Jump);
                }
                this.WireUpNavigators(control1);
            }
        }

        #endregion

        #region Get Methods

        /// <summary>
        /// Gets the items.
        /// 
        /// </summary>
        /// <param name="state">The state.
        ///             </param><param name="workflow">The workflow.
        ///             </param>
        /// <param name="wbpItemTemplateNames"></param>
        /// <returns>
        /// Array of item.
        /// 
        /// </returns>
        private static List<Item> GetItems(WorkflowState state, IWorkflow workflow, IReadOnlyDictionary<Guid, string> wbpItemTemplateNames)
        {
            Assert.ArgumentNotNull((object)state, "state");
            Assert.ArgumentNotNull((object)workflow, "workflow");

            DataUri[] items = workflow.GetItems(state.StateID);
            var sortedItems = new List<Item>();
            if (items == null || items.Length == 0)
            {
                return sortedItems;
            }

            foreach (DataUri dataUri in items)
            {
                var obj = Context.ContentDatabase.Items[dataUri];

                //Add Dummy Parent if needed
                if (UserCanViewItem(obj.Parent, state, true) && IsChildItem(obj, wbpItemTemplateNames))
                {
                    var dummyParent = AddDummyParent(obj.Parent, items, wbpItemTemplateNames);
                    if (!sortedItems.Any(x => x.ID.Equals(obj.Parent.ID)))
                    {
                        sortedItems.Add(obj.Parent);
                    }

                    if (dummyParent != null && !sortedItems.Any(x => x.ID.Equals(dummyParent.ID)))
                    {
                        sortedItems.Add(dummyParent);
                    }
                }

                //Add Item
                if (UserCanViewItem(obj, state, false))
                {
                    sortedItems.Add(obj);
                }
            }
            return sortedItems.OrderBy(x => x.Paths.FullPath).ToList();
        }

        /// <summary>
        /// Get the comments from the latest workflow event
        /// 
        /// </summary>
        /// <param name="events">The workflow events to process</param><param name="item">The item to get the comment for</param>
        /// <returns>
        /// The last comments
        /// </returns>
        private static string GetLastComments(WorkflowEvent[] events, Item item)
        {
            Assert.ArgumentNotNull((object)events, "events");
            var length = events.Length;
            return length > 0
                ? GetWorkflowCommentsDisplayPipeline.Run(events[length - 1], item)
                : string.Empty;
        }

        /// <summary>
        /// Gets the pane ID.
        /// 
        /// </summary>
        /// <param name="workflow">The workflow.
        ///             </param>
        /// <returns>
        /// The get pane id.
        /// 
        /// </returns>
        private static string GetPaneID(IWorkflow workflow)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            return "P" + Regex.Replace(workflow.WorkflowID, "\\W", string.Empty);
        }

        /// <summary>
        /// Gets the history.
        /// 
        /// </summary>
        /// <param name="workflow">The workflow.
        ///             </param><param name="events">The workflow history for the item
        ///             </param>
        /// <returns>
        /// The get history.
        /// 
        /// </returns>
        private string GetHistory(IWorkflow workflow, WorkflowEvent[] events)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)events, "events");
            string str;
            if (events.Length > 0)
            {
                WorkflowEvent workflowEvent = events[events.Length - 1];
                string text = workflowEvent.User;
                string name = Context.Domain.Name;
                if (text.StartsWith(name + "\\", StringComparison.OrdinalIgnoreCase))
                    text = StringUtil.Mid(text, name.Length + 1);
                str = string.Format(Translate.Text("{0} changed from <b>{1}</b> to <b>{2}</b> on {3}."), (object)StringUtil.GetString(new string[2]
                {
                  text,
                  Translate.Text("Unknown")
                }), (object)this.GetStateName(workflow, workflowEvent.OldState), (object)this.GetStateName(workflow, workflowEvent.NewState), (object)DateUtil.FormatDateTime(DateUtil.ToServerTime(workflowEvent.Date), "D", Context.User.Profile.Culture));
            }
            else
                str = Translate.Text("No changes have been made.");
            return str;
        }

        /// <summary>
        /// Gets the name of the state.
        /// 
        /// </summary>
        /// <param name="workflow">The workflow.
        ///             </param><param name="stateID">The state ID.
        ///             </param>
        /// <returns>
        /// The get state name.
        /// 
        /// </returns>
        private string GetStateName(IWorkflow workflow, string stateID)
        {
            Assert.ArgumentNotNull((object)workflow, "workflow");
            Assert.ArgumentNotNull((object)stateID, "stateID");
            if (this.stateNames == null)
            {
                this.stateNames = new NameValueCollection();
                foreach (WorkflowState workflowState in workflow.GetStates())
                    this.stateNames.Add(workflowState.StateID, workflowState.DisplayName);
            }
            return StringUtil.GetString(new string[2] { this.stateNames[stateID], "?" });
        }

        //Last User to submit in Workflow
        private static string GetLastSubmitterUserName(Item contentItem)
        {
            string result = string.Empty;
            var contentWorkflow = contentItem.Database.WorkflowProvider.GetWorkflow(contentItem);
            var contentHistory = contentWorkflow.GetHistory(contentItem);

            if (contentHistory.Length > 0)
            {
                var lastUser = contentHistory[contentHistory.Length - 1].User;
                var user = User.FromName(lastUser, false);
                var userProfile = user.Profile;
                result = userProfile.UserName;
            }
            else
            {
                //no history use Admin
                result = @"sitecore\admin";
            }
            return result;
        }

        #endregion

        #region UI Messages
        /// <summary>
        /// Jumps the specified sender.
        /// 
        /// </summary>
        /// <param name="sender">The sender.
        ///             </param><param name="message">The message.
        ///             </param><param name="offset">The offset.
        ///             </param>
        private void Jump(object sender, Message message, int offset)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull((object)message, "message");
            string control = Context.ClientPage.ClientRequest.Control;
            string workflowID = ShortID.Decode(control.Substring(0, 32));
            string stateID = ShortID.Decode(control.Substring(33, 32));
            string str = control.Substring(0, 65);
            this.Offset[stateID] = offset;
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            Assert.IsNotNull((object)workflowProvider, "Workflow provider for database \"" + Context.ContentDatabase.Name + "\" not found.");
            IWorkflow workflow = workflowProvider.GetWorkflow(workflowID);
            Error.Assert(workflow != null, "Workflow \"" + workflowID + "\" not found.");
            Assert.IsNotNull((object)workflow, "workflow");
            WorkflowState state = workflow.GetState(stateID);
            Assert.IsNotNull((object)state, "Workflow state \"" + stateID + "\" not found.");
            var border1 = new Border { ID = str + "_content" };
            Border border2 = border1;
            var items = GetItems(state, workflow, WorkboxPlusConfigSettings.ItemTemplateNames);
            this.DisplayState(workflow, state, items ?? new List<Item>(), (System.Web.UI.Control)border2, offset, this.PageSize);
            Context.ClientPage.ClientResponse.SetOuterHtml(str + "_content", (System.Web.UI.Control)border2);
        }

        /// <summary>
        /// Handles the message.
        /// 
        /// </summary>
        /// <param name="message">The message.
        ///             </param>
        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            switch (message.Name)
            {
                case "workflow:send":
                    this.Send(message);
                    return;
                case "workflow:sendselected":
                    this.SendSelected(message);
                    return;
                case "workflow:sendall":
                    this.SendAll(message);
                    return;
                case "window:close":
                    Windows.Close();
                    return;
                case "workflow:showhistory":
                    ShowHistory(message, Context.ClientPage.ClientRequest.Control);
                    return;
                case "workflow:submitChildren":
                    this.SendChildren(message);
                    return;
                case "workbox:hide":
                    Context.ClientPage.SendMessage((object)this, "pane:hide(id=" + message["id"] + ")");
                    Context.ClientPage.ClientResponse.SetAttribute("Check_Check_" + message["id"], "checked", "false");
                    break;
                case "pane:hidden":
                    Context.ClientPage.ClientResponse.SetAttribute("Check_Check_" + message["paneid"], "checked", "false");
                    break;
                case "workbox:show":
                    Context.ClientPage.SendMessage((object)this, "pane:show(id=" + message["id"] + ")");
                    Context.ClientPage.ClientResponse.SetAttribute("Check_Check_" + message["id"], "checked", "true");
                    break;
                case "pane:showed":
                    Context.ClientPage.ClientResponse.SetAttribute("Check_Check_" + message["paneid"], "checked", "true");
                    break;
            }
            base.HandleMessage(message);
            string index = message["id"];
            if (string.IsNullOrEmpty(index))
                return;
            string string1 = StringUtil.GetString(new string[1]
              {
                message["language"]
              });
            string string2 = StringUtil.GetString(new string[1]
              {
                message["version"]
              });
            Item obj = Context.ContentDatabase.Items[index, Language.Parse(string1), Sitecore.Data.Version.Parse(string2)];
            if (obj == null)
                return;
            Dispatcher.Dispatch(message, obj);
        }

        /// <summary>
        /// Sends the specified message.
        /// 
        /// </summary>
        /// <param name="message">The message.
        ///             </param>
        private void Send(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;
            string workflowID = message["wf"];
            if (workflowProvider.GetWorkflow(workflowID) == null || Context.ContentDatabase.Items[message["id"], Language.Parse(message["la"]), Sitecore.Data.Version.Parse(message["vs"])] == null)
                return;
            Context.ClientPage.ServerProperties["id"] = (object)message["id"];
            Context.ClientPage.ServerProperties["language"] = (object)message["la"];
            Context.ClientPage.ServerProperties["version"] = (object)message["vs"];
            Context.ClientPage.ServerProperties["command"] = (object)message["command"];
            Context.ClientPage.ServerProperties["workflowid"] = (object)workflowID;
            Context.ClientPage.Start((object)this, "Comment",
                new NameValueCollection
                  {
                    {
                      "ui",
                      message["ui"]
                    },
                    {
                      "suppresscomment",
                      message["suppresscomment"]
                    }
                  });
        }

        /// <summary>
        /// Sends all.
        /// 
        /// </summary>
        /// <param name="message">The message.
        ///             </param>
        private void SendAll(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;
            string workflowID = message["wf"];
            string stateID = message["ws"];
            IWorkflow workflow = workflowProvider.GetWorkflow(workflowID);
            if (workflow == null)
                return;
            WorkflowState state = workflow.GetState(stateID);
            var items = GetItems(state, workflow, WorkboxPlusConfigSettings.ItemTemplateNames);
            Assert.IsNotNull((object)items, "uris is null");
            bool flag = false;
            foreach (Item obj in items)
            {
                if (obj == null)
                    continue;

                try
                {
                    WorkflowUIHelper.ExecuteCommand(obj, workflow, message["command"], null, Refresh);
                }
                catch (WorkflowStateMissingException ex)
                {
                    flag = true;
                }
            }
            if (!flag)
                return;
            SheerResponse.Alert("One or more items could not be processed because their workflow state does not specify the next step.");
        }

        /// <summary>
        /// Sends the selected.
        /// 
        /// </summary>
        /// <param name="message">The message.
        ///             </param>
        private void SendSelected(Message message)
        {
            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;

            string workflowID = message["wf"];
            string str1 = message["ws"];
            IWorkflow workflow = workflowProvider.GetWorkflow(workflowID);
            if (workflow == null)
                return;

            int num = 0;
            bool flag = false;
            foreach (string str2 in Context.ClientPage.ClientRequest.Form.Keys)
            {
                if (str2 != null && str2.StartsWith("check_", StringComparison.InvariantCulture))
                {
                    string[] strArray = Context.ClientPage.ClientRequest.Form["hidden_" + str2.Substring(6)].Split(',');
                    Item obj = Context.ContentDatabase.Items[strArray[0], Language.Parse(strArray[1]), Sitecore.Data.Version.Parse(strArray[2])];
                    if (obj != null)
                    {
                        WorkflowState state = workflow.GetState(obj);
                        if (state.StateID == str1)
                        {
                            try
                            {
                                workflow.Execute(message["command"], obj, state.DisplayName, true);
                            }
                            catch (WorkflowStateMissingException ex)
                            {
                                flag = true;
                            }
                            ++num;
                        }
                    }
                }
            }

            if (flag)
                SheerResponse.Alert("One or more items could not be processed because their workflow state does not specify the next step.");
            if (num == 0)
                Context.ClientPage.ClientResponse.Alert("There are no selected items.");
            else
                this.Refresh();
        }

        /// <summary>
        /// Shows the history.
        /// 
        /// </summary>
        /// <param name="message">The message.
        ///             </param><param name="control">The control.
        ///             </param>
        private static void ShowHistory(Message message, string control)
        {
            Assert.ArgumentNotNull((object)message, "message");
            Assert.ArgumentNotNull((object)control, "control");
            var xmlControl = Resource.GetWebControl("WorkboxHistory") as XmlControl;
            Assert.IsNotNull((object)xmlControl, "history is null");
            xmlControl["ItemID"] = (object)message["id"];
            xmlControl["Language"] = (object)message["la"];
            xmlControl["Version"] = (object)message["vs"];
            xmlControl["WorkflowID"] = (object)message["wf"];
            Context.ClientPage.ClientResponse.ShowPopup(control, "below", (System.Web.UI.Control)xmlControl);
        }

        /// <summary>
        /// Toggles the User Filter.
        /// 
        /// </summary>
        protected void Apply_User_Filter()
        {
            var isChecked = !Registry.GetBool("/Current_User/IsUserFiltered");
            Registry.SetBool("/Current_User/IsUserFiltered", isChecked);
            Refresh();
            SheerResponse.SetReturnValue(true);
        }

        /// <summary>
        /// Sends Children of this item.
        /// 
        /// </summary>
        /// <param name="message">The message.
        ///             </param>
        private void SendChildren(Message message)
        {
            throw new NotImplementedException();

            Assert.ArgumentNotNull((object)message, "message");
            IWorkflowProvider workflowProvider = Context.ContentDatabase.WorkflowProvider;
            if (workflowProvider == null)
                return;

            string workflowID = message["wf"];
            string stateID = message["ws"];
            IWorkflow workflow = workflowProvider.GetWorkflow(workflowID);
            if (workflow == null)
                return;

            var state = workflow.GetState(stateID);
            //todo: execute submit for all children
        }

        #endregion
    }

}
