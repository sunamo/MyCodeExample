using Lastfm.Services;
using sunamo.Values;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Web.UI.HtmlControls;
using web;
using webforms;

namespace sunamo.cz.Ggdag
{
    /// <summary>
    /// Kombinovaná stránka pro zobrazení videa - slouží k zobrazení anglicko-českých, instrumentálních a českých skladeb.
    /// </summary>
    public partial class ShowSong : LyricsPage
    {
        public ShowSong()
            : base()
        {

        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Langs l = GetLang();
            base.errors = errors;

            FillIDUsers();

            var rd = this.RouteData;

            string song = "";
            int idSong = int.MaxValue;
            string unLoginedUser = "";
            // Dictionary to all user name used in loaded page due to avoid asking DB again with same Select
            Dictionary<int, string> resolvedUserNames = new Dictionary<int, string>();
            if (idLoginedUser != -1)
            {
                unLoginedUser = GeneralCells.LoginOfUser(idLoginedUser);
                if (!resolvedUserNames.ContainsKey(idLoginedUser))
                {
                    resolvedUserNames.Add(idLoginedUser, unLoginedUser);
                }
            }

            #region Checking if uri-passed original and translated song is present
            ResultCheckWebArgument rSong = RoutePageHelper.CheckStringArgument(rd, "song", out song);

            if (rSong == ResultCheckWebArgument.AllOk)
            {
                idSong = MSStoredProceduresI.ci.SelectCellDataTableIntOneRow(true, Tables.Lyr_Song, "ID", "Uri", song);
                if (idSong == int.MaxValue)
                {
                    divContent.Visible = false;
                    Warning("Požadovaný song " + song + "nebyl v databázi nalezen. ");
                    Include(styles, scripts, null, null);
                    return;
                }
            }

            int idUserOriginal = -1;
            string originalLogin = "";
            ResultCheckWebArgument rOriginal = RoutePageHelper.CheckStringArgument(rd, "Original", out originalLogin);

            if (rOriginal == ResultCheckWebArgument.AllOk)
            {
                idUserOriginal = GeneralCells.IDOfUser_Login(originalLogin);
                if (idUserOriginal == -1)
                {
                    divContent.Visible = false;
                    Warning("Požadovaný song byl nalezen, ale mezi originálními texty písně nebyl nalezen uživatel " + originalLogin + " specifikovaný v URI");
                    Include(styles, scripts, null, null);
                    return;
                }
                else
                {
                    if (!resolvedUserNames.ContainsKey(idUserOriginal))
                    {
                        resolvedUserNames.Add(idUserOriginal, originalLogin);
                    }
                }
            }

            int idUserTranslated = -1;
            string translated = "";
            ResultCheckWebArgument rTranslated = RoutePageHelper.CheckStringArgument(rd, "Translated", out translated);

            if (rTranslated == ResultCheckWebArgument.AllOk)
            {
                idUserTranslated = GeneralCells.IDOfUser_Login(translated);
                if (idUserTranslated == -1)
                {
                    divContent.Visible = false;
                    Warning("Požadovaný song byl nalezen, ale mezi přeloženými texty písně nebyl nalezen uživatel " + translated + " specifikovaný v URI");
                    Include(styles, scripts, null, null);
                    return;
                }
                else
                {
                    if (!resolvedUserNames.ContainsKey(idUserTranslated))
                    {
                        resolvedUserNames.Add(idUserTranslated, translated);
                    }
                }
            }
            #endregion

            if (rSong == ResultCheckWebArgument.AllOk)
            {
                short idAlbum = LyricsCells.IDAlbumOfSong(idSong);

                string nameAlbum = LyricsCells.NameOfAlbum(idAlbum);

                #region Showing number of views
                DayViewManager.IncrementOrInsertNew(this, ViewTable.Lyr_Song, idSong);
                divViewCount.InnerHtml = GeneralHtmlGenerator.ViewCountOverall(this);
                divViewCountLast7Days.InnerHtml = GeneralHtmlGenerator.ViewCountLast7Days(LyricsCells.ViewLastWeekOfSong(idSong));
                divViewsToday.InnerHtml = GeneralHtmlGenerator.ViewCountToday(this);

                if (idLoginedUser != 1)
                {
                    MSStoredProceduresI.ci.UpdatePlusIntValue(Tables.Lyr_Song, "Views", 1, "ID", idSong);
                }
                #endregion

                #region Include external sources
                scripts.Insert(0, "ts/Lyrics/Song.js");
                scripts.Insert(0, "js/jquery/jquery.actual.min.js");
                scripts.Insert(0, "js/jquery/jquery.equalheights.js");
                scripts.Insert(0, JavaScriptPaths.jQueryPrettySocial);
                styles.Add(StyleSheetPaths.AwesomeFont);
                styles.Add(StyleSheetPaths.PrettySocial);
                #endregion

                #region Setting <title>
                string header = null;
                int IDArtist = int.MaxValue;
                header = LyricsHelper.GetArtistAndTitle(idSong, out IDArtist);
                Title = header;
                CreateTitle();
                #endregion

                string uriOfInterpret = LyricsCells.UriOfArtist(IDArtist);
                aInterpretPage.HRef = LyricsUri.Artist(this, uriOfInterpret);

                #region Add jQuery logic to play and show/hide video
                JavaScriptInjection.InjectFunctionOpenNewTab(this, "searchOnYouTube", SearchingOnWeb.YouTube(header));
                HtmlInjection.AddOnClickParameter(searchOnYouTube, "return searchOnYouTube();");

                name.InnerHtml = header;
                DataTable dtYTVideos = MSStoredProceduresI.ci.SelectDataTableSelective(Tables.Lyr_YoutubeVideos, "IDUser,CodeYT", "IDSong", idSong);
                HtmlGenerator hgYt = new HtmlGenerator();

                if (dtYTVideos.Rows.Count == 0)
                {
                    playOnYouTube.Visible = false;
                    playHere.Visible = false;
                }
                else
                {
                    string codeYT = "";

                    foreach (DataRow item in dtYTVideos.Rows)
                    {
                        YouTubeVideoOnPage yt = new YouTubeVideoOnPage(item.ItemArray);
                        if (codeYT == "")
                        {
                            codeYT = yt.CodeYT;
                            hfYtCode.Value = codeYT;
                        }
                        hgYt.WriteTagWithAttr("a", "href", "javascript:showYTVideo2(" + yt.IDUser + ", '" + yt.CodeYT + "');");

                        string un = "";
                        if (!resolvedUserNames.ContainsKey(yt.IDUser))
                        {
                            un = GeneralCells.LoginOfUser(yt.IDUser);
                            resolvedUserNames.Add(yt.IDUser, un);
                        }
                        else
                        {
                            un = resolvedUserNames[yt.IDUser];
                        }
                        hgYt.WriteRaw(un);
                        hgYt.TerminateTag("a");
                        if (idLoginedUser == yt.IDUser)
                        {
                            hgYt.WriteRaw(" [ ");
                            hgYt.WriteTagWithAttr("a", "href", LyricsUri.ManageYtVideo(this, song, yt.CodeYT));
                            hgYt.WriteRaw("Spravovat");
                            hgYt.TerminateTag("a");
                            hgYt.WriteRaw(" ] ");
                        }

                        hgYt.WriteRaw(" | ");
                    }

                    JavaScriptInjection.InjectFunctionOpenNewTab(this, "playOnYouTube", YouTube.GetLinkToVideo(codeYT));
                    HtmlInjection.AddOnClickParameter(playHere, "return showYTVideo();");
                    HtmlInjection.AddOnClickParameter(playOnYouTube, "return playOnYouTube();");
                }
                #endregion

                hgYt.WriteTagWithAttr("a", "href", LyricsUri.ManageYtVideo(this, song, "New"));
                hgYt.WriteRaw("Přidat nové YT video");
                hgYt.TerminateTag("a");

                otherYtVideo.InnerHtml = hgYt.ToString();

                // Fetch all lyrics of song
                DataTable dtOriginal = MSStoredProceduresI.ci.SelectDataTableSelective(Tables.Lyr_Lyrics, "IDUser,Rating,IsTranslate", "IDSong", idSong, "Rating", SortOrder.Descending);
                List<LyricsWithRating> original = new List<LyricsWithRating>();
                List<LyricsWithRating> translate = new List<LyricsWithRating>();
                bool foundedTranslated = false;
                bool foundedOriginal = false;
                int idTranslatedForce = -1;
                int idOriginalForce = -1;
                HtmlGenerator hgOriginal = new HtmlGenerator();
                HtmlGenerator hgTranslated = new HtmlGenerator();

                #region Parse all of lyrics to original and translate collection
                foreach (DataRow item in dtOriginal.Rows)
                {
                    LyricsWithRating l2 = new LyricsWithRating(item.ItemArray);
                    if (!resolvedUserNames.ContainsKey(l2.IDUser))
                    {
                        string un = GeneralCells.LoginOfUser(l2.IDUser);
                        resolvedUserNames.Add(l2.IDUser, un);
                    }
                    if (l2.IsTranslate)
                    {
                        #region If song is translated
                        string un = resolvedUserNames[l2.IDUser];
                        hgTranslated.WriteTagWithAttr("a", "href", LyricsUri.SongOriginalTranslated(this, song, originalLogin, resolvedUserNames[l2.IDUser]));
                        hgTranslated.WriteRaw(un);
                        hgTranslated.TerminateTag("a");

                        if (idUserTranslated == l2.IDUser)
                        {
                            foundedTranslated = true;
                        }

                        // If translated can be any
                        if (translated == "")
                        {
                            idTranslatedForce = l2.IDUser;
                            if (WriteToHgTranslated(song, resolvedUserNames, hgTranslated, l2))
                            {
                                hgTranslated.WriteRaw(" (právě zobrazený)");
                            }
                        }
                        else if (translated == un)
                        {
                            #region In QS was lyrics exactly specified
                            idTranslatedForce = l2.IDUser;
                            if (WriteToHgTranslated(song, resolvedUserNames, hgTranslated, l2))
                            {
                                hgTranslated.WriteRaw(" (právě zobrazený)");
                            }
                            #endregion
                        }

                        hgTranslated.WriteBr();
                        translate.Add(l2);
                        #endregion
                    }
                    else
                    {
                        #region If song is in original language ...
                        string un = resolvedUserNames[l2.IDUser];
                        hgOriginal.WriteTagWithAttr("a", "href", LyricsUri.SongOriginalTranslated(this, song, resolvedUserNames[l2.IDUser], translated));
                        hgOriginal.WriteRaw(un);
                        hgOriginal.TerminateTag("a");
                        if (idUserOriginal == l2.IDUser)
                        {

                            foundedOriginal = true;
                        }

                        if (originalLogin == "")
                        {
                            idOriginalForce = l2.IDUser;
                            if (WriteToHgOriginal(song, resolvedUserNames, hgOriginal, l2))
                            {
                                hgOriginal.WriteRaw(" (právě zobrazený)");
                            }

                        }
                        else if (originalLogin != "" && originalLogin == un)
                        {
                            idOriginalForce = l2.IDUser;
                            if (WriteToHgOriginal(song, resolvedUserNames, hgOriginal, l2))
                            {
                                hgOriginal.WriteRaw(" (právě zobrazený)");
                            }
                        }
                        hgOriginal.WriteBr();
                        original.Add(l2);
                        #endregion
                    }
                }
                #endregion

                #region If exact song lyrics wasn't enter, select first in table
                int idLyricsEn = int.MaxValue;
                if (idOriginalForce != -1)
                {
                    idLyricsEn = MSStoredProceduresI.ci.SelectCellDataTableIntOneRow(true, Tables.Lyr_Lyrics, "ID", AB.Get("IDSong", idSong), AB.Get("IDUser", idOriginalForce), AB.Get("IsTranslate", false));
                }

                int idLyricsCz = int.MaxValue;
                if (idTranslatedForce != -1)
                {
                    idLyricsCz = MSStoredProceduresI.ci.SelectCellDataTableIntOneRow(true, Tables.Lyr_Lyrics, "ID", AB.Get("IDSong", idSong), AB.Get("IDUser", idTranslatedForce), AB.Get("IsTranslate", true));
                }
                #endregion

                hgTranslated.WriteTagWithAttr("a", "href", LyricsUri.ManageLyrics(this, song, LyricsType.Translated, unLoginedUser));
                hgTranslated.WriteRaw("Přidat text zde");
                hgTranslated.TerminateTag("a");

                hgOriginal.WriteTagWithAttr("a", "href", LyricsUri.ManageLyrics(this, song, LyricsType.Original, unLoginedUser));
                hgOriginal.WriteRaw("Přidat text zde");
                hgOriginal.TerminateTag("a");

                var comparer = new LyricsWithRatingComparer();
                original.Sort(comparer);
                translate.Sort(comparer);

                divAllEnLyrics.InnerHtml = hgOriginal.ToString();
                divAllCzLyrics.InnerHtml = hgTranslated.ToString();

                hfIds.Value = song.ToString();
                hfBaseUri.Value = "http://" + this.Request.Url.Host + "/";

                #region Write original lyrics to HTML
                string lyricsFirstOriginal = "";
                string lyricsFirstTranslate = "";

                if (original.Count == 0)
                {
                    lblEN.Text = "Všechny originální texty byly smazány";
                    if (unLoginedUser != "")
                    {
                        lblEN.Text += ", chcete zde text <a href='" + LyricsUri.ManageLyrics(this, song, LyricsType.Original, unLoginedUser) + "'>přidat</a>? Pokud je song pouze instrumentální, této hlášky si nevšímejte. Web nerozlišuje mezi cizojazyčnými, českými a instrumentálními songy.";
                    }
                    favAreaENNonLogined.Visible = false;
                    starRatingENNonLogined.Visible = false;
                    favAreaEN.Visible = false;
                    lblRatEN.Visible = false;
                    starRatingEn.Visible = false;
                }
                else
                {
                    if (idLyricsEn == int.MaxValue)
                    {
                        divMessageAboutDefaultTextEN.InnerHtml = "Zadaný uživatel " + originalLogin + " nebyl nalezen v textech u tohoto songu zobrazuje se song s nejvyššším hodnocením";
                        divEn.Visible = false;
                    }
                    else
                    {
                        object[] o = null;
                        string columnsWhichToFetch = "Text,Rating,Added,LastEdit";
                        if (originalLogin != "" && foundedOriginal)
                        {
                            //, AB.Get("IDSong", idSong), AB.Get("IDUser", idUserOriginal), AB.Get("IsTranslate", false)
                            o = MSStoredProceduresI.ci.SelectSelectiveOneRow(Tables.Lyr_Lyrics, "ID", idLyricsEn, columnsWhichToFetch);
                        }
                        else
                        {
                            // , AB.Get("IDSong", idSong), AB.Get("IDUser", original[0].IDUser), AB.Get("IsTranslate", false)
                            o = MSStoredProceduresI.ci.SelectSelectiveOneRow(Tables.Lyr_Lyrics, "ID", idLyricsEn, columnsWhichToFetch);
                        }
                        int viewCount = int.MinValue;
                        if (idLoginedUser == 1)
                        {
                            int viewCount2 = MSStoredProceduresI.ci.SelectCellDataTableIntOneRow(true, Tables.Lyr_Lyrics, "ViewCount", "ID", idLyricsEn);
                            if (viewCount2 != int.MaxValue)
                            {
                                viewCount = viewCount2;
                            }
                        }
                        else
                        {
                            viewCount = MSStoredProceduresI.ci.UpdatePlusIntValue(Tables.Lyr_Lyrics, "ViewCount", 1, "ID", idLyricsEn);
                        }
                        uint viewCount3 = NormalizeNumbers.NormalizeInt(viewCount);

                        viewCountEn.InnerHtml = "Počet shlédnutí: " + viewCount3;
                        lyricsFirstOriginal = MSTableRowParse.GetString(o, 0);
                        lblEN.Text = LyricsHelper.GetHtmlLyrics(lyricsFirstOriginal);
                        lblAddedEn.Text = "Přidáno: " + DTHelper.DateToString(MSTableRowParse.GetDateTime(o, 2), l);
                        lblLastEditEn.Text = "Editováno: " + DTHelper.DateToString(MSTableRowParse.GetDateTime(o, 3), l);
                        hfIdLyricsEN.Value = idLyricsEn.ToString();
                        if (idLoginedUser == -1)
                        {
                            favAreaEN.Visible = false;
                            starRatingEn.Visible = false;
                        }
                        else
                        {
                            float oriRating = MSTableRowParse.GetFloat(o, 1);
                            if (oriRating == 0)
                            {
                                lblRatEN.Text = LyricsStrings.GiveFirstRatingEn;
                            }
                            else
                            {
                                lblRatEN.Text = LyricsStrings.GiveRatingEn;
                            }
                            CheckByValue(oriRating, h1EN, h2EN, h3EN, h4EN, h5EN);
                            string imgFavSrc = GetSrcImgFav(idLyricsEn);
                            imgFavEn.Src = imgFavSrc;
                            favAreaENNonLogined.Visible = false;
                            starRatingENNonLogined.Visible = false;
                        }
                    }
                }
                #endregion

                #region Write translated lyrics to HTML
                if (translate.Count == 0)
                {
                    lblCZ.Text = "Všechny překlady této písně byly smazány nebo nikdy neexistovali";
                    if (unLoginedUser != "")
                    {
                        lblCZ.Text += ", chcete zde text <a href='" + LyricsUri.ManageLyrics(this, song, LyricsType.Translated, unLoginedUser) + "'>přidat</a>? Pokud je song původně v češtině, této hlášky si nevšímejte. Web nerozlišuje mezi cizojazyčnými, českými a instrumentálními songy.";
                    }
                    favAreaCZNonLogined.Visible = false;
                    starRatingCZNonLogined.Visible = false;
                    lblRatCZ.Visible = false;
                    starRatingCZ.Visible = false;
                    favAreaCz.Visible = false;
                }
                else
                {
                    if (idLyricsCz == int.MaxValue)
                    {
                        divMessageAboutDefaultTextCZ.InnerHtml = "Zadaný uživatel " + translated + " nebyl nalezen v textech u tohoto songu zobrazuje se song s nejvyššším hodnocením";
                        divCz.Visible = false;
                    }
                    else
                    {
                        object[] o = null;
                        string columnsWhichToFetch = "Text,Rating,Added,LastEdit";
                        if (translated != "" && foundedTranslated)
                        {
                            o = MSStoredProceduresI.ci.SelectSelectiveOneRow(Tables.Lyr_Lyrics, "ID", idLyricsCz, columnsWhichToFetch);
                        }
                        else
                        {
                            o = MSStoredProceduresI.ci.SelectSelectiveOneRow(Tables.Lyr_Lyrics, "ID", idLyricsCz, columnsWhichToFetch);
                        }
                        int viewCount = int.MinValue;
                        if (idLoginedUser == 1)
                        {
                            int viewCount2 = MSStoredProceduresI.ci.SelectCellDataTableIntOneRow(true, Tables.Lyr_Lyrics, "ViewCount", "ID", idLyricsCz);
                            if (viewCount2 != int.MaxValue)
                            {
                                viewCount = viewCount2;
                            }
                        }
                        else
                        {
                            viewCount = MSStoredProceduresI.ci.UpdatePlusIntValue(Tables.Lyr_Lyrics, "ViewCount", 1, "ID", idLyricsCz);
                        }
                        uint viewCount3 = NormalizeNumbers.NormalizeInt(viewCount);

                        viewCountCz.InnerHtml = "Počet shlédnutí: " + viewCount3;
                        lyricsFirstTranslate = MSTableRowParse.GetString(o, 0);
                        lblCZ.Text = LyricsHelper.GetHtmlLyrics(lyricsFirstTranslate);

                        lblAddedCz.Text = "Přidáno: " + DTHelper.DateToString(MSTableRowParse.GetDateTime(o, 2), l);
                        lblLastEditCz.Text = "Editováno: " + DTHelper.DateToString(MSTableRowParse.GetDateTime(o, 3), l);
                        hfIdLyricsCZ.Value = idLyricsCz.ToString();

                        if (idLoginedUser == -1)
                        {
                            favAreaCz.Visible = false;
                            starRatingCZ.Visible = false;
                        }
                        else
                        {
                            float tranRating = MSTableRowParse.GetFloat(o, 1);
                            if (tranRating == 0)
                            {
                                lblRatCZ.Text = LyricsStrings.GiveFirstRatingCz;
                            }
                            else
                            {
                                lblRatCZ.Text = LyricsStrings.GiveRatingCz;
                            }
                            CheckByValue(tranRating, h1CZ, h2CZ, h3CZ, h4CZ, h5CZ);

                            string imgFavSrc = GetSrcImgFav(idLyricsCz);
                            imgFavCz.Src = imgFavSrc;
                            favAreaCZNonLogined.Visible = false;
                            starRatingCZNonLogined.Visible = false;
                        }
                    }
                }
                #endregion

                #region Set meta info of page as OpenGraph, Schema.org snippets and to PrettySocial widget
                PageSnippet pageSnippet = null;

                if (YouTubeThumbnailLyrics.ci.HasAnyFile(idSong))
                {
                    int divide = 1;
                    if (lyricsFirstTranslate != "" && lyricsFirstOriginal != "")
                    {
                        divide = 2;
                    }
                    pageSnippet = new PageSnippet { description = SH.ShortForLettersCountThreeDots(SH.TrimNewLineAndTab(lyricsFirstOriginal), 150 / divide) + " " + SH.ShortForLettersCountThreeDots(SH.TrimNewLineAndTab(lyricsFirstTranslate), 150 / divide), image = Consts.HttpWwwCzSlash + YouTubeThumbnailLyrics.ci.GetBaseUri(idSong, 1), title = Title };
                }

                if (pageSnippet != null)
                {
                    OpenGraphHelper.InsertBasicToPageHeader(this, pageSnippet, MySites.Lyrics);
                    SchemaOrgHelper.InsertBasicToPageHeader(this, pageSnippet, MySites.Lyrics);
                    descriptionPage = pageSnippet.description;
                }

                PrettySocialHelper.Init(this, Title, descriptionPage, google, facebook, twitter);
                #endregion

                #region Append five random songs from same artist
                List<int> addedYtVideosToJsArray = new List<int>();
                StringBuilder fillingJavaScriptArray = new StringBuilder();
                object[] oa = MSStoredProceduresI.ci.SelectSelectiveOneRow(Tables.Lyr_Artist, "ID", IDArtist, "Name,CountTags,CountSimilar");
                string nameOfArtist = MSTableRowParse.GetString(oa, 0);
                byte countTags = MSTableRowParse.GetByte(oa, 1);
                byte countSimilar = MSTableRowParse.GetByte(oa, 2);
                int max = 10;
                int maxSameArtist = 5;
                List<int> similarSongs = new List<int>();

                List<int> songsOfArtist2 = LyricsHelper.GetSongsOfArtist(IDArtist);
                foreach (var item3 in songsOfArtist2)
                {
                    if (item3 != idSong)
                    {
                        similarSongs.Add(item3);
                        if (similarSongs.Count == maxSameArtist)
                        {
                            break;
                        }
                    }
                }
                #endregion

                #region Fill up to 10 similar songs from other artist
                int[] idSimilarArtists = LyricsHelper.GetSimilarOfArtist(IDArtist, countSimilar).ToArray();

                idSimilarArtists = CA.JumbleUp<int>(idSimilarArtists);
                foreach (var item2 in idSimilarArtists)
                {
                    if (similarSongs.Count == max)
                    {
                        break;
                    }
                    List<int> songsOfArtist = LyricsHelper.GetSongsOfArtist(item2);
                    foreach (var item3 in songsOfArtist)
                    {
                        if (item3 != idSong)
                        {
                            similarSongs.Add(item3);
                            if (similarSongs.Count == max)
                            {
                                break;
                            }
                        }
                    }
                }
                #endregion

                #region If still won't be 10 songs, fill up to 10 songs by tags
                short[] idTags = LyricsHelper.GetTagsOfArtist(IDArtist, countTags).ToArray();
                idTags = CA.JumbleUp<short>(idTags);

                foreach (var item in idTags)
                {
                    if (similarSongs.Count == max)
                    {
                        break;
                    }

                    int[] idArtistOfTag = MSStoredProceduresI.ci.SelectValuesOfColumnAllRowsInt(Tables.Lyr_ArtistTag, "IDArtist", new ABC(AB.Get("IDTag", item)), new ABC(AB.Get("IDArtist", IDArtist))).ToArray();
                    idArtistOfTag = CA.JumbleUp<int>(idArtistOfTag);
                    foreach (var item2 in idArtistOfTag)
                    {
                        if (similarSongs.Count == max)
                        {
                            break;
                        }

                        List<int> songsOfArtist = LyricsHelper.GetSongsOfArtist(item2);
                        foreach (var item3 in songsOfArtist)
                        {
                            if (item2 != IDArtist)
                            {
                                similarSongs.Add(item3);
                                if (similarSongs.Count == max)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                #endregion

                List<string> viewCountBadgesYouCanLikeAlso = new List<string>();
                List<string> idBadgesYouCanLikeAlso = new List<string>();

                List<string> viewCountBadgesMostPopularOfInterpretAlbum = new List<string>();
                List<string> idBadgesMostPopularOfInterpretAlbum = new List<string>();

                List<string> idBadges = new List<string>();

                int viewCountBadges = 0;

                string nameJsArray = "videos";

                #region Write to output popular similar songs
                bool assignedImage = false;
                bool atLeastOneYouCanLikeAlso = false;
                if (similarSongs.Count != 0)
                {
                    #region Add info about similar songs to collections
                    HtmlGenerator hgYouCanLikeAlso = new HtmlGenerator();
                    int countSongsYouCanLikeAlso = similarSongs.Count;

                    List<string> linksPhoto = new List<string>(countSongsYouCanLikeAlso);
                    List<string> linksText = new List<string>(countSongsYouCanLikeAlso);
                    List<string> innerHtmlText = new List<string>(countSongsYouCanLikeAlso);
                    List<string> srcPhoto = new List<string>(countSongsYouCanLikeAlso);

                    // Shuffle to make it more varied
                    int[] ssju = CA.JumbleUp<int>(similarSongs.ToArray());

                    foreach (int item in ssju)
                    {
                        int idSong2 = item;
                        if (idSong2 == idSong)
                        {
                            continue;
                        }

                        if (!addedYtVideosToJsArray.Contains(idSong2))
                        {
                            fillingJavaScriptArray.Append(YouTubeThumbnailLyrics.ci.AllFilesRelativeUriJavascriptArray(nameJsArray, idSong2));
                            addedYtVideosToJsArray.Add(idSong2);
                        }
                        else
                        {
                            #region If any song will be twice in similar, I fill up to 10 with not-similar song. Still is better song in differenct stile than none
                            idSong2 = MSStoredProceduresI.ci.RandomValueFromColumnInt(Tables.Lyr_Song, "ID");
                            while (addedYtVideosToJsArray.Contains(idSong2))
                            {
                                idSong2 = MSStoredProceduresI.ci.RandomValueFromColumnInt(Tables.Lyr_Song, "ID");
                            }
                            addedYtVideosToJsArray[addedYtVideosToJsArray.Count - 1] = idSong2;
                            #endregion
                        }

                        object[] o = MSStoredProceduresI.ci.SelectSelectiveOneRow(Tables.Lyr_Song, "ID", idSong2, "Name,IDArtist,Views");
                        int idArtistOfSimilarSong = MSTableRowParse.GetInt(o, 1);
                        int viewLastWeek = MSTableRowParse.GetInt(o, 2);

                        if (!assignedImage)
                        {
                            if (IDArtist != int.MaxValue)
                            {
                                if (idArtistOfSimilarSong != IDArtist)
                                {
                                    if (File.Exists(LyricsHelper.GetPathOfArtistImage(idArtistOfSimilarSong, 0, ImageSize.Small)))
                                    {
                                        imgYouCanLikeAlso.Attributes["style"] = CssGeneration.GetRoundedImage(34, LyricsHelper.GetUriOfArtistImage(Request, idArtistOfSimilarSong, 0, ImageSize.Small), 13);
                                        assignedImage = true;
                                    }
                                    else
                                    {
                                        imgYouCanLikeAlso.Visible = false;
                                    }
                                }
                            }
                            else
                            {
                                assignedImage = true;
                                imgYouCanLikeAlso.Visible = false;
                            }
                        }

                        atLeastOneYouCanLikeAlso = true;
                        string nameSong = MSTableRowParse.GetString(o, 0);

                        srcPhoto.Add(idSong2.ToString());
                        string noa = LyricsCells.NameOfArtist(idArtistOfSimilarSong);
                        string anchor = LyricsHelper.UriOfSong(this, noa, nameSong);
                        linksPhoto.Add(anchor);
                        linksText.Add(anchor);
                        innerHtmlText.Add(" " + LyricsHelper.GetArtistAndTitle(noa, nameSong));

                        string idb = viewCountBadges.ToString();

                        idBadgesYouCanLikeAlso.Add(idb);
                        viewCountBadgesYouCanLikeAlso.Add(NormalizeNumbers.NormalizeInt(viewLastWeek).ToString());
                        viewCountBadges++;

                    }
                    #endregion

                    HtmlGenerator2.TopListWithImages(hgYouCanLikeAlso, 130, 97, web.UH.GetWebUri(this, "img/Lyrics/NoThumbnail.png"), linksPhoto, linksText, innerHtmlText, srcPhoto, idBadgesYouCanLikeAlso, nameJsArray);
                    JavaScriptInjection.InjectInternalScript(this, JavaScriptGenerator2.JsForIosBadge("blue", idBadgesYouCanLikeAlso, viewCountBadgesYouCanLikeAlso));
                    if (atLeastOneYouCanLikeAlso)
                    {
                        innerDivYouCanLikeAlso.InnerHtml = hgYouCanLikeAlso.ToString();
                    }
                }
                divYouCanLikeAlso.Visible = atLeastOneYouCanLikeAlso;
                #endregion

                #region Write to output popular songs from same interpret
                bool atLeastOneMostPopularOfInterpret = false;
                int maxCountMostPopularOfInterpret = 10;
                int actualCountMostPopularOfInterpret = 0;

                if (IDArtist != int.MaxValue)
                {
                    HtmlGenerator hgMostPopularOfInterpret = new HtmlGenerator();
                    // Get most popular of song - Views DB column
                    DataTable dtMostPopularOfInterpret = MSStoredProceduresI.ci.SelectDataTableLimitLastRows(Tables.Lyr_Song, maxCountMostPopularOfInterpret + 1, "ID,Name,Views", "Views", AB.Get("IDArtist", IDArtist));

                    int countSongsMostPopularOfInterpret = dtMostPopularOfInterpret.Rows.Count;
                    if (countSongsMostPopularOfInterpret != 0)
                    {
                        int viewLastWeekFirst = MSTableRowParse.GetInt(dtMostPopularOfInterpret.Rows[0].ItemArray, 2);
                        if (viewLastWeekFirst != int.MinValue)
                        {
                            countSongsMostPopularOfInterpret = Math.Min(countSongsMostPopularOfInterpret, maxCountMostPopularOfInterpret);

                            List<string> linksPhoto = new List<string>(countSongsMostPopularOfInterpret);
                            List<string> linksText = new List<string>(countSongsMostPopularOfInterpret);
                            List<string> innerHtmlText = new List<string>(countSongsMostPopularOfInterpret);
                            List<string> srcPhoto = new List<string>(countSongsMostPopularOfInterpret);
                            List<string> viewCountBadgesMostPopularOfInterpret = new List<string>();
                            List<string> idBadgesMostPopularOfInterpret = new List<string>();

                            foreach (DataRow item in dtMostPopularOfInterpret.Rows)
                            {
                                if (actualCountMostPopularOfInterpret == maxCountMostPopularOfInterpret)
                                {
                                    break;
                                }

                                object[] o = item.ItemArray;
                                int viewLastWeek = MSTableRowParse.GetInt(o, 2);
                                if (viewLastWeek == int.MinValue)
                                {
                                    break;
                                }
                                int idVideo = MSTableRowParse.GetInt(o, 0);
                                if (idVideo == idSong)
                                {
                                    continue;
                                }
                                actualCountMostPopularOfInterpret++;
                                atLeastOneMostPopularOfInterpret = true;
                                string nameSong = MSTableRowParse.GetString(o, 1);

                                if (!addedYtVideosToJsArray.Contains(idVideo))
                                {
                                    fillingJavaScriptArray.Append(YouTubeThumbnailLyrics.ci.AllFilesRelativeUriJavascriptArray(nameJsArray, idVideo));
                                    addedYtVideosToJsArray.Add(idVideo);
                                }

                                srcPhoto.Add(idVideo.ToString());
                                string odkaz = LyricsHelper.UriOfSong(this, nameOfArtist, nameSong);
                                linksPhoto.Add(odkaz);
                                linksText.Add(odkaz);
                                innerHtmlText.Add(" " + LyricsHelper.GetArtistAndTitle(nameOfArtist, nameSong));

                                string idb = viewCountBadges.ToString();
                                idBadgesMostPopularOfInterpret.Add(idb);

                                viewCountBadgesMostPopularOfInterpret.Add(NormalizeNumbers.NormalizeInt(viewLastWeek).ToString());
                                viewCountBadges++;
                            }

                            JavaScriptInjection.InjectInternalScript(this, JavaScriptGenerator2.JsForIosBadge("blue", idBadgesMostPopularOfInterpret, viewCountBadgesMostPopularOfInterpret));
                            HtmlGenerator2.TopListWithImages(hgMostPopularOfInterpret, 130, 97, web.UH.GetWebUri(this, "img/Lyrics/NoThumbnail.png"), linksPhoto, linksText, innerHtmlText, srcPhoto, idBadgesMostPopularOfInterpret, nameJsArray);
                        }
                    }

                    if (atLeastOneMostPopularOfInterpret)
                    {
                        if (File.Exists(LyricsHelper.GetPathOfArtistImage(IDArtist, 0, ImageSize.Small)))
                        {
                            imgInterpret.Attributes["style"] = CssGeneration.GetRoundedImage(34, LyricsHelper.GetUriOfArtistImage(Request, IDArtist, 0, ImageSize.Small), 13);
                        }
                        else
                        {
                            imgInterpret.Visible = false;
                        }

                        innerDivMostPopularOfInterpret.InnerHtml = hgMostPopularOfInterpret.ToString();
                    }
                }

                divMostPopularOfInterpret.Visible = atLeastOneMostPopularOfInterpret;
                #endregion

                #region Write to output popular songs from same albums
                bool atLeastOneMostPopularOfInterpretAlbum = false;
                if (idAlbum != short.MaxValue)
                {
                    aAlbumPage.InnerHtml += " " + nameAlbum;
                    aAlbumPage.HRef = LyricsUri.Album(this, uriOfInterpret, LyricsCells.UriOfAlbum(idAlbum));
                    HtmlGenerator hgMostPopularOfInterpretAlbum = new HtmlGenerator();
                    DataTable dtMostPopularOfInterpretAlbum = MSStoredProceduresI.ci.SelectDataTableLimitLastRows(Tables.Lyr_Song, int.MaxValue, "ID,Name,Views", "Views", AB.Get("IDArtist", IDArtist), AB.Get("IDAlbum", idAlbum));

                    int countSongsMostPopularOfInterpretAlbum = dtMostPopularOfInterpretAlbum.Rows.Count;

                    if (countSongsMostPopularOfInterpretAlbum != 0)
                    {
                        int viewLastWeekFirst = MSTableRowParse.GetInt(dtMostPopularOfInterpretAlbum.Rows[0].ItemArray, 2);
                        if (viewLastWeekFirst != int.MinValue)
                        {
                            List<string> linksPhoto = new List<string>(countSongsMostPopularOfInterpretAlbum);
                            List<string> linksText = new List<string>(countSongsMostPopularOfInterpretAlbum);
                            List<string> innerHtmlText = new List<string>(countSongsMostPopularOfInterpretAlbum);
                            List<string> srcPhoto = new List<string>(countSongsMostPopularOfInterpretAlbum);

                            foreach (DataRow item in dtMostPopularOfInterpretAlbum.Rows)
                            {
                                object[] o = item.ItemArray;
                                int viewLastWeek = MSTableRowParse.GetInt(o, 2);
                                if (viewLastWeek == int.MinValue)
                                {
                                    break;
                                }
                                int idVideo = MSTableRowParse.GetInt(o, 0);
                                if (idVideo == idSong)
                                {
                                    continue;
                                }
                                atLeastOneMostPopularOfInterpretAlbum = true;
                                string nameSong = MSTableRowParse.GetString(o, 1);

                                if (!addedYtVideosToJsArray.Contains(idVideo))
                                {
                                    fillingJavaScriptArray.Append(YouTubeThumbnailLyrics.ci.AllFilesRelativeUriJavascriptArray(nameJsArray, idVideo));
                                    addedYtVideosToJsArray.Add(idVideo);
                                }

                                srcPhoto.Add(idVideo.ToString());
                                string odkaz = LyricsHelper.UriOfSong(this, nameOfArtist, nameSong);
                                linksPhoto.Add(odkaz);
                                linksText.Add(odkaz);
                                innerHtmlText.Add(" " + LyricsHelper.GetArtistAndTitle(nameOfArtist, nameSong));


                                string idb = viewCountBadges.ToString();

                                idBadgesMostPopularOfInterpretAlbum.Add(idb);
                                viewCountBadgesMostPopularOfInterpretAlbum.Add(NormalizeNumbers.NormalizeInt(viewLastWeek).ToString());
                                viewCountBadges++;
                            }

                            JavaScriptInjection.InjectInternalScript(this, JavaScriptGenerator2.JsForIosBadge("blue", idBadgesMostPopularOfInterpretAlbum, viewCountBadgesMostPopularOfInterpretAlbum));
                            HtmlGenerator2.TopListWithImages(hgMostPopularOfInterpretAlbum, 130, 97, web.UH.GetWebUri(this, "img/Lyrics/NoThumbnail.png"), linksPhoto, linksText, innerHtmlText, srcPhoto, idBadgesMostPopularOfInterpretAlbum, nameJsArray);
                        }
                    }

                    if (atLeastOneMostPopularOfInterpretAlbum)
                    {
                        if (File.Exists(LyricsHelper.GetPathOfAlbumImage(idAlbum, AlbumImageSize.Small)))
                        {
                            imgAlbum.Attributes["style"] = CssGeneration.GetRoundedImage(34, LyricsHelper.GetUriOfAlbumImage(Request, idAlbum, AlbumImageSize.Small), 13);
                        }
                        else
                        {
                            imgAlbum.Visible = false;
                        }

                        innerDivMostPopularOfInterpretAlbum.InnerHtml = hgMostPopularOfInterpretAlbum.ToString();
                    }
                }
                else
                {
                    divAlbumPage.Visible = false;
                }

                divMostPopularOfInterpretAlbum.Visible = atLeastOneMostPopularOfInterpretAlbum;
                #endregion

                if (atLeastOneMostPopularOfInterpret || atLeastOneMostPopularOfInterpretAlbum || atLeastOneYouCanLikeAlso)
                {
                    JavaScriptInjection.InjectInternalScript(this, JavaScriptGenerator2.AlternatingImages(nameJsArray, nameJsArray, true));
                    JavaScriptInjection.InjectInternalScript(this, fillingJavaScriptArray.ToString());
                }
            }
            else
            {
                NoToView();
                Include(styles, scripts, null, null);
                return;
            }
            FillIDUsers();

            Include(styles, scripts, null, null);
        }

        private string GetSrcImgFav(int idLyricsCz)
        {
            if (MSStoredProceduresI.ci.SelectExistsCombination(Tables.Lyr_FavoritesVotes, AB.Get("IDLyrics", idLyricsCz), AB.Get("IDUser", idLoginedUser)))
            {
                return web.UH.GetWebUri3(this, "img/star16.png");
            }
            return web.UH.GetWebUri3(this, "img/unstar16.png");
        }

        private bool WriteToHgOriginal(string song, Dictionary<int, string> resolvedUserNames, HtmlGenerator hgOriginal, LyricsWithRating l)
        {
            return AppendAuthorToHg(song, resolvedUserNames, hgOriginal, l, LyricsType.Original);
        }

        private bool AppendAuthorToHg(string song, Dictionary<int, string> resolvedUserNames, HtmlGenerator hgOriginal, LyricsWithRating l, LyricsType lyricsType)
        {
            if (idLoginedUser == l.IDUser)
            {
                hgOriginal.WriteRaw(" (");
                hgOriginal.WriteTagWithAttr("a", "href", LyricsUri.ManageLyrics(this, song, lyricsType, resolvedUserNames[idLoginedUser]));
                hgOriginal.WriteRaw("Spravovat");
                hgOriginal.TerminateTag("a");
                hgOriginal.WriteRaw(")");
                hgOriginal.WriteRaw(" (právě zobrazený)");

                return false;
            }
            return true;
        }

        private bool WriteToHgTranslated(string song, Dictionary<int, string> resolvedUserNames, HtmlGenerator hgTranslated, LyricsWithRating l)
        {
            return AppendAuthorToHg(song, resolvedUserNames, hgTranslated, l, LyricsType.Translated);
        }

        private void NoSongID()
        {
            content.Visible = false;
            Warning("Litujeme, ale zadané číslo textu písně zde nemáme");
            return;
        }

        private void NoToView()
        {
            content.Visible = false;
            Warning("Špatný odkaz, chybí Query parametr song");
        }

        private void CheckByValue(double rating, params HtmlInputRadioButton[] radioButtons)
        {
            int hod = Convert.ToInt32(rating);
            if (rating > 4.5)
            {
                hod = 5;
            }
            else if (rating > 3.5)
            {
                hod = 4;
            }
            else if (rating > 2.5)
            {
                hod = 3;
            }
            else if (rating > 1.5)
            {
                hod = 2;
            }
            else if (hod <= 1.5 && hod > 0)
            {
                hod = 1;
            }
            else
            {
                hod--;
            }
            if (hod < -1)
            {
                hod = -1;
            }
            if (hod != -1)
            {
                radioButtons[hod - 1].Checked = true;
            }
        }
    }
}
