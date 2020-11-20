﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;

/*TODO: Make user actions (not system actions trying to refresh itself) override last fatal error and try again.
 * and if worked, reset fatalerror.  Another option would be to add a status panel like outlook and
 * give the user the option to try to reestablish network connectivity that way 
 * */
namespace Rawr
{
    public class WebRequestWrapper
    {
        //5 seems to be the magic number when the armory is acting up.
        private const int RETRY_MAX = 5;

        public const string CONTENT_XML = "application/xml";
        public const string CONTENT_JPG = "image/jpeg";

        private class DownloadRequest
        {
            public string serverPath;
            public string localPath;
            public string error;
            public string contentType = CONTENT_XML;
        }
        private Queue<DownloadRequest> _downloadRequests;
        private List<DownloadRequest> _failedRequests;
        private Thread[] _webRequestThreads;
        private string _proxyServer;
        private bool _useDefaultProxy;
        private string _proxyUserName;
        private string _proxyPassword;
        private string _proxyDomain;
        private int _proxyPort;


        private static Exception _fatalError = null;
        private Dictionary<Character.CharacterRegion, string> _domains = new Dictionary<Character.CharacterRegion, string>();

        public interface INetworkSettingsProvider
        {
            int MaxHttpRequests { get; }
            bool UseDefaultProxySettings { get; }
            string ProxyServer { get; }
            int ProxyPort { get; }
            string ProxyUserName { get; }
            string ProxyPassword { get; }
            string ProxyDomain { get; }
            string ProxyType { get; }
            string UserAgent { get; }
            bool DownloadItemInfo { get; }
            bool ProxyRequiresAuthentication { get; }
            bool UseDefaultAuthenticationForProxy { get; }
            string WoWItemIconURI { get; }
            string UserAgent_IE7 { get; }
            string UserAgent_IE6 { get; }
            string UserAgent_FireFox2 { get; }
            string ClassTalentURI { get; }
            string CharacterTalentURI { get; }
            string CharacterSheetURI { get; }
            string ItemToolTipSheetURI { get; }
            string ItemUpgradeURI { get; }
            string WoWTalentIconURI { get; }
            string ItemInfoURI { get; }
        }

        public interface ICacheSettingsProvider
        {
            string RelativeItemImageCache { get; }
            string RelativeTalentImageCache { get; }
        }

        private class DefaultNetworkSettingsProvider : INetworkSettingsProvider
        {
            #region INetworkSettingsProvider Members

            public int MaxHttpRequests
            {
                get { return Rawr.Properties.NetworkSettings.Default.MaxHttpRequests; }
            }

            public bool UseDefaultProxySettings
            {
                get { return Rawr.Properties.NetworkSettings.Default.UseDefaultProxySettings; }
            }

            public string ProxyServer
            {
                get { return Rawr.Properties.NetworkSettings.Default.ProxyServer; }
            }

            public int ProxyPort
            {
                get { return Rawr.Properties.NetworkSettings.Default.ProxyPort; }
            }

            public string ProxyUserName
            {
                get { return Rawr.Properties.NetworkSettings.Default.ProxyUserName; }
            }

            public string ProxyPassword
            {
                get { return Rawr.Properties.NetworkSettings.Default.ProxyPassword; }
            }

            public string ProxyDomain
            {
                get { return Rawr.Properties.NetworkSettings.Default.ProxyDomain; }
            }

            public string ProxyType
            {
                get { return Rawr.Properties.NetworkSettings.Default.ProxyType; }
            }

            public string UserAgent
            {
                get { return Rawr.Properties.NetworkSettings.Default.UserAgent; }
            }

            public bool DownloadItemInfo
            {
                get { return Rawr.Properties.NetworkSettings.Default.DownloadItemInfo; }
            }

            public bool ProxyRequiresAuthentication
            {
                get { return Rawr.Properties.NetworkSettings.Default.ProxyRequiresAuthentication; }
            }

            public bool UseDefaultAuthenticationForProxy
            {
                get { return Rawr.Properties.NetworkSettings.Default.UseDefaultAuthenticationForProxy; }
            }

