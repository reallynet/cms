﻿using System.Collections.Generic;
using System.Linq;
using SiteServer.CMS.Core;
using SiteServer.CMS.DataCache.Core;
using SiteServer.CMS.Model;
using SiteServer.CMS.Model.Db;

namespace SiteServer.CMS.DataCache
{
	public static class ContentGroupManager
	{
	    private static class ContentGroupManagerCache
	    {
	        private static readonly object LockObject = new object();

	        private static readonly string CacheKey = DataCacheManager.GetCacheKey(nameof(ContentGroupManager));

	        public static void Clear()
	        {
	            DataCacheManager.Remove(CacheKey);
	        }

	        public static Dictionary<int, List<ContentGroupInfo>> GetAllContentGroups()
	        {
	            var retVal = DataCacheManager.Get<Dictionary<int, List<ContentGroupInfo>>>(CacheKey);
	            if (retVal != null) return retVal;

	            lock (LockObject)
	            {
	                retVal = DataCacheManager.Get<Dictionary<int, List<ContentGroupInfo>>>(CacheKey);
	                if (retVal == null)
	                {
	                    retVal = DataProvider.ContentGroupDao.GetAllContentGroups();

	                    DataCacheManager.Insert(CacheKey, retVal);
	                }
	            }

	            return retVal;
	        }
	    }

	    public static void ClearCache()
	    {
	        ContentGroupManagerCache.Clear();
	    }

	    public static bool IsExists(int siteId, string groupName)
	    {
	        var list = GetContentGroupInfoList(siteId);
	        return list.Any(group => group.GroupName == groupName);
	    }

	    public static ContentGroupInfo GetContentGroupInfo(int siteId, string groupName)
	    {
	        var list = GetContentGroupInfoList(siteId);
	        return list.FirstOrDefault(group => group.GroupName == groupName);
	    }

	    public static List<string> GetGroupNameList(int siteId)
	    {
	        var list = GetContentGroupInfoList(siteId);
	        return list.Select(group => group.GroupName).ToList();
	    }

        public static List<ContentGroupInfo> GetContentGroupInfoList(int siteId)
        {
            List<ContentGroupInfo> list = null;
            var dict = ContentGroupManagerCache.GetAllContentGroups();

            if (dict != null && dict.ContainsKey(siteId))
            {
                list = dict[siteId];
            }
            return list ?? new List<ContentGroupInfo>();
        }
    }
}
