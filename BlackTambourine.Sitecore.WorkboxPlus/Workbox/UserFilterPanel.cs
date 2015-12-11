using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Web.UI.WebControls;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.WebControls.Ribbons;
using System.Web.UI;

namespace BlackTambourine.Sitecore8.WorkboxPlus.Workbox
{
    public class UserFilterPanel : RibbonPanel
    {
        public override void Render(HtmlTextWriter output, Ribbon ribbon, Item button, CommandContext context)
        {
            Assert.ArgumentNotNull((object)output, "output");
            Assert.ArgumentNotNull((object)ribbon, "ribbon");
            Assert.ArgumentNotNull((object)button, "button");
            Assert.ArgumentNotNull((object)context, "context");

            const string controlName = "WF_Plus_User_Filter";
            var isChecked = Registry.GetBool("/Current_User/IsUserFiltered");

            output.Write("<div class=\"scRibbonToolbarPanel\">");
            ribbon.BeginSmallButtons(output);
            var smallCheckButton = new SmallCheckButton
            {
                Header = Translate.Text("Only My Draft/Rejected Items"),
                Checked = isChecked,
                Command = "Apply_User_Filter()",
                ID = "Check_" + controlName
            };
            ribbon.RenderSmallButton(output, smallCheckButton);
            ribbon.EndSmallButtons(output);
            output.Write("</div>");
        }
    }
}