            public string WoWItemIconURI
            {
                get { return Rawr.Properties.NetworkSettings.Default.WoWItemIconURI; }
            }

            public string UserAgent_IE7
            {
                get { return Rawr.Properties.NetworkSettings.Default.UserAgent_IE7; }
            }

            public string UserAgent_IE6
            {
                get { return Rawr.Properties.NetworkSettings.Default.UserAgent_IE6; }
            }

            public string UserAgent_FireFox2
            {
                get { return Rawr.Properties.NetworkSettings.Default.UserAgent_FireFox2; }
            }

            public string ClassTalentURI
            {
                get { return Rawr.Properties.NetworkSettings.Default.ClassTalentURI; }
            }

            public string CharacterTalentURI
            {
                get { return Rawr.Properties.NetworkSettings.Default.CharacterTalentURI; }
            }

            public string CharacterSheetURI
            {
                get { return Rawr.Properties.NetworkSettings.Default.CharacterSheetURI; }
            }

            public string ItemToolTipSheetURI
            {
                get { return Rawr.Properties.NetworkSettings.Default.ItemToolTipSheetURI; }
            }

            public string ItemUpgradeURI
            {
                get { return Rawr.Properties.NetworkSettings.Default.ItemUpgradeURI; }
            }

            public string WoWTalentIconURI
            {
                get { return Rawr.Properties.NetworkSettings.Default.WoWTalentIconURI; }
            }

            public string ItemInfoURI
            {
                get { return Rawr.Properties.NetworkSettings.Default.ItemInfoURI; }
            }

            #endregion
        }

        private class DefaultCacheSettingsProvider : ICacheSettingsProvider
        {
            #region ICacheSettingsProvider Members

            public string RelativeItemImageCache
            {
                get { return Rawr.Properties.CacheSettings.Default.RelativeItemImageCache; }
            }

            public string RelativeTalentImageCache
            {
                get { return Rawr.Properties.CacheSettings.Default.RelativeTalentImageCache; }
            }

            #endregion
        }

        public static INetworkSettingsProvider NetworkSettingsProvider = new DefaultNetworkSettingsProvider();
        public static ICacheSettingsProvider CacheSettingsProvider = new DefaultCacheSettingsProvider();

        public WebRequestWrapper()
        {
            int maxConnections = NetworkSettingsProvider.MaxHttpRequests;
            _failedRequests = new List<DownloadRequest>();
            _webRequestThreads = new Thread[maxConnections];
            _downloadRequests = new Queue<DownloadRequest>();
            _useDefaultProxy = NetworkSettingsProvider.UseDefaultProxySettings;

            _proxyServer = NetworkSettingsProvider.ProxyServer;
            _proxyPort = NetworkSettingsProvider.ProxyPort;
            _proxyUserName = NetworkSettingsProvider.ProxyUserName;
            _proxyPassword = NetworkSettingsProvider.ProxyPassword;
            _proxyDomain = NetworkSettingsProvider.ProxyDomain;
            _domains.Add(Character.CharacterRegion.US, "www");
            _domains.Add(Character.CharacterRegion.EU, "eu");
            _domains.Add(Character.CharacterRegion.KR, "kr");
            _domains.Add(Character.CharacterRegion.TW, "tw");
            _domains.Add(Character.CharacterRegion.CN, "cn");
        }


        public string DownloadClassTalentTree(Character.CharacterClass characterClass)
        {
            //http://www.worldofwarcraft.com/shared/global/talents/{0}/data.js
            return DownloadText(string.Format(NetworkSettingsProvider.ClassTalentURI, characterClass.ToString().ToLower()));
        }

        public XmlDocument DownloadCharacterTalentTree(string characterName, Character.CharacterRegion region, string realm)
        {
            //http://{0}.wowarmory.com/character-talents.xml?r={1}&n={2}
            string domain = _domains[region];
            XmlDocument doc = null;
            if (!String.IsNullOrEmpty(characterName))
            {
                doc = DownloadXml(string.Format(NetworkSettingsProvider.CharacterTalentURI,
                                                    domain, realm, characterName));
            }
            return doc;
        }

