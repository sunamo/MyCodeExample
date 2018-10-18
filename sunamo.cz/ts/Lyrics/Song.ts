import { LyricsShared } from "./LyricsShared";
import { su, MySitesShort, StatusType } from "su";

var hfYtUserId: JQuery = null;
var hfYtCode: JQuery = null;
var ytVideo: JQuery = null;
var beforeHoveringCz: boolean = false;
var beforeHoveringEn: boolean = false;
var unstar = "";
var star = "";

window.onload = function () {

    ytVideo = $('#ytVideo');
    su.hfBaseUri = $("#hfBaseUri");
    var baseUri = su.t(su.hfBaseUri);
    star = baseUri + "img/star16.png";
    unstar = baseUri + "img/unstar16.png";
    $('.language').equalHeights();
    LyricsShared.showDelimiterBetweenLyrics();

    hfYtUserId = $("#hfYtUserId");
    hfYtCode = $("#hfYtCode");

    SetHtmlYtVideo();

    var imgFavEn = $("#imgFavEn");
    var imgFavCz = $("#imgFavCz");

    if (imgFavEn.length) {
        SetLyricsIsInFavorite(false);
        imgFavEn.mouseover({ cz: false }, AfterMouseHover);
    }
    if (imgFavCz.length) {
        SetLyricsIsInFavorite(true);
        imgFavCz.bind("mouseover", { cz: true }, AfterMouseHover);
        imgFavCz.bind("mouseleave", { cz: true }, AfterMouseLeave);
        var imgFavCzElement = document.getElementById("imgFavCz");
        imgFavCz.bind("click", { cz: true }, DoFavorite);
    }
};

function getImgFavorite(cz): JQuery {
    if (cz) {
        return $("#imgFavCz");
    }
    else {
        return $("#imgFavEn");
    }
}

function getSpanFavorite(cz): JQuery {
    if (cz) {
        return $("#spanFavCz");
    }
    else {
        return $("#spanFavEn");
    }
}
// Používá se jen po načtení stránky, abych uložil zda je lyrics už v oblíbených nebo není
function SetLyricsIsInFavorite(cz) {
    var img = getImgFavorite(cz);
    var imgSrc = img.attr("src");
    if (imgSrc == unstar) {
        if (cz) {
            beforeHoveringCz = false;
        }
        else {
            beforeHoveringEn = false;
        }
    }
    else {
        if (cz) {
            beforeHoveringCz = true;
        }
        else {
            beforeHoveringEn = true;
        }
    }
    if (cz) {
        SetSpanByFavoriteBit(cz, beforeHoveringCz);
    }
    else {
        SetSpanByFavoriteBit(cz, beforeHoveringEn);

    }
    SetImgFavByBool(cz);
}

// Is calling only in AfterMouseHover()
function SetInverseSrc(img) {
    var imgSrc = img.attr("src");
    var toFavorite = (imgSrc == unstar);

    SetImageByFavoriteBit(img, toFavorite);

    return true;
}

function SetImgFavByBool(cz) {
    var dd = beforeHoveringEn;

    if (cz) {
        dd = beforeHoveringCz;
    }
    var img = getImgFavorite(cz);
    SetImageByFavoriteBit(img, dd);

    return true;
}

// Set right image by A2
function SetImageByFavoriteBit(img, toFavorite) {

    if (toFavorite) {
        img.attr("src", star);
    }
    else {
        img.attr("src", unstar);
    }
}

function DoFavorite(jq: JQuery.Event<HTMLElement, any>): false | JQuery.EventHandler<HTMLElement, null> | JQuery.EventHandlerBase<any, JQuery.Event<HTMLElement, null>> {
    var idLyrics = null;
    var cz = jq.data.cz;
    if (cz) {
        idLyrics = su.i($("#hfIdLyricsCZ"));
    }
    else {
        idLyrics = su.i($("#hfIdLyricsEN"));
    }

    var result = su.ajaxGet4("Lyr_FavoriteHandler.ashx?idLyrics=" + idLyrics);
    var isOk = (su.ToStatus2(result) == "uspech");
    if (isOk) {
        var img = getImgFavorite(cz);
        if (cz) {
            beforeHoveringCz = !beforeHoveringCz;
            SetSpanByFavoriteBit(cz, beforeHoveringCz);
            SetImageByFavoriteBit(img, beforeHoveringCz);
        }
        else {
            beforeHoveringEn = !beforeHoveringEn;
            SetSpanByFavoriteBit(cz, beforeHoveringEn);
            SetImageByFavoriteBit(img, beforeHoveringEn);
        }
    }

    return false;
}

function SetSpanByFavoriteBit(cz, beforeHovering) {
    var spanEle = getSpanFavorite(cz);
    if (cz && beforeHovering) {
        spanEle.html("Smazat překlad písně z oblíbených");
    }
    else if (cz && !beforeHovering) {
        spanEle.html("Přidat překlad písně do oblíbených");
    }
    else if (!cz && beforeHovering) {
        spanEle.html("Smazat text písně z oblíbených");
    }
    else if (!cz && !beforeHovering) {
        spanEle.html("Přidat text písně do oblíbených");
    }
}//

function AfterMouseLeave(jq: JQuery.Event<HTMLElement, any>): false | JQuery.EventHandler<HTMLElement, null> | JQuery.EventHandlerBase<any, JQuery.Event<HTMLElement, null>> {
    var dd = beforeHoveringEn;
    var cz = jq.data.cz;
    if (cz) {
        dd = beforeHoveringCz;
    }
    var img = getImgFavorite(cz);
    SetImageByFavoriteBit(img, dd);

    return false;
}

function AfterMouseHover(jq: JQuery.Event<HTMLElement, any>): false | JQuery.Event<HTMLElement, any> | JQuery.EventHandlerBase<any, JQuery.Event<HTMLElement, null>> {
    var img = getImgFavorite(jq.data.cz);
    SetInverseSrc(img);

    return false;
}

function DoRate(lang, stars) {
    var idLyrics = null;
    if (lang == "EN") {
        idLyrics = su.i($("#hfIdLyricsEN"));
    }
    else {
        idLyrics = su.i($("#hfIdLyricsCZ"));
    }
    var d = su.ajaxGet4("Lyr_RatingHandler.ashx?idLyrics=" + idLyrics + "&rating=" + stars);
    su.ToStatus2(d);
}

function SetHtmlYtVideo() {
    ytVideo.hide();
    ytVideo.html("");
    ytVideo.html(GetHtmlYtVideo());
}

function showYTVideo(): any {
    ytVideo.css("display", "block");
    $("#obsah").css("height", "100%");

    return undefined;
}

function GetHtmlYtVideo(): string {
    var ytCode: string = su.t(hfYtCode);
    var idUser: string = su.t(hfYtUserId);
    return su.ajaxGet3("http://" + document.location.hostname + "/Lyr_GetHtmlYtVideoHandler.ashx?ytCode=" + encodeURIComponent(ytCode) + "&idUser=" + idUser);
}
