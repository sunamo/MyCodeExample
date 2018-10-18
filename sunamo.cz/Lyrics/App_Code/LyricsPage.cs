using System;
using System.Collections.Generic;

public class LyricsPage : SunamoPage, IRoutePage
{
    protected List<string> styles = new List<string>(new string[] { StyleSheetPaths.IosBadge, "css/Lyr.css", "Shared/css/Shared.css", "Content/metro-icons.css", "Content/metro.css" });
    protected List<string> scripts = new List<string>(new string[] { JavaScriptPaths.IosBadge, "ts/Lyrics/Shared.js", "ts/swf.js", JavaScriptPaths.MetroUi, "js/jquery.min.js" });

    protected override void OnLoad(System.EventArgs e)
    {
        base.OnLoad(e);
        MasterPageHelper.AddFavicon(this, MySites.Lyrics);
    }

    protected override void OnPreInit(EventArgs e)
    {
        base.OnPreInit(e);
        
        sa = MySites.Lyrics;
    }

    public string GetRightUpRootRoute()
    {
        return SunamoRoutePage.GetRightUpRoot(this.Request);
    }
}