        public XmlDocument DownloadCharacterSheet(string characterName, Character.CharacterRegion region, string realm)
        {
            //http://{0}.wowarmory.com/character-sheet.xml?r={1}&n={2}
            string domain = _domains[region];
            XmlDocument doc = null;
            if (!String.IsNullOrEmpty(characterName))
            {
                doc = DownloadXml(string.Format(NetworkSettingsProvider.CharacterSheetURI,
                                                    domain, realm, characterName));
            }
            return doc;
        }

        public XmlDocument DownloadUpgrades(string characterName, Character.CharacterRegion region, string realm, int itemId)
        {
            //http://{0}.wowarmory.com/search.xml?searchType=items&pr={1}&pn={2}&pi={3}
            string domain = _domains[region];
            XmlDocument doc = null;
            if (!String.IsNullOrEmpty(characterName))
            {
                doc = DownloadXml(string.Format(NetworkSettingsProvider.ItemUpgradeURI,
                                                    domain, realm, characterName, itemId.ToString()));
            }
            return doc;
        }

        public XmlDocument DownloadItemInformation(int id)
        {
            return DownloadXml(string.Format(NetworkSettingsProvider.ItemInfoURI, id.ToString()));
        }

        public XmlDocument DownloadItemToolTipSheet(string id)
        {
            XmlDocument doc = null;
            if (!string.IsNullOrEmpty(id))
            {
                doc = DownloadXml(string.Format(NetworkSettingsProvider.ItemToolTipSheetURI, id));
            }
            return doc;
        }

        /// <summary>
        /// Downloads the Item icon
        /// </summary>
        /// <param name="iconName">the name of the item icon to download, no extension, no path</param>
        /// <returns>The full path to the downloaded icon.  Null is returned if no icon  could be downloaded</returns>
        public string DownloadItemIcon(string iconName)
        {
            string filePath = Path.Combine(ItemImageCachePath, iconName + ".jpg");
            DownloadFile(NetworkSettingsProvider.WoWItemIconURI + iconName + ".jpg",
                            filePath, CONTENT_JPG);
            if (!File.Exists(filePath))
            {
                filePath = null;
            }
            return filePath;
        }

        /// <summary>
        /// Downloads the icon associated with the talent passed in
        /// </summary>
        /// <param name="charClass">CharacterClass of the given icon</param>
        /// <param name="talentTree">name of the talent tree</param>
        /// <param name="talentName">name of the talent</param>
        /// <returns>The full path to the downloaded icon.  Null is returned if no icon could be downloaded</returns>
        public string DownloadTalentIcon(Character.CharacterClass charClass, string talentTree, string talentName)
        {
            string imageName = talentTree + "_" + talentName + ".jpg";
            string fullPathToSave = Path.Combine(TalentImageCachePath, charClass.ToString().ToLower() + "\\" + imageName);

            if (!String.IsNullOrEmpty(talentTree) && !String.IsNullOrEmpty(talentName))
            {
                //0 = class, 1=tree, 2=talentname - all lowercase
                //@"http://www.worldofwarcraft.com/shared/global/talents/{0}/images/{1}/{2}.jpg";
                string uri = string.Format(NetworkSettingsProvider.WoWTalentIconURI, charClass.ToString().ToLower(),
                                                talentTree.ToLower(), talentName.ToLower());
                DownloadFile(uri, fullPathToSave, CONTENT_JPG);
            }
            if (!File.Exists(fullPathToSave))
            {
                fullPathToSave = null;
            }
            return fullPathToSave;
        }

        /// <summary>
        /// Downloads the temp image for use as an icon.
        /// </summary>
        /// <returns>Full path to the temp image.</returns>
        public string DownloadTempImage()
        {
            return DownloadItemIcon("temp");
        }

        /// <summary>
        /// Gets the number of request failures since the last time the failure list was cleared.
        /// </summary>
        public int QueueFailureCount
        {
            get { return _failedRequests.Count; }
        }


        /// <summary>
        /// Count of the currently queued download requests.
        /// </summary>
        public int RequestQueueCount
        {
            get { return _downloadRequests.Count; }
        }

