using KidesServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace KidesServer.Helpers
{
	public static class DiscordCache
	{
		private static Dictionary<string, CacheObject> MessageListCache { get; set; }
		private static Dictionary<string, CacheObject> EmojiListCache { get; set; }

		static DiscordCache()
		{
			MessageListCache = new Dictionary<string, CacheObject>();
			EmojiListCache = new Dictionary<string, CacheObject>();
		}

		public static void newCacheObject(string cache, string hash, object toCache, TimeSpan expireTime)
		{
			try
			{
				Dictionary<string, CacheObject> useCache = typeof(DiscordCache).GetProperty(cache, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Dictionary<string, CacheObject>;
				CacheObject cacheObject = new CacheObject(toCache, expireTime);
				if (useCache.ContainsKey(hash))
					useCache.Remove(hash);
				useCache.Add(hash, cacheObject);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
			}
		}

		public static object getCacheObject(string cache, string hash)
		{
			try
			{
				Dictionary<string, CacheObject> useCache = typeof(DiscordCache).GetProperty(cache, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as Dictionary<string, CacheObject>;
				if (useCache.ContainsKey(hash))
				{
					var cachedObject = useCache[hash];
					var expired = cachedObject.isExpired();
					if (!expired)
						return useCache[hash].CachedObject;
					else
						useCache.Remove(hash);

				}
				return null;
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
				return null;
			}
		}
	}

	public class CacheObject
	{
		private DateTime timeCached;
		private TimeSpan expireTime;
		public object CachedObject;

		public CacheObject(object toCache, TimeSpan expireTime)
		{
			timeCached = DateTime.Now;
			this.expireTime = expireTime;
			CachedObject = toCache;
		}

		public bool isExpired()
		{
			if (timeCached + expireTime < DateTime.Now)
				return true;
			return false;
		}
	}
}