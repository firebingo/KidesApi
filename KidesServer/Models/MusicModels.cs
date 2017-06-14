using System;
using System.Collections.Generic;

namespace KidesServer.Models
{
	[Serializable]
	public class SongList
	{
		public List<SongModel> songList;
	}

	[Serializable]
	public class SongModel
	{
		public string Roman;
		public string English;
		public string Japanese;
		public string Hiragana;
		public string Url;
		public string Artist;
		public string Directory;
	}

	[Serializable]
	public class SongSearchResult : BaseResult
	{
		public string url;
	}

	[Serializable]
	public class SongStatResult : BaseResult
	{
		public Dictionary<string, int> songCounts;
	}
}