        /// <summary>
        /// If the last request received a 407 or no response. Used to prevent a lot of bad calls.
        /// It also has the good side effect of not locking someone's account out if they enter the proxy info incorrectly
        /// by sending lots of bad authorization attempts.
        /// </summary>
        public static bool LastWasFatalError
        {
            get { return _fatalError != null; }
        }

        public static Exception FatalError
        {
            get { return _fatalError; }
        }

        public static void ResetFatalErrorIndicator()
        {
            _fatalError = null;
        }
        /// <summary>
        /// Downloads an Icon Asyncronously
        /// </summary>
        /// <param name="iconPath">The name of the icon to download.  No extension, No Path.</param>
        public void DownloadItemIconAsync(string iconName)
        {
            string localPath = Path.Combine(ItemImageCachePath, iconName + ".jpg");
            if (!File.Exists(localPath))
            {
                DownloadRequest dl = new DownloadRequest();
                dl.serverPath = NetworkSettingsProvider.WoWItemIconURI + iconName + ".jpg";
                dl.localPath = localPath;
                dl.contentType = CONTENT_JPG;
                InitiateRequest(dl);
            }
        }


        private string ItemImageCachePath
        {
            get
            {
                return (Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                    CacheSettingsProvider.RelativeItemImageCache));
            }
        }

