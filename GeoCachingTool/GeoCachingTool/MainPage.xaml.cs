using apps;
using apps.AwesomeFont;
using apps.Essential;
using apps.Helpers;
using apps.Popups;
using GeoCachingTool.Data;
using GeoCachingTool.Enums;
using HtmlAgilityPack;
using sunamo;
using sunamo.Enums;
using sunamo.LoggerAbstract;
using sunamo.Values;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UniversalWebControl;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace GeoCachingTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IEssentialMainPageWithLogin, INotifyPropertyChanged, ISunamoBrowserHost
    {
        #region Variables
        public ResourceLoader loader = new ResourceLoader();
        /// <summary>
        /// Records instances WebViews of SunamoBrowser
        /// </summary>
        Dictionary<WebView, SunamoBrowser> webViewToSunamoBrowser = new Dictionary<WebView, SunamoBrowser>();
        public const string adresaWebu = Consts.HttpSunamoCzSlash;
        /// <summary>
        /// To deserialize response from sunamo.cz
        /// </summary>
        JavascriptSerialization js = new JavascriptSerialization(SerializationLibrary.Newtonsoft);
        /// <summary>
        /// Is used by class SunamoCzLoginManager
        /// </summary>
        public LoginDialog loginDialog { get; set; }
        /// <summary>
        /// Hold login session
        /// </summary>
        SunamoCzLoginManager sunamoCzCredentials = new SunamoCzLoginManager();
        /// <summary>
        /// Uri pattern whether uri has GC code
        /// </summary>
        string[] uriPatternsListing = { "wp=", "/geocache/" };
        /// <summary>
        /// If is updating cb now
        /// </summary>
        bool updateCbOpenedTabs = false;
        int indexClosedTabItem = 0;
        YesNoDialog yesNoDialogOpenInNewTab = null;
        /// <summary>
        /// Whether simply to non prompt
        /// </summary>
        bool promptAsNever = false;
        bool lastWasAlways = false;
        /// <summary>
        /// In key has webView to easy search when sender is WebView
        /// </summary>
        public Dictionary<WebView, bool> allowPrompt = new Dictionary<WebView, bool>();
        List<string> dontOpenUriContains = null;
        HtmlDocument hd = null;
        EnterOneValue enterNameOfList = null;
        /// <summary>
        /// Is as variable to assign to ItemsSource direct in MainPage
        /// </summary>
        ObservableCollection<SelectorHelperItem> savedCachesItems = new ObservableCollection<SelectorHelperItem>();
        LogService ls = new LogService();
        public event PropertyChangedEventHandler PropertyChanged;
#if DEBUG
        public SendToLocalhostCommand cmdSendToLocalhost = null;
        public SendToSunamoCzCommand cmdSendToSunamoCz = null;
#endif
        SolidColorBrush borderBrush = new SolidColorBrush(Colors.Green);
        CheckBoxList checkBoxListCacheListingsExtended = null;
        CheckBoxList checkBoxListCacheListingsParse = null;
        Popup _popup = null;
        SunamoBrowser _webControl = null;
        SunamoDictionaryWithKeysDependencyObject<CheckBox, CacheListingExtended> checkBoxesCachesListingsExtended = null;
        SunamoDictionaryWithKeysDependencyObject<CheckBox, CacheListingParse> checkBoxesCachesListingsParse = null;
        DateTimeFileIndex dtfi = null;
        const string extCachesList = ".cl";
        internal SavedCaches savedCaches = null;
        UserControl[] otherTabItemsContent = null;
        ComboBoxHelper<TabItem> cbOpenedTabsHelper = null;
        Dictionary<TabItem, CacheListingExtended> parsedListingOfPages = new Dictionary<TabItem, CacheListingExtended>();
        /// <summary>
        /// Add all pages
        /// It would like to clean up by values in parsedHtmlOfPages
        /// </summary>
        Dictionary<SunamoBrowser, TabItem> webControlsTabItems = new Dictionary<SunamoBrowser, TabItem>();
        /// <summary>
        /// Opened tabs. It passed to cbOpenedTabs.DataContext 
        /// </summary>
        public ObservableCollection<TabItem> tabItems { get; set; }
        static MainPage instance = null;
        bool fullyInitialized = false;
        #endregion

        #region Enums variables
        /// <summary>
        /// Page which App currently showing.
        /// </summary>
        Mode mode = Mode.WebBrowser;
        SavedCachesAction savedCachesAction = SavedCachesAction.OpenExternalGeocachingSunamoCz;
        OpenedTabsAction openedTabsAction = OpenedTabsAction.None;
        #endregion

        #region Properties
        public LogServiceAbstract<Color, StorageFile> lsg
        {
            get
            {
                return ls;
            }
        }

        public static MainPage Instance
        {
            get
            {
                return instance;
            }
        }

        public bool wasOpenedNewTab
        {
            get; set;
        }

        /// <summary>
        /// Every time I change the tab I have to update this variable. It's always set even I'm not on the caching listing page.
        /// </summary>
        public SunamoBrowser webControl
        {
            get
            {
                return _webControl;
            }
            set
            {
                _webControl = value;
                OnPropertyChanged("webControl");
            }
        }

        Langs l
        {
            get
            {
                return WpfApp.l;
            }
        }

        /// <summary>
        /// Popup is just one but can contains many: checkBoxListCacheListingsExtended, yesNoDialogOpenInNewTab, enterNameOfList, checkBoxListCacheListingsParse)
        /// </summary>
        public Popup popup
        {
            get
            {
                return _popup;
            }
            set
            {
                if (_popup != null)
                {
                    _popup.IsOpen = false;
                    _popup = null;
                }
                _popup = value;
            }
        }

        CoreDispatcher cd
        {
            get
            {
                return WpfApp.cd;
            }
        }

        CoreDispatcherPriority cdp
        {
            get
            {
                return WpfApp.cdp;
            }
        }
        #endregion

        #region Selected web views
        ISunamoAppsBrowser<Control> lastOpenedISunamoAppsBrowser = null;
        public SunamoBrowser lastOpenedSunamoBrowser
        {
            get
            {
                return lastOpenedISunamoAppsBrowser as SunamoBrowser;
            }
        }

        /// <summary>
        /// Return WebView from lastOpenedISunamoAppsBrowser
        /// </summary>
        WebView lastOpenedWebView
        {
            get
            {
                if (lastOpenedISunamoAppsBrowser == null)
                {
                    return null;
                }
                return lastOpenedISunamoAppsBrowser.WebView;
            }
        }

        /// <summary>
        /// SunamoBrowser which can be in moment of controlling in background
        /// </summary>
        SunamoBrowser _lastOpenedSunamoBrowserAlsoInBackground = null;
        /// <summary>
        /// part of ISunamoBrowserHost to control webview from outside
        /// </summary>
        public SunamoBrowser lastOpenedSunamoBrowserAlsoInBackground
        {
            get
            {
                return _lastOpenedSunamoBrowserAlsoInBackground;
            }
        }

        #endregion

        #region Initialize
        /// <summary>
        /// 
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            instance = this;

            sunamo.Essential.ThisApp.Namespace = "GeoCachingTool";
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

#if DEBUG
            abbLogInToSunamoCz.Visibility = Visibility.Visible;
            abbLogOutToSunamoCz.Visibility = Visibility.Visible;
#endif

            WpfApp.cd = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
            WpfApp.tbLastErrorOrWarning = tbLastErrorOrWarning;
            WpfApp.tbLastOtherMessage = tbLastOtherMessage;
            WpfApp.mp = this;

            await Initialize();
        }

        /// <summary>
        /// In standalone method to ability restore default settings of all controls
        /// </summary>
        /// <returns></returns>
        async Task Initialize()
        {
#if DEBUG
            cmdSendToLocalhost = new SendToLocalhostCommand();
            cmdSendToSunamoCz = new SendToSunamoCzCommand();
#endif
            await AppData.CreateAppFoldersIfDontExists();
            cbOpenedTabsHelper = new ComboBoxHelper<TabItem>(cbOpenedTabs);
            await AppData.CreateAppFoldersIfDontExists();

            dtfi = new DateTimeFileIndex(AppFolders.Data, extCachesList, FileEntriesDuplicitiesStrategy.Serie, true);
            dtfi.InitComplete += Dtfi_InitComplete;
            dtfi.RaisedException += Dtfi_RaisedException;

            savedCaches = new SavedCaches(dtfi, savedCachesItems);
            tabItems = new ObservableCollection<TabItem>();
            otherTabItemsContent = new UserControl[] { savedCaches };

            savedCaches.lbSavedCachesListHelper.SelectionChangedObject += LbSavedCachesListHelper_SelectionChangedObject;
            savedCaches.lbSavedCachesListHelper.ItemRemovedObject += LbSavedCachesListHelper_ItemRemoved;

            UpdateCbOpenedTabs();

            cbOpenedTabs.SelectionChanged += tcTabs_Selected;

            await AwesomeFontControls.SetAwesomeFontSymbol(txtWebBrowser, "\uf26b");
            await AwesomeFontControls.SetAwesomeFontSymbol(txtSavedCaches, "\uf0c7");
            await AwesomeFontControls.SetAwesomeFontSymbol(txtLog, "\uf133");
            await AwesomeFontControls.SetAwesomeFontSymbol(btnCloseActualTab, "\uf00d");
            await AwesomeFontControls.SetAwesomeFontSymbolAsContent(abbCreateHtmlList, "\uf121", 20);

            gridSaveCachesContent.Children.Add(savedCaches);

            cmdCloseActualTab.CanExecuteChanged += CmdCloseActualTab_CanExecuteChanged;

            openInNewTab.OpenIn = OpenInNewTab.Never;

            string uri = "";
            uri = "https://www.geocaching.com/";
            AddTabNever(uri, true, false);
            dontOpenUriContains = new List<string>(new string[] { "geocaching.com/map/" });

            OpenListingsInGcComCommand.Set(savedCaches.lbSavedCachesList);
            cmdOpenListingsInGcCom.UpdateAssociatedControls();

            OpenPagesToLogInGcComCommand.Set(savedCaches.lbSavedCachesList);
            cmdLogVisitsInGcCom.UpdateAssociatedControls();

            CreateHtmlCommand.Set(savedCaches.lbSavedCachesList);
            cmdCreateHtml.UpdateAssociatedControls();

            await PairLoginAndPassword(false);
            EnableAppInterface();
            await WpfApp.SetStatus(TypeOfMessage.Information, loader.GetString("AppStartedSuccessfully"));

            if (!fullyInitialized)
            {
                fullyInitialized = true;
                openInNewTab.OpenIn = OpenInNewTab.Never;
                return;
            }
        }
        #endregion

        #region DateTimeFileIndex handlers
        /// <summary>
        /// Add loaded files to ListView
        /// </summary>
        /// <param name="t"></param>
        private void Dtfi_InitComplete(List<FileNameWithDateTime> t)
        {
            foreach (FileNameWithDateTime item in t)
            {
                savedCachesItems.Insert(0, GetSelectorListViewItem(item));
            }
            UpdateSavedCachesListView();
        }

        /// <summary>
        /// Write status message
        /// </summary>
        /// <param name="s"></param>
        private async void Dtfi_RaisedException(string s)
        {
            await WpfApp.SetStatus(TypeOfMessage.Error, s);
        }
        #endregion

        #region SunamoBrowserControl handlers
        /// <summary>
        /// Search term A1 on google on actual tab
        /// </summary>
        /// <param name="s"></param>
        private void sbc_NewSearchRequested(string s)
        {
            var uri = UriWebServices.GoogleSearch(s);
            AddTabNever(uri, true, false);
        }

        /// <summary>
        /// Load in actual tab or if none is selected, open in new
        /// </summary>
        /// <param name="uriOut"></param>
        private void SunamoBrowserControls_NewUriEntered(Uri uriOut)
        {
            TabItem ti = SelectedTabItem();
            if (ti != null)
            {
                ti.Header = CA.GetTextAfterIfContainsPattern(uriOut.ToString(), loader.GetString("Loading"), uriPatternsListing);
                (ti.Content as SunamoBrowser).WebView.Navigate(uriOut);
            }
            else
            {
                tabItems.Clear();
                AddTabNever(uriOut.ToString(), true, false);
            }
        }

        /// <summary>
        /// Go back, if available
        /// </summary>
        private void sbc_BackButtonClick()
        {
            allowPrompt[_webControl.WebView] = true;
            promptAsNever = true;
            lastOpenedSunamoBrowser.cmdGoBackCommand.Execute(null);
            promptAsNever = false;
        }

        /// <summary>
        /// Go forward, if available
        /// </summary>
        private void sbc_NextButtonClick()
        {
            allowPrompt[_webControl.WebView] = true;
            promptAsNever = true;
            lastOpenedSunamoBrowser.cmdGoForwardCommand.Execute(null);
            promptAsNever = false;
        }

        /// <summary>
        /// Reload, if available
        /// </summary>
        private void sbc_ReloadButtonClick()
        {
            allowPrompt[_webControl.WebView] = true;
            promptAsNever = true;
            lastOpenedSunamoBrowser.cmdReloadCommand.Execute(null);
            promptAsNever = false;
        }

        /// <summary>
        /// Stop loading, if available
        /// </summary>
        private async void sbc_StopButtonClick()
        {
            lastOpenedSunamoBrowser.IsNavigating = false;
            lastOpenedSunamoBrowser.cmdStopLoadingCommand.Execute(null);
            await WebViewStateChanged(lastOpenedSunamoBrowser);
        }
        #endregion

        #region Helpers UI method
        /// <summary>
        /// Load credential to sunamo.cz from file and show dialog if A1. Anyway log in.
        /// </summary>
        /// <param name="zobrazitLoginDialog"></param>
        /// <returns></returns>
        private async Task PairLoginAndPassword(bool zobrazitLoginDialog)
        {
            // Load from files
            await LoginDialog.GetLoginAndPassword(RandomHelper.RandomStringWithoutSpecial(10));

            await sunamoCzCredentials.PairLoginAndPassword(this, zobrazitLoginDialog, loginDialog.Login, loginDialog.Password, borderBrush, js, adresaWebu);
        }

        /// <summary>
        /// A1 whether user is logined to sunamo.cz
        /// </summary>
        /// <param name="b"></param>
        void EnableAppInterface(bool b)
        {
            Visibility vc = Visibility.Collapsed;

            WpfApp.SetVisibility(abbOpenedListingsToNormalBrowser, vc);
            WpfApp.SetVisibility(abbOpenTabsToSavedList, vc);

            WpfApp.SetVisibility(abbLogVisitsInGcCom, vc);
            WpfApp.SetVisibility(abbOpenListingsInGcCom, vc);
            WpfApp.SetVisibility(abbCreateHtmlList, vc);

            if (mode == Mode.WebBrowser)
            {
                vc = Visibility.Visible;
                WpfApp.SetVisibility(abbOpenedListingsToNormalBrowser, vc);
                WpfApp.SetVisibility(abbOpenTabsToSavedList, vc);
            }
            else if (mode == Mode.SavedCaches)
            {
                vc = Visibility.Visible;
                WpfApp.SetVisibility(abbLogVisitsInGcCom, vc);
                WpfApp.SetVisibility(abbOpenListingsInGcCom, vc);
                WpfApp.SetVisibility(abbCreateHtmlList, vc);
            }

            if (b)
            {
                abbLogInToSunamoCz.Visibility = Visibility.Collapsed;
                abbLogOutToSunamoCz.Visibility = Visibility.Visible;
            }
            else
            {
                sunamoCzCredentials.idUser = -1;
                abbLogInToSunamoCz.Visibility = Visibility.Visible;
                abbLogOutToSunamoCz.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Collapse all controls and make visible just A1
        /// </summary>
        /// <param name="gridSearch"></param>
        /// <returns></returns>
        private async Task ShowGridInSplitViewContent(Grid gridSearch)
        {
            foreach (UIElement item in ((Grid)MySplitView.Content).Children)
            {
                WpfApp.SetVisibility(item, Visibility.Collapsed);
            }
            WpfApp.SetVisibility(gridSearch, Visibility.Visible);
        }

        /// <summary>
        /// Get data class for SelectorHelperListViewUC - here two 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private SelectorHelperItem GetSelectorListViewItem(FileNameWithDateTime item)
        {
            return new SelectorHelperItem(savedCaches.lbSavedCachesListHelper, Visibility.Collapsed, Visibility.Collapsed, Visibility.Visible, item.Row1, item.Row2, item, "\uf00d", "\uf016", "\uf0c7");
        }

        /// <summary>
        /// Set IsEnabled for controls which are dependent on login to sunamo.cz
        /// </summary>
        private void EnableAppInterface()
        {
            EnableAppInterface(sunamoCzCredentials.IsUserLogined());
        }

        void OnPropertyChanged(string pn)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(pn));
            }
        }
        #endregion

        #region WebView
        /// <summary>
        /// Return if A1 is showing now
        /// </summary>
        /// <param name="vw"></param>
        /// <returns></returns>
        public bool IsThisVisibleWebView(WebView vw)
        {
            return SelectedWebView() == vw;
        }

        /// <summary>
        /// Return WebView of actual showing
        /// </summary>
        /// <returns></returns>
        public WebView SelectedWebView()
        {
            return _webControl.WebView;
        }

        /// <summary>
        /// A2 can be null
        /// A4 whether open in same tab also with Prompt without user asking
        /// A5 whether show dialog "Open as new tab"
        /// </summary>
        /// <param name="wv"></param>
        /// <param name="args"></param>
        /// <param name="uri"></param>
        /// <param name="promptAsNever"></param>
        /// <param name="showPromptDialog"></param>
        /// <returns></returns>
        private async Task ForceOpenPage(WebView wv, WebViewNavigationStartingEventArgs args, Uri uri, bool promptAsNever, bool showPromptDialog)
        {
            if (args != null)
            {
                if (args.Uri == null)
                {
                    return;
                }
            }
            else
            {
                wv.Navigate(uri);
                return;
            }

            await WebViewStateChanged(lastOpenedSunamoBrowser);
            var openIn = openInNewTab.OpenIn;

            if (openIn == OpenInNewTab.Prompt || promptAsNever)
            {
                if (promptAsNever)
                {
                    await NeverOpenInNewTab(uri);
                }
                else
                {
                    if (yesNoDialogOpenInNewTab == null)
                    {
                        if (openIn == OpenInNewTab.Prompt)
                        {
                            if (wv != _webControl.WebView)
                            {
                                if (args != null)
                                {
                                    args.Cancel = true;
                                }
                            }
                        }

                        if (showPromptDialog && args != null)
                        {
                            yesNoDialogOpenInNewTab = null;
                            yesNoDialogOpenInNewTab = new YesNoDialog(loader.GetString("PromptNewTab"), args.Uri);
                            yesNoDialogOpenInNewTab.ClickOK += YesNoDialogOpenInNewTab_ClickOK;
                            yesNoDialogOpenInNewTab.ClickCancel += YesNoDialogOpenInNewTab_ClickCancel;
                            popup = PopupHelper.GetPopupResponsive(yesNoDialogOpenInNewTab, true, borderBrush);
                        }
                    }
                }
            }
            else if (openIn == OpenInNewTab.Always)
            {
                string uriS = "";
                if (args != null)
                {
                    uriS = args.Uri.ToString();
                }
                else
                {
                    uriS = uri.ToString();
                }
                bool b = AlreadyOpened(uriS);
                if (b)
                {

                    return;
                }
                if (await CanOpenNewTab(uriS))
                {
                    if (args != null)
                    {
                        args.Cancel = true;
                    }
                    AddTab(uriS, false, false);
                }
            }
            else
            {
                await NeverOpenInNewTab(uri);
            }
        }

        /// <summary>
        /// Return whether cache listing are already opened
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private bool AlreadyOpened(string uri)
        {
            var v = AllCacheListingsAsGcCodesList();
            var urio = new Uri(uri);

            foreach (var item in v)
            {
                if (GetGcCodeFromUri(urio) == item)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Cast A1 to SunamoBrowser
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private SunamoBrowser SunamoBrowserFromTabItem(TabItem item)
        {
            return SelectedTabItem().Content as SunamoBrowser;
        }

        /// <summary>
        /// Return A1 or string.Empty
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string SourceOfWebView(Uri source)
        {
            if (source == null)
            {
                return "";
            }
            return source.ToString();
        }

        /// <summary>
        /// Whether some SunamoBrowser is navigating
        /// </summary>
        /// <returns></returns>
        public bool IsSomeWebViewNavigating()
        {
            foreach (var sunamoBrowser in AllCacheListingsAsSunamoBrowserList())
            {
                bool CanExecute = sunamoBrowser.IsNavigating;
                if (CanExecute)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Is fired when loading was completed or failed
        /// Set IsEnabled for SunamoBrowserControl buttons and update text in tab title
        /// </summary>
        /// <param name="wvc"></param>
        /// <returns></returns>
        private async Task WebViewStateChanged(SunamoBrowser wvc)
        {
            sbc.btnBack.IsEnabled = await UniversalWebControl.GoBackCommand.Set(wvc);
            sbc.btnNext.IsEnabled = await UniversalWebControl.GoForwardCommand.Set(wvc);
            sbc.btnReload.IsEnabled = await UniversalWebControl.ReloadCommand.Set(wvc);
            sbc.btnStopLoading.IsEnabled = await UniversalWebControl.StopLoadingCommand.Set(wvc);

            if (webControlsTabItems.ContainsKey(wvc))
            {
                var ti = webControlsTabItems[wvc];
                sbc.SetUri(wvc.WebView.Source.ToString());
                UpdateInCbOpenedTabsByTitle(ti, await wvc.GetHtmlDocument());
            }
        }

        /// <summary>
        /// Create instance of SunamoBrowser, add as Child into MainPage, set handlers for sbc commands and render engine
        /// </summary>
        /// <returns></returns>
        private SunamoBrowser CreateInstanceWebViewHost()
        {
            var sunamoBrowser = new SunamoBrowser();
            sunamoBrowser.sbHost = this;
            Grid.SetColumnSpan(sunamoBrowser, 2);

            sunamoBrowser.cmdGoBackCommand.CanExecuteChanged += CmdGoBackCommand_CanExecuteChanged;
            sunamoBrowser.cmdGoForwardCommand.CanExecuteChanged += CmdGoForwardCommand_CanExecuteChanged;
            sunamoBrowser.cmdReloadCommand.CanExecuteChanged += CmdReloadCommand_CanExecuteChanged;
            sunamoBrowser.cmdStopLoadingCommand.CanExecuteChanged += CmdStopLoadingCommand_CanExecuteChanged;

            var aweView = sunamoBrowser.WebView;
            aweView.NavigationStarting += AweView_NavigationStarting;
            aweView.NavigationFailed += AweView_NavigationFailed;
            aweView.NewWindowRequested += AweView_NewWindowRequested;
            aweView.HorizontalAlignment = HorizontalAlignment.Stretch;
            aweView.VerticalAlignment = VerticalAlignment.Stretch;
            aweView.NavigationCompleted += AweView_NavigationCompleted;

            return sunamoBrowser;
        }
        #endregion


        #region Tab management
        /// <summary>
        /// Open A1 in actually displayed SunamoBrowser
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="setAsSelected"></param>
        /// <param name="setStatusNewTab"></param>
        private void AddTabNever(string uri, bool setAsSelected, bool setStatusNewTab)
        {
            OpenInNewTab openInNewTabOld = openInNewTab.OpenIn;
            openInNewTab.OpenIn = OpenInNewTab.Never;
            AddTab(uri, setAsSelected, setStatusNewTab);
            openInNewTab.OpenIn = openInNewTabOld;
        }

        /// <summary>
        /// Replace items by tabItems and try set previously selected 
        /// </summary>
        private void UpdateCbOpenedTabs()
        {
            updateCbOpenedTabs = true;
            object lo = cbOpenedTabs.SelectedItem;
            cbOpenedTabs.DataContext = null;
            cbOpenedTabs.DataContext = tabItems;
            cbOpenedTabs.SelectedItem = lo;
            updateCbOpenedTabs = false;
        }

        /// <summary>
        /// Set updated header of TabItem
        /// </summary>
        /// <param name="ti"></param>
        /// <param name="htmlDocument"></param>
        private void UpdateInCbOpenedTabsByTitle(TabItem ti, HtmlDocument htmlDocument)
        {
            ti.Header = HtmlParserS.Title(htmlDocument.DocumentNode);
        }

        /// <summary>
        /// Close actual showing tab
        /// </summary>
        /// <returns></returns>
        public async Task CloseActualTabItem()
        {
            TabItem ti = SelectedTabItem();
            indexClosedTabItem = tabItems.IndexOf(ti);
            parsedListingOfPages.Remove(ti);
            tabItems.Remove(ti);
            UpdateCbOpenedTabs();
            // Update close button - maybe was closed last tab
            CloseActualTabCommand.Set(lastOpenedSunamoBrowser);
        }

        /// <summary>
        /// A3 if set status
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="setAsSelected"></param>
        /// <param name="setStatusNewTab"></param>
        public async void AddTab(string uri, bool setAsSelected, bool setStatusNewTab)
        {
            string header = CA.GetTextAfterIfContainsPattern(uri, loader.GetString("Loading"), uriPatternsListing);
            if (header != null)
            {
                Uri urio = new Uri(uri);
                wasOpenedNewTab = true;
                var aweView = CreateInstanceWebViewHost();
                _lastOpenedSunamoBrowserAlsoInBackground = aweView;

                bool wasNull = false;
                if (lastOpenedISunamoAppsBrowser == null)
                {
                    lastOpenedISunamoAppsBrowser = aweView;

                    wasNull = true;
                }

                TabItem ti = null;
                ti = await AddTab(header, aweView, setAsSelected, setStatusNewTab);

                string gcCode = GetGcCodeFromUri(urio);
                if (gcCode != "")
                {
                    allowPrompt.Add(aweView.WebView, true);
                }
                aweView.WebView.Navigate(urio);

                webViewToSunamoBrowser.Add(aweView.WebView, aweView);
                webControlsTabItems.Add(aweView, ti);

                if (wasNull)
                {
                    if (ItemsControlHelper.HasIndexWithoutException(0, cbOpenedTabs.Items))
                    {
                        cbOpenedTabs.SelectedIndex = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Method can called only by AddTab(string, bool, bool)
        /// A4 if set status
        /// </summary>
        /// <param name="header"></param>
        /// <param name="content"></param>
        /// <param name="setAsSelected"></param>
        /// <returns></returns>
        private async Task<TabItem> AddTab(string header, object content, bool setAsSelected, bool setStatusNewTab)
        {
            TabItem ti = new TabItem();
            ti.Header = header;
            ti.Content = content;
            tabItems.Add(ti);
            wasOpenedNewTab = true;
            CloseActualTabCommand.Set(lastOpenedSunamoBrowser);

            if (setStatusNewTab)
            {
                await WpfApp.SetStatus(TypeOfMessage.Information, "Was opened new tab");
            }

            if (setAsSelected)
            {
                cbOpenedTabs.SelectedIndex = tabItems.Count - 1;
                await CbOpenedTabs_SelectionChanged();
            }
            else
            {
                int tic = tabItems.Count;
                if (tic == 1)
                {
                    cbOpenedTabs.SelectedIndex = 0;
                }
            }

            btnCloseActualTab.IsEnabled = cmdCloseActualTab.CanExecute(null);

            return ti;
        }

        /// <summary>
        /// Return cbOpenedTabs.SelectedItem
        /// </summary>
        /// <returns></returns>
        private TabItem SelectedTabItem()
        {
            return cbOpenedTabs.SelectedItem as TabItem;
        }

        /// <summary>
        /// Return if contains other than listings caches
        /// </summary>
        /// <returns></returns>
        public bool IsSelectedOtherTabItem()
        {
            TabItem ti = SelectedTabItem();
            if (CA.IsEqualToAnyElement<object>(ti.Content, otherTabItemsContent))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// If uri is already opened or has denied pattern, return false. Otherwise true
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task<bool> CanOpenNewTab(string uri)
        {
            bool b = AlreadyOpened(uri);
            if (b)
            {
                return false;
            }
            if (dontOpenUriContains == null)
            {
                return true;
            }
            foreach (var item in dontOpenUriContains)
            {
                if (uri.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Open A1 in actually displayed SunamoBrowserControl
        /// </summary>
        /// <param name="Uri"></param>
        /// <returns></returns>
        private async Task NeverOpenInNewTab(Uri Uri)
        {
            if (Uri != null)
            {
                sbc.SetUri(Uri.ToString());
                // The user wishes to open bookmarks in the same tab, do nothing
            }
        }
        #endregion

        #region AweView handlers
        /// <summary>
        /// Check before open obligatory conditions - logined to geocaching.com and valid uri format
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AweView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs e)
        {
            Uri yourtargetedurl = e.Uri;
            if (yourtargetedurl != null)
            {
                string targerUriString = yourtargetedurl.ToString();
                if (targerUriString.Contains(":") && !targerUriString.Contains("://"))
                {
                    e.Handled = true;
                    return;
                }

                if (!IsLoginedToGeoCachingCom(targerUriString))
                {
                    e.Handled = true;
                    return;
                }

                AddTab(targerUriString, false, true);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void AweView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            await WpfApp.SetStatus(TypeOfMessage.Warning, loader.GetString("NavigationCompleted"));
            if (lastOpenedISunamoAppsBrowser == null)
            {
                return;
            }
            //Enable buttons to upload to scz/localhost
#if DEBUG
            SendToLocalhostCommand.Set();
            SendToSunamoCzCommand.Set();
#endif
           
                if (sender == lastOpenedISunamoAppsBrowser.WebView)
                {
                    await WebViewStateChanged(lastOpenedSunamoBrowser);
                }
            
        }

        private async void AweView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            await WpfApp.SetStatus(TypeOfMessage.Warning, loader.GetString("NavigationFailed"));
            if (lastOpenedISunamoAppsBrowser == null)
            {
                return;
            }
#if DEBUG
            SendToLocalhostCommand.Set();
            SendToSunamoCzCommand.Set();
#endif
           
                if (sender == lastOpenedISunamoAppsBrowser.WebView)
                {
                    await WebViewStateChanged(lastOpenedSunamoBrowser);
                }
            
        }

        private async void AweView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (lastOpenedISunamoAppsBrowser == null)
            {
                return;
            }

            bool showPromptDialog = false;
            if (args.Uri == null)
            {
                return;
            }

            string uriS = args.Uri.ToString();
            if (!IsLoginedToGeoCachingCom(uriS))
            {
                await WpfApp.SetStatus(TypeOfMessage.Warning, loader.GetString("NavigationTo") + uriS + loader.GetString("FailedWithErrorColon") + loader.GetString("NotLoginedToGeocachingCom"));
                args.Cancel = true;
                LogInToGeoCachingCom();
                return;
            }

            if (openInNewTab.OpenIn == OpenInNewTab.Always)
            {
                lastWasAlways = true;
                if (sender == _webControl.WebView)
                {
                    args.Cancel = true;
                }
            }
            else if (openInNewTab.OpenIn == OpenInNewTab.Prompt)
            {
                // If allowPrompt dont contains or has true value in allowPrompt
                bool con = true;
                bool containsAuri = false;
                string auri = sender.Source.ToString();

                #region When uri contains something from uriPatternsListing, set containsAuri to true
                foreach (var item2 in uriPatternsListing)
                {
                    if (auri.Contains(item2))
                    {
                        containsAuri = true;
                        break;
                    }
                }
                #endregion

                // set con to true when i actual WebView allowed to prompt
                con = !allowPrompt.ContainsKey(sender);
                if (!con)
                {
                    con = allowPrompt[sender];
                }

                // If allowPrompt dont contains and !containsAuri
                if (con && !containsAuri)
                {
                    allowPrompt[sender] = true;

                    containsAuri = false;
                    auri = args.Uri.ToString();

                    foreach (var item2 in uriPatternsListing)
                    {
                        if (auri.Contains(item2))
                        {
                            containsAuri = true;
                            break;
                        }
                    }

                    if (containsAuri)
                    {
                        allowPrompt[sender] = false;
                        if ((auri.Contains("geocaching.com/geocache/") && !auri.Contains("_")))
                        {
                            showPromptDialog = true;
                            args.Cancel = true;
                        }
                    }
                }
            }

            if (sender == lastOpenedISunamoAppsBrowser.WebView)
            {
                if (lastWasAlways)
                {
                    lastWasAlways = false;
                }
                await ForceOpenPage(sender, args, args.Uri, promptAsNever, showPromptDialog);
            }

#if DEBUG
            // Will be called again after failed or completed
            SendToLocalhostCommand.Set();
            SendToSunamoCzCommand.Set();
#endif
        }
        #endregion

        #region Commands handlers
        private void CmdCloseActualTab_CanExecuteChanged(object o, EventArgs e)
        {
            btnCloseActualTab.IsEnabled = sunamo.BT.GetValueOfNullable(CloseActualTabCommand.previousCanExecute);
        }

        private void CmdStopLoadingCommand_CanExecuteChanged(object o, bool b)
        {
            // Compare due to loading is processed in background
            if (o == lastOpenedWebView)
            {
                sbc.btnStopLoading.IsEnabled = b;
            }
        }

        private void CmdReloadCommand_CanExecuteChanged(object o, bool b)
        {
            if (o == lastOpenedWebView)
            {
                sbc.btnReload.IsEnabled = b;
            }
        }

        private void CmdGoForwardCommand_CanExecuteChanged(object o, bool b)
        {
            if (o == lastOpenedWebView)
            {
                sbc.btnNext.IsEnabled = b;
            }
        }

        private void CmdGoBackCommand_CanExecuteChanged(object o, bool b)
        {
            if (o == lastOpenedWebView)
            {
                sbc.btnBack.IsEnabled = b;
            }
        }

        #endregion

        #region Cache management
        /// <summary>
        /// Return all opened tabs
        /// </summary>
        /// <returns></returns>
        public List<SunamoBrowser> AllCacheListingsAsSunamoBrowserList()
        {
            List<SunamoBrowser> vr = new List<SunamoBrowser>();
            if (tabItems != null)
            {
                foreach (var item in tabItems)
                {
                    var sunamoBrowser = SunamoBrowserFromTabItem(item);

                    bool containsAuri = false;
                    string auri = SourceOfWebView(sunamoBrowser.Source);

                    foreach (var item2 in uriPatternsListing)
                    {
                        if (auri.Contains(item2))
                        {
                            containsAuri = true;
                            break;
                        }
                    }

                    if (containsAuri)
                    {
                        vr.Add(sunamoBrowser);
                    }
                }
            }
            return vr;
        }

        /// <summary>
        /// Set new items from savedCachesItems to ListView
        /// </summary>
        private void UpdateSavedCachesListView()
        {
            savedCaches.lbSavedCachesList.lv.ItemsSource = null;
            savedCaches.lbSavedCachesList.lv.ItemsSource = savedCachesItems;
        }

        /// <summary>
        /// Return GcCode loaded caches
        /// </summary>
        /// <returns></returns>
        public List<string> AllCacheListingsAsGcCodesList()
        {
            List<string> vr = new List<string>();
            if (tabItems != null)
            {
                foreach (var item in tabItems)
                {
                    var sunamoBrowser = SunamoBrowserFromTabItem(item);

                    bool containsAuri = false;
                    string auri = SourceOfWebView(sunamoBrowser.Source);

                    foreach (var item2 in uriPatternsListing)
                    {
                        if (auri.Contains(item2))
                        {
                            containsAuri = true;
                            break;
                        }
                    }

                    if (containsAuri)
                    {
                        string ret = GetGcCodeFromUri(sunamoBrowser.Source);
                        if (ret != "")
                        {
                            vr.Add(ret);
                        }
                    }
                }
            }

            return vr;
        }
        #endregion

        #region Working with remote
        /// <summary>
        /// Show login page
        /// </summary>
        internal async void LogInToGeoCachingCom()
        {
            await ForceOpenPage(SelectedWebView(), null, new Uri("https://www.geocaching.com/login/default.aspx?RESETCOMPLETE=Y"), true, false);
        }

#if DEBUG
        public async Task SendToWSOpenedTabs(bool localhost)
        {
            if (localhost)
            {
                openedTabsAction = OpenedTabsAction.SendToLocalhost;
            }
            else
            {
                openedTabsAction = OpenedTabsAction.SendToSunamoCz;
            }

            await AskForOpenedTabs();
        }
#endif

        /// <summary>
        /// Return string.Empty in case of fail
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private string GetGcCodeFromUri(Uri source)
        {
            string gcCode = "";
            string uris = source.ToString();
            if (uris.Contains(uriPatternsListing[0]))
            {
                uris = uris.TrimEnd('/');
                string u = uris.Substring(uris.LastIndexOf('/') + 1);

                if (u.Contains("_"))
                {
                    string nameCache;
                    SH.GetPartsByLocation(out gcCode, out nameCache, u, '_');
                }
                else
                {
                    gcCode = u;
                }
            }
            else if (uris.Contains(uriPatternsListing[1]))
            {
                gcCode = QSHelper.GetParameter(uris, "wp");
            }
            gcCode = gcCode.ToUpper();
            if (gcCode.StartsWith("GC"))
            {
                return gcCode;
            }
            return "";
        }

        private bool IsLoginedToGeoCachingCom(string targerUriString)
        {
            bool containsAuri = false;

            // Uri which is not pattern can be use without login to GeoCaching.com
            foreach (var item2 in uriPatternsListing)
            {
                if (targerUriString.Contains(item2))
                {
                    containsAuri = true;
                    break;
                }
            }

            if (containsAuri)
            {
                if (cmdLogInToGeoCachingCom.CanExecute(null))
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> SendToWS(bool localhost, CacheListingExtended l)
        {
            if (localhost)
            {
                //LocalhostWS.ProcessHtmlRequest req = new LocalhostWS.ProcessHtmlRequest();
                LocalhostWS.Geo_ProcessHtmlSoapClient sc = new LocalhostWS.Geo_ProcessHtmlSoapClient();
                await WpfApp.SetStatus(TypeOfMessage.Information, await sc.ProcessHtmlAsync(new LocalhostWS.ExternalLoginResult { IdUser = sunamoCzCredentials.idUser, Sc = sunamoCzCredentials.sc }, l.disabled, l.gcCode, WebUtility.UrlEncode(l.listingShort + l.listing), l.coords,
                    l.guid, l.hiddenDT, l.hint, l.cacheName, l.size, l.difficulty, l.terrain, l.cacheType, l.cacheAuthorProfile,
                    l.cacheAuthor, l.wp2));
                return true;
            }
            else
            {
                SunamoCzWS.Geo_ProcessHtmlSoapClient sc = new SunamoCzWS.Geo_ProcessHtmlSoapClient();
                await WpfApp.SetStatus(TypeOfMessage.Information, await sc.ProcessHtmlAsync(new SunamoCzWS.ExternalLoginResult { IdUser = sunamoCzCredentials.idUser, Sc = sunamoCzCredentials.sc }, l.disabled, l.gcCode, WebUtility.UrlEncode(l.listingShort + l.listing), l.coords,
                    l.guid, l.hiddenDT, l.hint, l.cacheName, l.size, l.difficulty, l.terrain, l.cacheType, l.cacheAuthorProfile,
                    l.cacheAuthor, l.wp2));
                return true;
            }
        }
        #endregion

        #region Control handlers
        #region Dialog handlers
        public async Task<bool> Dialog_ClickOK(object sender, RoutedEventArgs e)
        {
            await PairLoginAndPassword(false);
            if (sunamoCzCredentials.IsUserLogined())
            {
                popup.IsOpen = false;
                VisualTreeHelper.DisconnectChildrenRecursive(popup);
                bool vr = true;
                EnableAppInterface(vr);
                await WpfApp.SetStatus(TypeOfMessage.Success, loader.GetString("SuccessfulLoginToSunamoCz"));
                return vr;
            }
            return false;
        }

        public void Dialog_ClickCancel(object sender, RoutedEventArgs e)
        {
            Dialog_ClickClose(sender, e);
        }

        public void Dialog_ClickClose(object sender, RoutedEventArgs e)
        {
            sunamoCzCredentials.sc = "";
            sunamoCzCredentials.idUser = -1;
            popup.IsOpen = false;
            VisualTreeHelper.DisconnectChildrenRecursive(popup);
            EnableAppInterface();
        }
        #endregion

        #region AppBarButtons handler
        private async void abbLogInToSunamoCz_Click(object sender, RoutedEventArgs e)
        {
            await PairLoginAndPassword(true);
        }

        private async void abbOpenedListingsToNormalBrowser_Click(object sender, RoutedEventArgs e)
        {
            openedTabsAction = OpenedTabsAction.OpenInMainBrowser;
            await AskForOpenedTabs();
        }
        #endregion

        private async void btnLogInNav_Click(object sender, RoutedEventArgs e)
        {
            mode = Mode.Log;
            await ShowGridInSplitViewContent(gridLog);
            EnableAppInterface();
        }

        private async void btnSavedCachesNav_Click(object sender, RoutedEventArgs e)
        {
            mode = Mode.SavedCaches;
            await ShowGridInSplitViewContent(gridSaveCaches);
            EnableAppInterface();
        }

        private async void btnWebBrowserNav_Click(object sender, RoutedEventArgs e)
        {
            mode = Mode.WebBrowser;
            await ShowGridInSplitViewContent(gridWebBrowser);
            EnableAppInterface();
        }

        private async void CbOpenedTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (updateCbOpenedTabs == false)
            {
                await CbOpenedTabs_SelectionChanged();
            }
        }

        private void LbSavedCachesListHelper_ItemRemoved(object o)
        {
            FileNameWithDateTime fnwdt = (FileNameWithDateTime)o;
            dtfi.DeleteFile(fnwdt);
        }

        #region MenuItems handlers
        public void miOpenSavedListInGeocachingSunamoCz_Click()
        {
            savedCachesAction = SavedCachesAction.OpenExternalGeocachingSunamoCz;
            StartOpenSavedList();
        }

        public async Task miOpenedTabsToList_Click()
        {
            if (parsedListingOfPages.Count != 0)
            {
                checkBoxesCachesListingsExtended = new SunamoDictionaryWithKeysDependencyObject<CheckBox, CacheListingExtended>();
                checkBoxListCacheListingsExtended = new CheckBoxList();
                checkBoxListCacheListingsExtended.ClickOK += CheckBoxList_ClickOK;
                checkBoxListCacheListingsExtended.ClickCancel += CheckBoxList_ClickCancel;

                List<string> addedGcCodes = new List<string>();

                foreach (var item in parsedListingOfPages)
                {
                    string gcCode = item.Value.gcCode;
                    if (!addedGcCodes.Contains(gcCode))
                    {
                        addedGcCodes.Add(gcCode);
                        CheckBox chb = CheckBoxHelper.Get(TextWrapping.NoWrap, item.Value.MostImportant());

                        checkBoxesCachesListingsExtended.Add(chb, item.Value);
                        checkBoxListCacheListingsExtended.AddCheckBox(chb);
                    }
                }
                popup = PopupHelper.GetPopupResponsive(checkBoxListCacheListingsExtended, true, borderBrush);
            }
            else
            {
                await WpfApp.SetStatus(TypeOfMessage.Information, loader.GetString("NoCacheListingInTabs"));
            }
        }

        public void miOpenSavedListInGeocachingCom_Click()
        {
            savedCachesAction = SavedCachesAction.OpenExternalGeocachingCom;
            StartOpenSavedList();
        }

        public void miOpenLogPageExternalInGeoCachingCom_Click()
        {
            savedCachesAction = SavedCachesAction.OpenExternalGeoCachingComLogPage;
            StartOpenSavedList();
        }

        public void miCreateHtml_Click()
        {
            savedCachesAction = SavedCachesAction.CreateHtmlList;
            StartOpenSavedList();
        }

        private async void miOpenedTabsToList_Click(object sender, RoutedEventArgs e)
        {
            await miOpenedTabsToList_Click();
        }
        #endregion

        private async void CheckBoxListWithCheckBoxCacheListingsParse_ClickOK(object sender)
        {
            popup.IsOpen = false;
            var s = checkBoxesCachesListingsParse.GetValuesByValuesOfKeysProperty<bool?>(CheckBox.IsCheckedProperty, true);

            int countCustomControls = checkBoxListCacheListingsParse.spCustomControls.Children.Count;

            UIElement cc1 = null;
            if (countCustomControls > 0)
            {
                cc1 = checkBoxListCacheListingsParse.spCustomControls.Children[0];
            }

            bool withLinks = false;

            if (cc1 is CheckBox)
            {
                withLinks = sunamo.BT.GetValueOfNullable((cc1 as CheckBox).IsChecked);
            }

            HtmlGenerator hg = null;

            if (savedCachesAction == SavedCachesAction.CreateHtmlList)
            {
                hg = new HtmlGenerator();
                hg.WriteTag("ol");
            }

            foreach (var item in s)
            {
                if (savedCachesAction == SavedCachesAction.CreateHtmlList)
                {
                    hg.WriteTag("li");
                    hg.WriteRaw(item.MostImportantList(false, withLinks));
                    hg.TerminateTag("li");
                }
                if (savedCachesAction == SavedCachesAction.OpenExternalGeocachingSunamoCz)
                {
                    await Launcher.LaunchUriAsync(new Uri("http://www.sunamo.cz/geocaching/CacheDetails.aspx?wp=" + item.gcCode));
                }
                else if (savedCachesAction == SavedCachesAction.OpenExternalGeocachingCom)
                {
                    await Launcher.LaunchUriAsync(new Uri(GeoCachingComSite.CacheDetailsWp(item.gcCode)));
                }
                else if (savedCachesAction == SavedCachesAction.OpenExternalGeoCachingComLogPage)
                {
                    await Launcher.LaunchUriAsync(new Uri(GeoCachingComSite.LogWp(item.gcCode)));
                }
            }

            if (savedCachesAction == SavedCachesAction.CreateHtmlList)
            {
                hg.TerminateTag("ol");

                ClipboardHelper.SetText(hg.ToString().Trim());

                await WpfApp.SetStatus(TypeOfMessage.Success, loader.GetString("HtmlListSuccessfullyCreated"));
            }

            checkBoxListCacheListingsExtended = null;

            checkBoxListCacheListingsParse = null;
            popup = null;
            checkBoxesCachesListingsParse = null;
        }

        /// <summary>
        /// Open dialog about app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void abbAboutApp_Click(object sender, RoutedEventArgs e)
        {
            AboutApp ap = new AboutApp();
            ap.ClickOK += Ap_ClickOK;

            popup = PopupHelper.GetPopupResponsive(ap, true, borderBrush);
        }

        /// <summary>
        /// Close dialog About App
        /// </summary>
        /// <param name="t"></param>
        private void Ap_ClickOK(object t)
        {
            Dialog_ClickClose(null, null);
        }

        #region YesNoDialogOpenInNewTab handlers
        private async void YesNoDialogOpenInNewTab_ClickCancel(YesNoDialogEventArgs args)
        {
            popup.IsOpen = false;

            promptAsNever = true;
            string uri = args.Arg.ToString();

            if (await CanOpenNewTab(uri))
            {
                lastOpenedWebView.Navigate(new Uri(uri));
                await NeverOpenInNewTab(new Uri(uri));
            }

            yesNoDialogOpenInNewTab = null;
            popup = null;
            promptAsNever = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        private async void YesNoDialogOpenInNewTab_ClickOK(YesNoDialogEventArgs args)
        {
            popup.IsOpen = false;
            string uri = args.Arg.ToString();
            if (await CanOpenNewTab(uri))
            {
                bool containsAuri = false;

                foreach (var item in uriPatternsListing)
                {
                    if (uri.Contains(item))
                    {
                        containsAuri = true;
                        break;
                    }
                }

                if (containsAuri)
                {
                    if (allowPrompt.ContainsKey(_webControl.WebView))
                    {
                        allowPrompt[_webControl.WebView] = true;
                    }
                    else
                    {
                        allowPrompt.Add(_webControl.WebView, true);
                    }
                }
                AddTab(uri, false, true);
            }

            yesNoDialogOpenInNewTab = null;
            popup = null;
        }
        #endregion

        /// <summary>
        /// Delete from layout lastOpenedISunamoAppsBrowser and set another SunamoBrowser
        /// </summary>
        /// <returns></returns>
        private async Task CbOpenedTabs_SelectionChanged()
        {
            TabItem ti = (cbOpenedTabs.SelectedItem as TabItem);
            if (ti != null)
            {
                SunamoBrowser wvc = ti.Content as SunamoBrowser;
                if (wvc != null)
                {
                    if (lastOpenedISunamoAppsBrowser != null)
                    {
                        gridWebBrowser.Children.Remove((UIElement)lastOpenedISunamoAppsBrowser);
                    }

                    CloseActualTabCommand.Set(wvc);

                    FrameworkElement fe = (FrameworkElement)wvc;
                    lastOpenedISunamoAppsBrowser = wvc;
                    gridWebBrowser.Children.Add(fe);
                    Grid.SetRow(fe, 2);

                    await WebViewStateChanged(wvc);
                }
            }
        }

        /// <summary>
        /// After select caches - do with them action
        /// </summary>
        /// <param name="sender"></param>
        private async void CheckBoxListCacheListingsExtended_ClickOK(object sender)
        {
            popup.IsOpen = false;
            var extended = checkBoxesCachesListingsExtended.GetValuesByValuesOfKeysProperty<bool?>(CheckBox.IsCheckedProperty, true);
            object o = new object();

            List<bool> b = new List<bool>();

            foreach (var item in extended)
            {
                if (openedTabsAction == OpenedTabsAction.OpenInMainBrowser)
                {
                    b.Add(await Launcher.LaunchUriAsync(new Uri(item.GcComUri)));
                }
#if DEBUG
                else if (openedTabsAction == OpenedTabsAction.SendToLocalhost)
                {
                    b.Add(await MainPage.Instance.SendToWS(true, item));
                }
                else if (openedTabsAction == OpenedTabsAction.SendToSunamoCz)
                {
                    b.Add(await MainPage.Instance.SendToWS(false, item));
                }
#endif
            }
            if (CA.IsAllTheSame<bool>(true, b))
            {
                await WpfApp.SetStatus(TypeOfMessage.Success, loader.GetString("AllCachesListingsInTabsProcessed"));
            }

            checkBoxListCacheListingsExtended = null;
            popup = null;
            checkBoxesCachesListingsExtended = null;
        }

        /// <summary>
        /// Hide popup 
        /// </summary>
        /// <param name="o"></param>
        private void CheckBoxListCacheListingsExtended_ClickCancel(object o)
        {
            popup.IsOpen = false;
            checkBoxListCacheListingsParse = null;
            popup = null;
            checkBoxesCachesListingsParse = null;
        }

        /// <summary>
        /// Ask for list name in popup
        /// </summary>
        /// <param name="sender"></param>
        private void CheckBoxList_ClickOK(object sender)
        {
            popup = null;
            enterNameOfList = new EnterOneValue(loader.GetString("LocationOfCaches"), Langs.cs);
            enterNameOfList.ClickOK += EnterNameOfList_ClickOK;
            enterNameOfList.ClickCancel += EnterNameOfList_ClickCancel;
            popup = PopupHelper.GetPopupResponsive(enterNameOfList, true, borderBrush);
        }

        #region EnterNameOfList handlers
        /// <summary>
        /// Hide popup
        /// </summary>
        /// <param name="e"></param>
        private void EnterNameOfList_ClickCancel(EnterOneValueEventArgs e)
        {
            popup = null;

            checkBoxesCachesListingsExtended = null;
        }

        /// <summary>
        /// Create file with caches
        /// </summary>
        /// <param name="ea"></param>
        private async void EnterNameOfList_ClickOK(EnterOneValueEventArgs ea)
        {
            popup = null;

            // Save ticked caches, show in new tab, which for now as the only one dont render AweBrowser
            var s = checkBoxesCachesListingsExtended.GetValuesByValuesOfKeysProperty<bool?>(CheckBox.IsCheckedProperty, true);
            checkBoxesCachesListingsExtended = null;

            StringBuilder sb = new StringBuilder();
            foreach (CacheListingExtended item in s)
            {
                sb.AppendLine(item.MostImportantList());
            }

            string name = FS.DeleteWrongCharsInFileName(ea.EnteredText.ToString().Trim(), false);

            FileNameWithDateTime fnwdt = await dtfi.SaveFileWithDate(name, sb.ToString());
            savedCachesItems.Insert(0, GetSelectorListViewItem(fnwdt));
            UpdateSavedCachesListView();
            await WpfApp.SetStatus(TypeOfMessage.Information, loader.GetString("WasCreatedListOfCachesWithNameColon") + name);
        }
        #endregion

        /// <summary>
        /// Set IsEnabled of AppBarButton by conditions
        /// </summary>
        /// <param name="o"></param>
        private void LbSavedCachesListHelper_SelectionChangedObject(object o)
        {
            cmdOpenListingsInGcCom.UpdateAssociatedControls();
            cmdLogVisitsInGcCom.UpdateAssociatedControls();
            cmdCreateHtml.UpdateAssociatedControls();
        }

        #region CheckBoxList handlers
        /// <summary>
        /// Hide popup
        /// </summary>
        /// <param name="ea"></param>
        private void CheckBoxList_ClickCancel(object ea)
        {
            popup.IsOpen = false;
            checkBoxListCacheListingsExtended = null;
            popup = null;
            checkBoxesCachesListingsExtended = null;
        }
        #endregion

        #region CheckBoxListCacheListingsParse handlers
        /// <summary>
        /// Hide popup
        /// </summary>
        /// <param name="o"></param>
        private void CheckBoxListCacheListingsParse_ClickCancel(object o)
        {
            popup.IsOpen = false;
            checkBoxListCacheListingsParse = null;
            popup = null;
            checkBoxesCachesListingsParse = null;
        }
        #endregion

        /// <summary>
        /// After close tab through button
        /// Select tab before closed. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void tcTabs_Selected(object sender, RoutedEventArgs e)
        {
            TabItem ti = SelectedTabItem();
            if (ti == null)
            {
                ti = CA.GetElementActualOrBefore<TabItem>(tabItems, indexClosedTabItem);
                if (ti == null)
                {
                    lastOpenedWebView.NavigateToString("");
                    lastOpenedISunamoAppsBrowser = null;
                    sbc.SetUri("");
                }
                else
                {
                    //
                    cbOpenedTabs.SelectedItem = ti;
                    await CbOpenedTabs_SelectionChanged();
                }
            }

            if (ti != null)
            {
                foreach (var item in otherTabItemsContent)
                {
                    if (ti.Content == item)
                    {
                        return;
                    }
                }

                webControl = (SunamoBrowser)ti.Content;
            }
            else
            {
                hd = null;
                webControl = CreateInstanceWebViewHost();
            }
        }


        #endregion Control handlers

        #region Working with HTML
        /// <summary>
        /// Load file from disc and parse
        /// </summary>
        /// <returns></returns>
        async Task<List<CacheListingParse>> ParseActualSelectedCacheList()
        {
            List<CacheListingParse> vr = new List<CacheListingParse>();
            if (savedCaches.lbSavedCachesListHelper.IsSelected)
            {
                CacheListingParse actual = null;
                var fnwdt = savedCaches.lbSavedCachesListHelper.SelectedU;
                string[] lines = await apps.TF.GetLines(await dtfi.GetStorageFile(fnwdt));
                Guid g = Guid.Empty;
                foreach (var item in lines)
                {
                    if (CacheListingExtended.HasRightFormat(item))
                    {
                        if (actual != null)
                        {
                            vr.Add(actual);
                            actual.hint = actual.hint.Trim();
                        }

                        actual = CacheListingExtended.Parse(item);
                        actual.guid = g.ToString();
                    }
                    else
                    {
                        Guid.TryParse(item, out g);
                        if (g == Guid.Empty)
                        {
                            actual.hint += item + Environment.NewLine;
                        }
                    }
                }
                vr.Add(actual);
            }

            return vr;
        }

        /// <summary>
        /// Get all opened listings in typed class
        /// </summary>
        /// <returns></returns>
        private async Task<List<CacheListingExtended>> ParseActualOpenedListings()
        {
            List<CacheListingExtended> vr = new List<CacheListingExtended>();
            List<SunamoBrowser> sunamoBrowsers = AllCacheListingsAsSunamoBrowserList();
            foreach (var item in tabItems)
            {
                SunamoBrowser sunamoBrowser = SunamoBrowserFromTabItem(item);
                if (!sunamoBrowser.IsNavigating)
                {
                    bool containsAuri = false;
                    string auri = SourceOfWebView(sunamoBrowser.Source);

                    foreach (var item2 in uriPatternsListing)
                    {
                        if (auri.Contains(item2))
                        {
                            containsAuri = true;
                            break;
                        }
                    }

                    if (containsAuri)
                    {
                        var l = parsedListingOfPages[item];
                        vr.Add(l);
                    }
                }
            }
            return vr;
        }
        #endregion

        #region Log founded caches
        /// <summary>
        /// Show popup with checkboxes with caches which will be open in browser
        /// </summary>
        /// <returns></returns>
        public async Task AskForOpenedTabs()
        {
            List<CacheListingExtended> actualSelectedCacheList = await ParseActualOpenedListings();
            if (actualSelectedCacheList.Count != 0)
            {
                checkBoxesCachesListingsExtended = new SunamoDictionaryWithKeysDependencyObject<CheckBox, CacheListingExtended>();
                checkBoxListCacheListingsExtended = new CheckBoxList();
                checkBoxListCacheListingsExtended.ClickOK += CheckBoxListCacheListingsExtended_ClickOK;
                checkBoxListCacheListingsExtended.ClickCancel += CheckBoxListCacheListingsExtended_ClickCancel;

                List<string> addedGcCodes = new List<string>();

                foreach (var item in actualSelectedCacheList)
                {
                    string gcCode = item.gcCode;
                    if (!addedGcCodes.Contains(gcCode))
                    {
                        addedGcCodes.Add(gcCode);

                        CheckBox chb;
                        chb = CheckBoxHelper.Get(TextWrapping.NoWrap, item.MostImportant());

                        checkBoxesCachesListingsExtended.Add(chb, item);
                        checkBoxListCacheListingsExtended.AddCheckBox(chb);
                    }
                }
                popup = PopupHelper.GetPopupResponsive(checkBoxListCacheListingsExtended, true, borderBrush);
            }
            else
            {
                await WpfApp.SetStatus(TypeOfMessage.Information, loader.GetString("NoCacheListingInTabs"));
            }
        }

        /// <summary>
        /// Open popup with checkboxes with caches
        /// </summary>
        private async void StartOpenSavedList()
        {
            List<CacheListingParse> actualSelectedCacheList = await ParseActualSelectedCacheList();
            if (actualSelectedCacheList.Count != 0)
            {
                checkBoxesCachesListingsParse = new SunamoDictionaryWithKeysDependencyObject<CheckBox, CacheListingParse>();
                CheckBox chbCreateHtmlListWithLinks = null;

                if (savedCachesAction == SavedCachesAction.CreateHtmlList)
                {
                    chbCreateHtmlListWithLinks = new CheckBox();
                    chbCreateHtmlListWithLinks.Content = loader.GetString("MakeWithLinks");
                }

                checkBoxListCacheListingsParse = new CheckBoxList(chbCreateHtmlListWithLinks);
                checkBoxListCacheListingsParse.ClickOK += CheckBoxListWithCheckBoxCacheListingsParse_ClickOK;
                checkBoxListCacheListingsParse.ClickCancel += CheckBoxListCacheListingsParse_ClickCancel;

                List<string> addedGcCodes = new List<string>();

                foreach (var item in actualSelectedCacheList)
                {
                    string gcCode = item.gcCode;
                    if (!addedGcCodes.Contains(gcCode))
                    {
                        addedGcCodes.Add(gcCode);

                        CheckBox chb;
                        chb = CheckBoxHelper.Get(TextWrapping.NoWrap, item.MostImportant());

                        checkBoxesCachesListingsParse.Add(chb, item);
                        checkBoxListCacheListingsParse.AddCheckBox(chb);
                    }
                }
                popup = PopupHelper.GetPopupResponsive(checkBoxListCacheListingsParse, true, borderBrush);
            }
            else
            {
                await WpfApp.SetStatus(TypeOfMessage.Information, loader.GetString("NoCacheListingsInSavedList"));
            }
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="lmn"></param>
        /// <param name="alsoLb"></param>
        /// <returns></returns>
        public async Task SetStatus(LogMessageAbstract<Color, StorageFile> lmn, bool alsoLb)
        {
            if (alsoLb)
            {
                await RefreshLogs(lmn);
            }
            var st = lmn.st;
            var status = lmn.Message;
            await WpfApp.SetStatusToTextBlock(st, status);
        }

        /// <summary>
        /// Add log 
        /// </summary>
        /// <param name="lm"></param>
        /// <returns></returns>
        private async Task RefreshLogs(LogMessageAbstract<Color, StorageFile> lm)
        {
            await cd.RunAsync(cdp, () =>
            {
                lbLogs.Children.Insert(0, WpfControlGenerator.LogMessage(lm));
            });
        }
        #endregion
    }
}
