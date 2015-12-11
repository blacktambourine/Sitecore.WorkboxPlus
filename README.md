# Sitecore.WorkboxPlus v1.0
Enhanced Workbox for Sitecore

This extends the default Sitecore 8 Workbox to include the following:

- A user filter to only show items that belong to the current user (for the Workflow States specified in the config file e.g. Draft only).
- grouping of items at "Page Level" (this can be turned off in the config file using the "EnablePageLevelApproval" setting)
- A workflow action to submit a page and its children (ExecuteCommandOnItemAndChildrenAction). 
This has a parameter called "CommandName"; this is needed to specify which workflow command to run on the item and its children (e.g. CommandName=Submit).


Using the config file you can specify which item Templates to group under their parent (or "Page Level"). The ExecuteCommandOnItemAndChildrenAction only looks at these items.

i.e. Page level means that if you have several datasource items under a “Page” item in sitecore, they can be grouped together in the UI and submitted together using this new Workflow Action.