        private string TalentImageCachePath
        {
            get
            {
                return (Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                CacheSettingsProvider.RelativeTalentImageCache));
            }
        }

        /// <summary>
        /// Used to create a web client with all of the appropriote proxy/useragent/etc settings
        /// </summary>
        private WebClient CreateWebClient()
        {
            WebClient client = new WebClient() { Encoding = Encoding.UTF8 };
            client.Headers.Add("user-agent", NetworkSettingsProvider.UserAgent);
            if (NetworkSettingsProvider.ProxyType == "Http")
            {
                if (_useDefaultProxy)
                {
                    client.Proxy = HttpWebRequest.DefaultWebProxy;
                }
                else if (!String.IsNullOrEmpty(_proxyServer))
                {
                    client.Proxy = new WebProxy(_proxyServer, _proxyPort);
                }
                if (client.Proxy != null && NetworkSettingsProvider.ProxyRequiresAuthentication)
                {
                    if (NetworkSettingsProvider.UseDefaultAuthenticationForProxy)
                    {
                        client.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
                    }
                    else
                    {
                        client.Proxy.Credentials = new NetworkCredential(_proxyUserName, _proxyPassword, _proxyDomain);
                    }
                }
            }
            return client;
        }

        private void DownloadFile(string URI, string localPath)
        {
            DownloadFile(URI, localPath, CONTENT_XML);
        }

        /// <summary>
        /// Download a given file with the appropriote configuration information
        /// </summary>
        /// <param name="serverPath">URI to download</param>
        /// <param name="localPath">local path, including file name,  where the downloaded file will be saved</param>
        private void DownloadFile(string URI, string localPath, string contentType)
        {
            int retry = 0;
            bool success = false;
            //occasionally a zero byte file slips through without throwing an exception
            if (!File.Exists(localPath) || new FileInfo(localPath).Length <= 0)
            {
                do
                {
                    if (!LastWasFatalError)
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(localPath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(localPath));
                        }
                        using (WebClient client = CreateWebClient())
                        {
                            try
                            {
                                client.DownloadFile(URI, localPath);
                                if (!client.ResponseHeaders[HttpResponseHeader.ContentType].StartsWith(contentType))
                                {
                                    throw new Exception("invalid content type");
                                }
                                success = true;
                            }
                            catch (Exception ex)
                            {
                                CheckExecptionForFatalError(ex);
                                //if on a client file download, there is an exception, 
                                //it will create a 0 byte file. We don't want that empty file.
                                if (File.Exists(localPath))
                                {
                                    File.Delete(localPath);
                                }
                                retry++;
                                if (retry == RETRY_MAX || LastWasFatalError)
                                {
                                    throw;
                                }
                            }
                        }
                    }
                } while (retry <= RETRY_MAX && !success && !LastWasFatalError);
            }
        }

        /// <summary>
        /// This is used to prevent multiple attempts at network traffic when its not working and 
        /// continuing to issue requests could cause serious problems for the user.
        /// </summary>
        /// <param name="ex"></param>
        private void CheckExecptionForFatalError(Exception ex)
        {
            //Log.Write("Exception trying to download: "+ ex);
            //Log.Write(ex.StackTrace);
            if (ex.Message.Contains("407") /*proxy auth required */
                || ex.Message.Contains("403") /*proxy info probably wrong, if we keep issuing requests, they will probably get locked out*/
                || ex.Message.Contains("timed out") /*either proxy required and firewall dropped the request, or armory is down*/
                || ex.Message.Contains("invalid content type") /*unexpected content type returned*/
                || ex.Message.Contains("The remote name could not be resolved") /* DNS problems*/
                )
            {
                _fatalError = ex;
            }
        }

        private string DownloadText(string URI)
        {
            WebClient webClient = CreateWebClient();
            string value = null;
            int retry = 0;
            bool success = false;
            do
            {
                if (!LastWasFatalError)
                {
                    try
                    {
                        value = webClient.DownloadString(URI);
                        if (!String.IsNullOrEmpty(value))
                        {
                            success = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        CheckExecptionForFatalError(ex);
                    }
                }
                retry++;
            } while (retry <= RETRY_MAX && !success && !LastWasFatalError);
            return value;
        }

        private XmlDocument DownloadXml(string URI)
        {
            XmlDocument returnDocument = null;
            int retry = 0;
            //Download Text has retry logic in it as well, but that just makes sure it gets a response, this
            //makes sure we get a valid XML response.
            do
            {
                string xml = DownloadText(URI);
                //If it contains "<table", then the armory accidentally returned it as html instead of xml.
                if (!string.IsNullOrEmpty(xml) && !xml.Contains("<table"))
                {
                    try
                    {
                        returnDocument = new XmlDocument();
                        returnDocument.LoadXml(xml);
                        if (returnDocument == null || returnDocument.DocumentElement == null
                                    || !returnDocument.DocumentElement.HasChildNodes
                                    || !returnDocument.DocumentElement.ChildNodes[0].HasChildNodes)
                        {
                            //document returned no data we care about.
                            returnDocument = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                retry++;
            } while (returnDocument == null && !LastWasFatalError && retry < RETRY_MAX);

            return returnDocument;
        }

        /// <summary>
        /// Queues up download requests and then starts a new thread if the thread pool is not full.
        /// </summary>
        /// <param name="dl">Download Request to Service</param>
        private void InitiateRequest(DownloadRequest dl)
        {
            lock (_downloadRequests)
            {
                _downloadRequests.Enqueue(dl);
            }

            for (int i = 0; i < _webRequestThreads.Length; i++)
            {
                if (_webRequestThreads[i] == null || _webRequestThreads[i].ThreadState == ThreadState.Stopped || _webRequestThreads[i].ThreadState == ThreadState.Aborted)
                {
                    //Thread is either null or terminated, start a new one
                    _webRequestThreads[i] = new Thread(new ThreadStart(ThreadDoWork));
                    _webRequestThreads[i].Start();
                    break;
                }
            }
        }

        /// <summary>
        /// Loop over the queued up download requests and service them.  Terminate thread when queue is empty.
        /// </summary>
        private void ThreadDoWork()
        {
            try
            {
                DownloadRequest dl = null;
                while (_downloadRequests.Count > 0 && !LastWasFatalError)
                {
                    lock (_downloadRequests)
                    {
                        if (_downloadRequests.Count > 0)
                        {
                            dl = _downloadRequests.Dequeue();
                        }
                    }
                    if (dl != null)
                    {
                        try
                        {
                            DownloadFile(dl.serverPath, dl.localPath, dl.contentType);
                        }
                        catch (Exception ex)
                        {
                            CheckExecptionForFatalError(ex);
                            dl.error = ex.Message;
                            _failedRequests.Add(dl);
                        }
                    }
                }
            }
            catch (Exception)
            {
                //Log.Write(ex.Message);
                // Log.Write(ex.StackTrace);
            }
        }
    }
}
