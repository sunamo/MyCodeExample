<%@ Page EnableViewState="false" Title="Album nenalezeno" Language="C#" MasterPageFile="~/Lyrics.Master" AutoEventWireup="true" CodeBehind="Album.aspx.cs" Inherits="sunamo.cz.Lyrics.Album" %>
<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <p class="chyba" id="errors" runat="server" visible="false" enableviewstate="false"></p>

    <div id="divContent" runat="server">
        <ul class="breadcrumbs2" id="divNavigation" runat="server">
            <li><a runat="server" id="bcHome"><span class="icon mif-home"></span></a></li>
            <li><a runat="server" id="bcArtist"></a></li>
            <li><a runat="server" id="bcAlbum"></a></li>
        </ul>

        <h1 id="h1" runat="server"></h1>

        <p><i>Všechny informace na této stránce pocházejí z <a href="http://last.fm/">Last.fm</a></i></p>

        <div id="divJustifiedAlbumsWrapper" runat="server">
            <h3>Další alba interpreta</h3>

            <div id="divJustifiedAlbums" runat="server"></div>
        </div>

        <div id="divSongsInAlbumWrapper" runat="server">
            <h3>Songy z tohoto alba</h3>

            <div id="divSongsInAlbum" runat="server"></div>
        </div>

    </div>
</asp:Content>
