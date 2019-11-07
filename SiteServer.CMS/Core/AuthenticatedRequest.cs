﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using SiteServer.CMS.DataCache;
using SiteServer.CMS.Model;
using SiteServer.CMS.Plugin.Apis;
using SiteServer.CMS.Plugin.Impl;
using SiteServer.Plugin;
using SiteServer.Utils;
using SiteServer.Utils.Auth;

namespace SiteServer.CMS.Core
{
    public class AuthenticatedRequest : IAuthenticatedRequest
    {
        public AuthenticatedRequest(): this(HttpContext.Current.Request)
        {
            
        }

        public AuthenticatedRequest(HttpRequest request)
        {
            try
            {
                HttpRequest = request;

                var apiToken = ApiToken;
                if (!string.IsNullOrEmpty(apiToken))
                {
                    var tokenInfo = AccessTokenManager.GetAccessTokenInfoAsync(apiToken).GetAwaiter().GetResult();
                    if (tokenInfo != null)
                    {
                        if (!string.IsNullOrEmpty(tokenInfo.AdminName))
                        {
                            var adminInfo = AdminManager.GetAdminInfoByUserNameAsync(tokenInfo.AdminName).GetAwaiter().GetResult();
                            if (adminInfo != null && !adminInfo.Locked)
                            {
                                Administrator = adminInfo;
                                IsAdminLoggin = true;
                            }
                        }

                        IsApiAuthenticated = true;
                    }
                }

                var userToken = UserToken;
                if (!string.IsNullOrEmpty(userToken))
                {
                    var tokenImpl = UserApi.Instance.ParseAccessToken(userToken);
                    if (tokenImpl.UserId > 0 && !string.IsNullOrEmpty(tokenImpl.UserName))
                    {
                        var user = UserManager.GetUserByUserIdAsync(tokenImpl.UserId).GetAwaiter().GetResult();
                        if (user != null && !user.Locked && user.Checked && user.UserName == tokenImpl.UserName)
                        {
                            User = user;
                            IsUserLoggin = true;
                        }
                    }
                }

                var adminToken = AdminToken;
                if (!string.IsNullOrEmpty(adminToken))
                {
                    var tokenImpl = AdminApi.Instance.ParseAccessToken(adminToken);
                    if (tokenImpl.UserId > 0 && !string.IsNullOrEmpty(tokenImpl.UserName))
                    {
                        var adminInfo = AdminManager.GetAdminInfoByUserIdAsync(tokenImpl.UserId).GetAwaiter().GetResult();
                        if (adminInfo != null && !adminInfo.Locked && adminInfo.UserName == tokenImpl.UserName)
                        {
                            Administrator = adminInfo;
                            IsAdminLoggin = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtils.AddErrorLog(ex);
            }
        }

        public bool IsApiAuthenticated { get; }

        public bool IsUserLoggin { get; }

        public bool IsAdminLoggin { get; private set; }

        public string ApiToken
        {
            get
            {
                var accessTokenStr = string.Empty;
                if (!string.IsNullOrEmpty(HttpRequest.Headers.Get(Constants.AuthKeyApiHeader)))
                {
                    accessTokenStr = HttpRequest.Headers.Get(Constants.AuthKeyApiHeader);
                }
                else if (!string.IsNullOrEmpty(HttpRequest.QueryString[Constants.AuthKeyApiQuery]))
                {
                    accessTokenStr = HttpRequest.QueryString[Constants.AuthKeyApiQuery];
                }
                else if (!string.IsNullOrEmpty(CookieUtils.GetCookie(Constants.AuthKeyApiCookie)))
                {
                    accessTokenStr = CookieUtils.GetCookie(Constants.AuthKeyApiCookie);
                }

                if (StringUtils.EndsWith(accessTokenStr, TranslateUtils.EncryptStingIndicator))
                {
                    accessTokenStr = TranslateUtils.DecryptStringBySecretKey(accessTokenStr);
                }

                return accessTokenStr;
            }
        }

        private string UserToken
        {
            get
            {
                var accessTokenStr = string.Empty;
                if (!string.IsNullOrEmpty(CookieUtils.GetCookie(Constants.AuthKeyUserCookie)))
                {
                    accessTokenStr = CookieUtils.GetCookie(Constants.AuthKeyUserCookie);
                }
                else if (!string.IsNullOrEmpty(HttpRequest.Headers.Get(Constants.AuthKeyUserHeader)))
                {
                    accessTokenStr = HttpRequest.Headers.Get(Constants.AuthKeyUserHeader);
                }
                else if (!string.IsNullOrEmpty(HttpRequest.QueryString[Constants.AuthKeyUserQuery]))
                {
                    accessTokenStr = HttpRequest.QueryString[Constants.AuthKeyUserQuery];
                }

                if (StringUtils.EndsWith(accessTokenStr, TranslateUtils.EncryptStingIndicator))
                {
                    accessTokenStr = TranslateUtils.DecryptStringBySecretKey(accessTokenStr);
                }

                return accessTokenStr;
            }
        }

        public string AdminToken
        {
            get
            {
                var accessTokenStr = string.Empty;
                if (!string.IsNullOrEmpty(CookieUtils.GetCookie(Constants.AuthKeyAdminCookie)))
                {
                    accessTokenStr = CookieUtils.GetCookie(Constants.AuthKeyAdminCookie);
                }
                else if (!string.IsNullOrEmpty(HttpRequest.Headers.Get(Constants.AuthKeyAdminHeader)))
                {
                    accessTokenStr = HttpRequest.Headers.Get(Constants.AuthKeyAdminHeader);
                }
                else if (!string.IsNullOrEmpty(HttpRequest.QueryString[Constants.AuthKeyAdminQuery]))
                {
                    accessTokenStr = HttpRequest.QueryString[Constants.AuthKeyAdminQuery];
                }

                if (StringUtils.EndsWith(accessTokenStr, TranslateUtils.EncryptStingIndicator))
                {
                    accessTokenStr = TranslateUtils.DecryptStringBySecretKey(accessTokenStr);
                }

                return accessTokenStr;
            }
        }

        private Dictionary<string, object> _postData;

        public Dictionary<string, object> PostData
        {
            get
            {
                if (_postData != null) return _postData;

                var bodyStream = new StreamReader(HttpRequest.InputStream);
                bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
                var json = bodyStream.ReadToEnd();

                _postData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(json)) return _postData;

                var dict = TranslateUtils.JsonDeserialize<Dictionary<string, object>>(json);
                foreach (var key in dict.Keys)
                {
                    _postData[key] = dict[key];
                }

                return _postData;
            }
        }

        public HttpRequest HttpRequest { get; }

        public NameValueCollection QueryString => HttpRequest.QueryString;

        public int SiteId => GetQueryInt("siteId");

        public int ChannelId => GetQueryInt("channelId");

        public int ContentId => GetQueryInt("contentId");

        public bool IsQueryExists(string name)
        {
            return HttpRequest.QueryString[name] != null;
        }

        public string GetQueryString(string name)
        {
            return !string.IsNullOrEmpty(HttpRequest.QueryString[name])
                ? AttackUtils.FilterSql(HttpRequest.QueryString[name])
                : null;
        }

        public int GetQueryInt(string name, int defaultValue = 0)
        {
            return !string.IsNullOrEmpty(HttpRequest.QueryString[name])
                ? TranslateUtils.ToIntWithNagetive(HttpRequest.QueryString[name])
                : defaultValue;
        }

        public decimal GetQueryDecimal(string name, decimal defaultValue = 0)
        {
            return !string.IsNullOrEmpty(HttpRequest.QueryString[name])
                ? TranslateUtils.ToDecimalWithNagetive(HttpRequest.QueryString[name])
                : defaultValue;
        }

        public bool GetQueryBool(string name, bool defaultValue = false)
        {
            var str = HttpRequest.QueryString[name];
            return !string.IsNullOrEmpty(str) ? TranslateUtils.ToBool(str) : defaultValue;
        }

        public bool IsPostExists(string name)
        {
            return PostData.ContainsKey(name);
        }

        public T GetPostObject<T>(string name = "")
        {
            string json;
            if (string.IsNullOrEmpty(name))
            {
                var bodyStream = new StreamReader(HttpRequest.InputStream);
                bodyStream.BaseStream.Seek(0, SeekOrigin.Begin);
                json = bodyStream.ReadToEnd();
            }
            else
            {
                json = GetPostString(name);
            }

            return TranslateUtils.JsonDeserialize<T>(json);
        }

        private object GetPostObject(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            return PostData.TryGetValue(name, out var value) ? value : null;
        }

        public string GetPostString(string name)
        {
            var value = GetPostObject(name);
            if (value == null) return null;
            if (value is string) return (string)value;
            return value.ToString();
        }

        public int GetPostInt(string name, int defaultValue = 0)
        {
            var value = GetPostObject(name);
            if (value == null) return defaultValue;
            if (value is int) return (int)value;
            return TranslateUtils.ToIntWithNagetive(value.ToString(), defaultValue);
        }

        public decimal GetPostDecimal(string name, decimal defaultValue = 0)
        {
            var value = GetPostObject(name);
            if (value == null) return defaultValue;
            if (value is decimal) return (decimal)value;
            return TranslateUtils.ToDecimalWithNagetive(value.ToString(), defaultValue);
        }

        public bool GetPostBool(string name, bool defaultValue = false)
        {
            var value = GetPostObject(name);
            if (value == null) return defaultValue;
            if (value is bool) return (bool)value;
            return TranslateUtils.ToBool(value.ToString(), defaultValue);
        }

        public DateTime GetPostDateTime(string name, DateTime defaultValue)
        {
            var value = GetPostObject(name);
            if (value == null) return defaultValue;
            if (value is DateTime) return (DateTime)value;
            return TranslateUtils.ToDateTime(value.ToString(), defaultValue);
        }

        #region Log

        public async Task AddSiteLogAsync(int siteId, string action)
        {
            await AddSiteLogAsync(siteId, 0, 0, action, string.Empty);
        }

        public async Task AddSiteLogAsync(int siteId, string action, string summary)
        {
            await AddSiteLogAsync(siteId, 0, 0, action, summary);
        }

        public async Task AddSiteLogAsync(int siteId, int channelId, string action, string summary)
        {
            await LogUtils.AddSiteLogAsync(siteId, channelId, 0, Administrator, action, summary);
        }

        public async Task AddSiteLogAsync(int siteId, int channelId, int contentId, string action, string summary)
        {
            await LogUtils.AddSiteLogAsync(siteId, channelId, contentId, Administrator, action, summary);
        }

        public async Task AddAdminLogAsync(string action, string summary)
        {
            await LogUtils.AddAdminLogAsync(Administrator, action, summary);
        }

        public async Task AddAdminLogAsync(string action)
        {
            await LogUtils.AddAdminLogAsync(Administrator, action);
        }

        #endregion

        #region Cookie

        public void SetCookie(string name, string value)
        {
            CookieUtils.SetCookie(name, value);
        }

        public void SetCookie(string name, string value, TimeSpan expiresAt)
        {
            CookieUtils.SetCookie(name, value, expiresAt);
        }

        public string GetCookie(string name)
        {
            return CookieUtils.GetCookie(name);
        }

        public bool IsCookieExists(string name)
        {
            return CookieUtils.IsExists(name);
        }

        #endregion

        private PermissionsImpl _userPermissionsImpl;

        public PermissionsImpl UserPermissionsImpl
        {
            get
            {
                if (_userPermissionsImpl != null) return _userPermissionsImpl;

                if (User != null)
                {
                    var groupInfo = UserGroupManager.GetUserGroupInfo(User.GroupId);
                    if (groupInfo != null)
                    {
                        Administrator = AdminManager.GetAdminInfoByUserNameAsync(groupInfo.AdminName).GetAwaiter().GetResult();
                    }
                }

                _userPermissionsImpl = new PermissionsImpl(Administrator);

                return _userPermissionsImpl;
            }
        }

        public IPermissions UserPermissions => UserPermissionsImpl;

        private PermissionsImpl _adminPermissionsImpl;

        public PermissionsImpl AdminPermissionsImpl
        {
            get
            {
                if (_adminPermissionsImpl != null) return _adminPermissionsImpl;

                _adminPermissionsImpl = new PermissionsImpl(Administrator);

                return _adminPermissionsImpl;
            }
        }

        public IPermissions AdminPermissions => AdminPermissionsImpl;

        #region Administrator

        public int AdminId => Administrator?.Id ?? 0;

        public string AdminName
        {
            get
            {
                if (Administrator != null)
                {
                    return Administrator.UserName;
                }

                if (User != null)
                {
                    var groupInfo = UserGroupManager.GetUserGroupInfo(User.GroupId);
                    if (groupInfo != null)
                    {
                        return groupInfo.AdminName;
                    }
                }

                return string.Empty;
            }
        }

        public Administrator Administrator { get; private set; }

        public string AdminLogin(string userName, bool isAutoLogin)
        {
            if (string.IsNullOrEmpty(userName)) return null;
            var adminInfo = AdminManager.GetAdminInfoByUserNameAsync(userName).GetAwaiter().GetResult();
            if (adminInfo == null || adminInfo.Locked) return null;

            Administrator = adminInfo;
            IsAdminLoggin = true;

            var expiresAt = TimeSpan.FromDays(Constants.AccessTokenExpireDays);
            var accessToken = AdminApi.Instance.GetAccessToken(adminInfo.Id, adminInfo.UserName, expiresAt);

            LogUtils.AddAdminLogAsync(adminInfo, "管理员登录").GetAwaiter().GetResult();

            if (isAutoLogin)
            {
                CookieUtils.SetCookie(Constants.AuthKeyAdminCookie, accessToken, expiresAt);
            }
            else
            {
                CookieUtils.SetCookie(Constants.AuthKeyAdminCookie, accessToken);
            }

            return accessToken;
        }

        public void AdminLogout()
        {
            CookieUtils.Erase(Constants.AuthKeyAdminCookie);
        }

        #endregion

        #region User

        public int UserId => User?.Id ?? 0;

        public string UserName => User?.UserName ?? string.Empty;

        public User User { get; private set; }

        public string UserLogin(string userName, bool isAutoLogin)
        {
            if (string.IsNullOrEmpty(userName)) return null;

            var user = UserManager.GetUserByUserNameAsync(userName).GetAwaiter().GetResult();
            if (user == null || user.Locked || !user.Checked) return null;

            User = user;

            var expiresAt = TimeSpan.FromDays(Constants.AccessTokenExpireDays);
            var accessToken = UserApi.Instance.GetAccessToken(UserId, UserName, expiresAt);

            DataProvider.UserDao.UpdateLastActivityDateAndCountOfLoginAsync(User).GetAwaiter().GetResult();
            LogUtils.AddUserLoginLog(userName);

            if (isAutoLogin)
            {
                CookieUtils.SetCookie(Constants.AuthKeyUserCookie, accessToken, expiresAt);
            }
            else
            {
                CookieUtils.SetCookie(Constants.AuthKeyUserCookie, accessToken);
            }

            return accessToken;
        }

        public void UserLogout()
        {
            User = null;
            CookieUtils.Erase(Constants.AuthKeyUserCookie);
        }

        #endregion

        public object AdminRedirectCheck(bool checkInstall = false, bool checkDatabaseVersion = false,
            bool checkLogin = false)
        {
            var redirect = false;
            var redirectUrl = string.Empty;

            if (checkInstall && string.IsNullOrEmpty(WebConfigUtils.ConnectionString))
            {
                redirect = true;
                redirectUrl = PageUtils.GetAdminUrl("Installer/");
            }
            else if (checkDatabaseVersion && ConfigManager.Instance.IsInitialized &&
                     ConfigManager.Instance.DatabaseVersion != SystemManager.ProductVersion)
            {
                redirect = true;
                redirectUrl = PageUtils.GetAdminUrl("syncDatabase.cshtml");
            }
            else if (checkLogin && (!IsAdminLoggin || Administrator == null || Administrator.Locked))
            {
                redirect = true;
                redirectUrl = PageUtils.GetAdminUrl("login.cshtml");
            }

            if (redirect)
            {
                return new
                {
                    Value = false,
                    RedirectUrl = redirectUrl
                };
            }

            return null;
        }

        public static string GetAccessToken(int userId, string userName, TimeSpan expiresAt)
        {
            if (userId <= 0 || string.IsNullOrEmpty(userName)) return null;

            var userToken = new AccessTokenImpl
            {
                UserId = userId,
                UserName = userName,
                ExpiresAt = DateUtils.GetExpiresAt(expiresAt)
            };

            return JsonWebToken.Encode(userToken, WebConfigUtils.SecretKey, JwtHashAlgorithm.HS256);
        }

        public static string GetAccessToken(int userId, string userName, DateTime expiresAt)
        {
            if (userId <= 0 || string.IsNullOrEmpty(userName)) return null;

            var userToken = new AccessTokenImpl
            {
                UserId = userId,
                UserName = userName,
                ExpiresAt = expiresAt
            };

            return JsonWebToken.Encode(userToken, WebConfigUtils.SecretKey, JwtHashAlgorithm.HS256);
        }

        public static AccessTokenImpl ParseAccessToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken)) return new AccessTokenImpl();

            try
            {
                var tokenObj = JsonWebToken.DecodeToObject<AccessTokenImpl>(accessToken, WebConfigUtils.SecretKey);

                if (tokenObj?.ExpiresAt.AddDays(Constants.AccessTokenExpireDays) > DateTime.Now)
                {
                    return tokenObj;
                }
            }
            catch
            {
                // ignored
            }

            return new AccessTokenImpl();
        }
    }
}