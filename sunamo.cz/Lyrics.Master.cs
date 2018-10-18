using System;
using System.Web.UI.WebControls;
using System.IO;
using sunamo;
using webforms;

public partial class LyricsMaster : SunamoMasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        body.Attributes.Add("data-user-id", ((SunamoPage)Page).idLoginedUser.ToString());
        if (txtSearchText.Value != "")
        {
            string red = "http://" + Request.Url.Host + "/Lyrics/Search/" + UH.UrlEncode(txtSearchText.Value.Replace(".", "-")).Replace('+', ' ');
            ((SunamoPage)Page).WriteToDebugWithTime(red);
        }

        footer.InnerHtml = SunamoPageHelper.GetFooterHtml(MySites.Lyrics);

        imgColorPiano.Src = web.UH.GetWebUri(this.Page, "css/Lyr/colorpiano.png");
        aHome.HRef = web.UH.GetWebUri(this.Page, "Lyrics/Home");

        MasterPageHelper.WriteGeneralCode(this.Page, false, true);
        SunamoCzMetroUIHelper.SetHtmlMetroUpperBarV3(this, horniLista, MySites.Lyrics);

        btnLogOut.ServerClick += new EventHandler(btnLogOut_ServerClick);
        btnLogIn.ServerClick += new EventHandler(btnLogIn_ServerClick);

        LoginedUser pu = SessionManager.GetLoginedUser(Page);
        if (pu.login != "")
        {
            loginForm.Visible = false;
            logined.Visible = true;
        }
        else
        {
            loginForm.Visible = true;
            logined.Visible = false;
        }
    }

    void btnLogIn_ServerClick(object sender, EventArgs e)
    {
        LoginResponse lr = Logins.LoginCommonAllPages((SunamoPage)this.Page, login.Text, heslo.Text, true, true, Request.RawUrl);
        switch (lr.type)
        {
            case LoginResponseType.Redirect:
                ((SunamoPage)Page).WriteToDebugWithTime(lr.value);
                break;
            case LoginResponseType.Warning:
                JavaScriptInjection.alert(lr.value, this.Page);
                break;
            case LoginResponseType.Alert:
                JavaScriptInjection.alert(lr.value, this.Page);
                break;
            default:
                throw new Exception("Neimplementovaná větěv v btnLogIn_ServerClick");
        }
    }

    void btnLogOut_ServerClick(object sender, EventArgs e)
    {
        ((SunamoPage)Page).WriteToDebugWithTime("/Me/Logout.aspx?ReturnUrl=" + UH.UrlEncode(this.Request.RawUrl));
    }

    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);

        login.TextMode = TextBoxMode.SingleLine;
        heslo.TextMode = TextBoxMode.Password;
    }

    protected override void OnInit(EventArgs e)
    {
        base.OnInit(e);
        string path = Server.MapPath(SunamoRoutePage.GetRightUp(this.Page.Request) + "img/Lyrics/cds");
        string[] images = Directory.GetFiles(path);
        Random rnd = new Random();
        cdpic.Src = "img/Lyrics/cds/" + Path.GetFileName(images[rnd.Next(0, images.Length - 1)]);
    }
}
