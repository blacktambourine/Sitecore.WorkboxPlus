using System;
using System.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Web;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;
using BlackTambourine.Sitecore8.WorkboxPlus.Config;

namespace BlackTambourine.Sitecore8.WorkboxPlus.Actions
{
    public class ExecuteCommandOnItemAndChildrenAction
    {
        /// <summary>
        /// Return WorkboxPlus.config settings
        /// </summary>
        /// <returns></returns>
        private readonly WorkboxPlusConfig _workboxPlusConfigSettings = Factory.CreateObject("WorkboxPlus/configuration", true) as WorkboxPlusConfig;

        /// <summary>
        /// Get all children of the item (if applicable) and Run the Action specified in the parameters e.g. a Submit single item command
        /// </summary>
        /// <param name="args"></param>
        public void Process(WorkflowPipelineArgs args)
        {
            try
            {
                if (args == null)
                {
                    throw new Exception("WorkflowPipelineArgs is null");
                }

                ProcessorItem processorItem = args.ProcessorItem;
                var comments = args.CommentFields.LastOrDefault().Value;

                if (processorItem != null)
                {
                    var actionItem = processorItem.InnerItem;
                    var contentItem = args.DataItem;

                    var parameters = WebUtil.ParseUrlParameters(actionItem["parameters"]);
                    var commandName = parameters[0];
                    if (string.IsNullOrEmpty(commandName))
                    {
                        throw new Exception("Command Name is not specified in Parameters");
                    }

                    //if this item is a child item, exit this command
                    var isChildItem = _workboxPlusConfigSettings.ItemTemplateNames.ContainsKey(contentItem.TemplateID.ToGuid());
                    if (isChildItem)
                    {
                        return;
                    }

                    //apply this action to this item and all relevant children (i.e. template type specified in config and currently in the same workflow state as the parent)
                    var parentState = contentItem[FieldIDs.WorkflowState];
                    var parentWorkflow = contentItem[FieldIDs.Workflow];
                    ExecuteCommandForItem(contentItem, commandName, comments);
                    foreach (Item child in contentItem.Children)
                    {
                        var childState = child[FieldIDs.WorkflowState];
                        var childWorkflow = child[FieldIDs.Workflow];

                        if (_workboxPlusConfigSettings.EnablePageLevelApproval &&
                            _workboxPlusConfigSettings.ItemTemplateNames.ContainsKey(child.TemplateID.ToGuid()) &&
                            parentState == childState &&
                            parentWorkflow == childWorkflow)
                        {
                            ExecuteCommandForItem(child, commandName, comments);
                        }
                    }
                }

                //ignore the next state of the Command
                args.AbortPipeline();
            }
            catch (Exception ex)
            {
                //log exception, but continue to send item through workflow
                Log.Error("Submit with Children error", ex, this);
            }
        }

        /// <summary>
        /// Execute the Command on this Item
        /// </summary>
        /// <param name="contentItem"></param>
        /// <param name="commandName"></param>
        /// <param name="comments"></param>
        private static void ExecuteCommandForItem(Item contentItem, string commandName, string comments)
        {
            IWorkflow contentWorkflow = contentItem.Database.WorkflowProvider.GetWorkflow(contentItem);
            if (contentWorkflow == null)
            {
                throw new Exception(string.Format("No workflow assigned to item {0}", contentItem.ID));
            }

            var command = contentWorkflow.GetCommands(contentItem[FieldIDs.WorkflowState]).FirstOrDefault(c => c.DisplayName == commandName);
            if (command == null)
            {
                throw new Exception(string.Format("workflow command {0} not found for item {1}", commandName, contentItem.ID));
            }
            contentWorkflow.Execute(command.CommandID, contentItem, comments, false, new object[0]);
        }

    }
}
