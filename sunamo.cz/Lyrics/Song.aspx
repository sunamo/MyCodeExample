<%@ Page EnableViewState="false" Title="Detail songu" Language="C#" MasterPageFile="~/Lyrics.Master" AutoEventWireup="true" CodeBehind="Song.aspx.cs" Inherits="sunamo.cz.Ggdag.ShowSong" %>

<asp:Content ID="Content3" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <p class="error" id="errors" runat="server" visible="false" enableviewstate="false"></p>

    <div id="divContent" runat="server">
        <asp:HiddenField runat="server" ID="hfOriginalUser" />
        <asp:HiddenField runat="server" ID="hfTranslateUser" />
        <asp:HiddenField runat="server" ID="hfYtCode" />
        <asp:HiddenField ID="hfIds" runat="server" />
        <asp:HiddenField ID="hfIdLyricsEN" runat="server" />
        <asp:HiddenField ID="hfIdLyricsCZ" runat="server" />
        <asp:HiddenField ID="hfBaseUri" runat="server" />
        <asp:HiddenField ID="hfDefaultOriginal" runat="server" />
        <asp:HiddenField ID="hfDefaultTranslated" runat="server" />

        <div class="vo tb" id="content" runat="server">
            <h1 id="name" style="font-size: 45px;" runat="server"></h1>

            <div class="toCenter">
                <hr style="width: 216px;" class="naStred" />

                <div class="social-container">
                    <div class="links">
                        <a data-type="googleplus" id="google" runat="server" class="prettySocial fa fa-google-plus"></a>
                        <a data-type="twitter" id="twitter" runat="server" data-via="sunamo" class="prettySocial fa fa-twitter"></a>
                        <a data-type="facebook" id="facebook" runat="server" class="prettySocial fa fa-facebook"></a>
                    </div>
                </div>
                <hr style="width: 216px;" class="naStred" />
            </div>

            <div class="marginTop15Bottom5">
                <a id="aInterpretPage" runat="server">Stránka interpreta</a>
                <div id="divAlbumPage" runat="server"><a id="aAlbumPage" runat="server">Stránka alba</a></div>
            </div>

            <p><i>Špatné video, text nebo překlad ke songu? Informujte mě o tomto problému na <a href="mailto:radek.jancik@sunamo.cz">mailu</a>.</i></p>

            <div id="divViewCount" runat="server"></div>
            <div id="divViewCountLast7Days" runat="server"></div>
            <div id="divViewsToday" runat="server"></div>

            <div class="text-size">
                Velikost textu a překladu:
				<a href="#" id="decfont">A-</a>
                <a href="#" id="defaultfont">A</a>
                <a href="#" id="incfont">A+</a>
            </div>

            <div class="preklady">
                <div class="en2 language post-single-content-inner" style="">

                    <div id="divMessageAboutDefaultTextEN" runat="server"></div>
                    <br />
                    <div id="divEn" runat="server">
                        <div id="divEnInner">
                            <div class="favArea" id="favAreaEN" runat="server">
                                <img src="../img/unstar16.png" id="imgFavEn" runat="server" /><span id="spanFavEn" runat="server">Přidat text písně do oblíbených</span>
                            </div>
                            <div id="favAreaENNonLogined" runat="server">Pro přidání textu písničky do oblíbených se přihlašte</div>
                            <br />
                            <br />
                            <asp:Literal ID="lblEN" runat="server"></asp:Literal>
                            <br />
                            <br />

                            <div id="viewCountEn" runat="server"></div>
                            <asp:Label ID="lblRatEN" runat="server"></asp:Label><br />

                            <div class="star-rating" id="starRatingEn" runat="server">

                                <input type="radio" id="h1EN" runat="server" class="star" name="RatingEN" value="" onclick="DoRate('EN', 1); return true;" />
                                <input type="radio" id="h2EN" runat="server" class="star" name="RatingEN" value="" onclick="DoRate('EN', 2); return true;" />
                                <input type="radio" id="h3EN" runat="server" class="star" name="RatingEN" value="" onclick="DoRate('EN', 3); return true;" />
                                <input type="radio" id="h4EN" runat="server" class="star" name="RatingEN" value="" onclick="DoRate('EN', 4); return true;" />
                                <input type="radio" id="h5EN" runat="server" class="star" name="RatingEN" value="" onclick="DoRate('EN', 5); return true;" />
                            </div>
                            <div id="starRatingENNonLogined" runat="server">
                                Pro hlasování na kvalitu písně se přihlašte
                            </div>
                            <br />

                            <asp:Label ID="lblAddedEn" runat="server"></asp:Label><br />
                            <asp:Label ID="lblLastEditEn" runat="server"></asp:Label>
                        </div>

                        <div>Všechny originální texty k této písni:</div>
                        <div id="divAllEnLyrics" runat="server"></div>

                    </div>
                </div>
                <div class="language cz2 post-single-content-inner">

                    <div id="divMessageAboutDefaultTextCZ" runat="server"></div>
                    <br />
                    <div id="divCz" runat="server">
                        <div id="divCzInner">

                            <div class="favArea" id="favAreaCz" runat="server">
                                <img src="../img/unstar16.png" id="imgFavCz" runat="server" /><span id="spanFavCz" runat="server">Přidat překlad do oblíbených</span>
                            </div>
                            <div id="favAreaCZNonLogined" runat="server">Pro přidání překladu písničky do oblíbených se přihlašte</div>

                            <br />
                            <br />
                            <asp:Literal ID="lblCZ" runat="server"></asp:Literal>
                            <br />
                            <br />

                            <div id="viewCountCz" runat="server"></div>
                            <asp:Label ID="lblRatCZ" runat="server"></asp:Label><br />

                            <div class="star-rating" id="starRatingCZ" runat="server">
                                <input type="radio" id="h1CZ" runat="server" class="star" name="RatingCZ" value="" onclick="DoRate('CZ', 1); return true;" />
                                <input type="radio" id="h2CZ" runat="server" class="star" name="RatingCZ" value="" onclick="DoRate('CZ', 2); return true;" />
                                <input type="radio" id="h3CZ" runat="server" class="star" name="RatingCZ" value="" onclick="DoRate('CZ', 3); return true;" />
                                <input type="radio" id="h4CZ" runat="server" class="star" name="RatingCZ" value="" onclick="DoRate('CZ', 4); return true;" />
                                <input type="radio" id="h5CZ" runat="server" class="star" name="RatingCZ" value="" onclick="DoRate('CZ', 5); return true;" />
                            </div>

                            <div id="starRatingCZNonLogined" runat="server">
                                Pro hlasování na kvalitu překladu písně se přihlašte
                            </div>
                            <br />
                            <asp:Label ID="lblAddedCz" runat="server"></asp:Label><br />
                            <asp:Label ID="lblLastEditCz" runat="server"></asp:Label>
                        </div>

                        <div>Všechny překlady k této písni:</div>
                        <div id="divAllCzLyrics" runat="server"></div>

                    </div>
                </div>
            </div>

            <div style="clear: both; width: 100%; height: 1px"></div>
            <div id="botton">

                <div id="ytVideo" runat="server" style="display: none;">
                    <br />
                </div>

                <div id="otherYtVideo" runat="server"></div>

                <p>
                    <button class="button primary" runat="server" id="playHere" type="button"><span class="mif-play"></span>Přehrát zde</button>
                    <button class="button" runat="server" id="playOnYouTube" type="button"><span class="mif-youtube"></span>Přehrát na YouTube</button>
                    <button class="button" runat="server" id="searchOnYouTube" type="button"><span class="mif-search"></span>Vyhledat na YouTube</button>
                </p>
            </div>

            <div id="divYouCanLikeAlso" runat="server">
                <div id="imgYouCanLikeAlso" runat="server"></div>
                <h3 style="float: left;">Mohlo by se Vám také líbit</h3>
                <div class="cb"></div>
                <div id="innerDivYouCanLikeAlso" runat="server"></div>
            </div>

            <div id="divMostPopularOfInterpret" runat="server">
                <div id="imgInterpret" runat="server"></div>
                <h3 style="float: left">Nejpopulárnější songy interpreta</h3>
                <div class="cb"></div>
                <div id="innerDivMostPopularOfInterpret" runat="server"></div>
            </div>

            <div id="divMostPopularOfInterpretAlbum" runat="server">
                <div id="imgAlbum" runat="server"></div>
                <h3 style="float: left;">Nejpopulárnější songy z tohoto alba</h3>
                <div class="cb"></div>
                <div id="innerDivMostPopularOfInterpretAlbum" runat="server"></div>
            </div>

        </div>
    </div>
</asp:Content>
