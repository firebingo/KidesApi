using System;
using System.Collections.Generic;

namespace KidesServer.Helpers
{
	public static class GeneralCache
	{
		private static Dictionary<string, Dictionary<string, CacheObject>> ObjectCache = new Dictionary<string, Dictionary<string, CacheObject>>();
		private static object dictLock = new object();

		public static void newCacheObject(string cache, string hash, object toCache, TimeSpan expireTime)
		{
			try
			{
				lock (dictLock)
				{
					if (!ObjectCache.ContainsKey(cache))
						ObjectCache.Add(cache, new Dictionary<string, CacheObject>());

					CacheObject cacheObject = new CacheObject(toCache, expireTime);
					if (ObjectCache[cache].ContainsKey(hash))
						ObjectCache[cache].Remove(hash);
					ObjectCache[cache].Add(hash, cacheObject);
				}
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
				lock (dictLock)
				{
					if (!ObjectCache.ContainsKey(cache))
						return null;
					if (ObjectCache[cache].ContainsKey(hash))
					{
						var cachedObject = ObjectCache[cache][hash];
						var expired = cachedObject.isExpired();
						if (!expired)
							return ObjectCache[cache][hash].CachedObject;
						else
							ObjectCache[cache].Remove(hash);

					}
					return null;
				}
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