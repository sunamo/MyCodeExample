using Lastfm.Services;
using sunamo.Values;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using webforms;

namespace sunamo.cz.Lyrics
{
    public partial class Album : LyricsPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            base.errors = errors;
            WarningEvent += Album_WarningEvent;
            
            var rd = RouteData;
            string artistUri = "";
            ResultCheckWebArgument rArtistUri = RoutePageHelper.CheckStringArgument(rd, "Artist", out artistUri);
            if (rArtistUri == ResultCheckWebArgument.AllOk)
            {
                string albumUri = "";
                ResultCheckWebArgument rAlbumUri = RoutePageHelper.CheckStringArgument(rd, "Album", out albumUri);
                if (rAlbumUri == ResultCheckWebArgument.AllOk)
                {
                    int idArtist = LyricsCells.IDOfArtist_Uri(artistUri);
                    if (idArtist != int.MaxValue)
                    {
                        short idAlbum = LyricsHelper.IDOfAlbum_Uri(albumUri, idArtist);
                        if (idAlbum != short.MaxValue)
                        {
                            string nameAlbum = LyricsCells.NameOfAlbum(idAlbum);
                            string nameInterpret = LyricsCells.NameOfArtist(idArtist);
                            string uriArtist = LyricsCells.UriOfArtist(idArtist);
                            string uriAlbum = LyricsCells.UriOfAlbum(idAlbum);

                            bcHome.HRef = web.UH.GetWebUri2(this, sa);
                            bcArtist.HRef = LyricsUri.Artist(this, uriArtist);
                            bcArtist.InnerHtml = nameInterpret;
                            bcAlbum.HRef = LyricsUri.Album(this, uriArtist, uriAlbum);
                            bcAlbum.InnerHtml = nameAlbum;
                            string title = "Album " + nameAlbum + " interpreta " + nameInterpret;
                            h1.InnerHtml = title;
                            Title = title;
                            CreateTitle();

                            scripts.Insert(0, "ts/Lyrics/Album.js");
                            scripts.Insert(0, JavaScriptPaths.JustifiedGallery);
                            styles.Add(StyleSheetPaths.JustifiedGallery);

                            List<string> saAnchors = new List<string>();
                            List<string> saImages = new List<string>();
                            List<string> saAlts = new List<string>();

                            List<short> allAlbums = LyricsHelper.GetAlbumsOfArtist(idArtist);
                            List<string> idBadges = new List<string>();
                            List<string> viewCountBadges = new List<string>();
                            int serieBadges = 0;

                            foreach (short item in allAlbums)
                            {
                                if (item != idAlbum)
                                {
                                    string path = LyricsHelper.GetPathOfAlbumImage(item, AlbumImageSize.ExtraLarge);
                                    if (File.Exists(path))
                                    {
                                        string name = LyricsCells.NameOfAlbum(item);
                                        saAnchors.Add(LyricsUri.Album(this, uriArtist, LyricsCells.UriOfAlbum(item)));
                                        saImages.Add(LyricsHelper.GetUriOfAlbumImage(Request, item, AlbumImageSize.ExtraLarge));
                                        saAlts.Add(name);
                                    }
                                }
                            }
                            if (saAnchors.Count != 0)
                            {
                                divJustifiedAlbums.InnerHtml = JustifiedGalleryHelper.GetInnerHtml(saAnchors, saImages, saAlts);
                            }
                            else
                            {
                                divJustifiedAlbums.InnerHtml = HtmlGenerator2.Italic("Žádné další album umělce nebylo nalezeno.");
                            }

                            List<int> addedYtVideosToJsArray = new List<int>();
                            StringBuilder fillingJavaScriptArray = new StringBuilder();
                            string nameJsArray = "videos";

                            bool atLeastOneSongsInAlbum = false;
                            DataTable dtSongsInAlbum = MSStoredProceduresI.ci.SelectDataTableLimitLastRows(Tables.Lyr_Song, int.MaxValue, "ID,Name,Views", "Views", CA.ToArrayT<AB>(AB.Get("IDArtist", idArtist), AB.Get("IDAlbum", idAlbum)), null, null, null);
                            HtmlGenerator hgSongsInAlbum = new HtmlGenerator();

                            PageSnippet pageSnippet = null;
                            List<string> songsInAlbumList = new List<string>();


                            if (dtSongsInAlbum.Rows.Count != 0)
                            {
                                atLeastOneSongsInAlbum = true;
                                
                                int countSongsInAlbum = dtSongsInAlbum.Rows.Count;
                                List<string> odkazyPhoto = new List<string>(countSongsInAlbum);
                                List<string> odkazyText = new List<string>(countSongsInAlbum);
                                List<string> innerHtmlText = new List<string>(countSongsInAlbum);
                                List<string> srcPhoto = new List<string>(countSongsInAlbum);
                                foreach (DataRow item in dtSongsInAlbum.Rows)
                                {
                                    object[] o = item.ItemArray;
                                    int idVideo = MSTableRowParse.GetInt(o, 0);
                                    string nameSong = MSTableRowParse.GetString(o, 1);
                                    ulong views = NormalizeNumbers.NormalizeInt(MSTableRowParse.GetInt(o, 2));

                                    if (!addedYtVideosToJsArray.Contains(idVideo))
                                    {
                                        string image1 = "";
                                        fillingJavaScriptArray.Append(YouTubeThumbnailLyrics.ci.AllFilesRelativeUriJavascriptArray(nameJsArray, idVideo, out image1));
                                        addedYtVideosToJsArray.Add(idVideo);
                                        if (pageSnippet == null)
                                        {
                                            if (image1 != "")
                                            {
                                                pageSnippet = new PageSnippet();
                                                pageSnippet.title = Title;
                                                pageSnippet.image = Consts.HttpWwwCzSlash + image1;
                                            }
                                        }
                                    }

                                    songsInAlbumList.Add(nameSong);
                                    srcPhoto.Add(idVideo.ToString());
                                    string odkaz = LyricsHelper.UriOfSong(this, nameInterpret, nameSong);
                                    odkazyPhoto.Add(odkaz);
                                    odkazyText.Add(odkaz);
                                    innerHtmlText.Add(" " + LyricsHelper.GetArtistAndTitle(nameInterpret, nameSong));

                                    string idb = serieBadges.ToString();
                                    //translateTl.idBadges.Add(idb);
                                    idBadges.Add(idb);
                                    viewCountBadges.Add(views.ToString());
                                    serieBadges++;
                                }
                                HtmlGenerator2.TopListWithImages(hgSongsInAlbum, 130, 97, web.UH.GetWebUri(this, "img/Lyrics/NoThumbnail.png"), odkazyPhoto, odkazyText, innerHtmlText, srcPhoto, idBadges, nameJsArray);

                                if (pageSnippet != null)
                                {
                                    pageSnippet.description = "Songy v albu: " + SH.JoinString(", ", songsInAlbumList);
                                    OpenGraphHelper.InsertBasicToPageHeader(this, pageSnippet, MySites.Lyrics);
                                    SchemaOrgHelper.InsertBasicToPageHeader(this, pageSnippet, MySites.Lyrics);
                                }

                                JavaScriptInjection.InjectInternalScript(this, JavaScriptGenerator2.AlternatingImages(nameJsArray, nameJsArray, true));
                                JavaScriptInjection.InjectInternalScript(this, fillingJavaScriptArray.ToString());
                                JavaScriptInjection.InjectInternalScript(this, JavaScriptGenerator2.JsForIosBadge("blue", idBadges, viewCountBadges));
                            }

                            if (atLeastOneSongsInAlbum)
                            {
                                divSongsInAlbum.InnerHtml = hgSongsInAlbum.ToString();
                            }
                            else
                            {
                                divSongsInAlbum.InnerHtml = HtmlGenerator2.Italic("Žádný song z tohoto alba nebyl nalezen.");
                            }
                        }
                        else
                        {
                            Warning("Album bylo nalezeno v Uri ale již ne v databázi.");
                            Include(styles, scripts, null, null);
                            return;
                        }
                    }
                    else
                    {
                        Warning("Umělec byl nalezen v Uri ale již ne v databázi.");
                        Include(styles, scripts, null, null);
                        return;
                    }
                }
                else
                {
                    Warning(PageHelperBase.GetErrorDescriptionStringURI(rAlbumUri, "Album"));
                    Include(styles, scripts, null, null);
                    return;
                }
            }
            else
            {
                Warning(PageHelperBase.GetErrorDescriptionStringURI(rArtistUri, "Artist"));
                Include(styles, scripts, null, null);
                return;
            }
            Include(styles, scripts, null, null);
        }

        private void Album_WarningEvent()
        {
            divContent.Visible = false;
        }
    }
}
