﻿using KidesServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Tx.Windows;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using KidesServer.Helpers;

namespace KidesServer.Logic
{
	public static class MusicLogic
	{
		public static string baseUrl = AppConfig.config.baseMusicUrl;
		public static SongList songList = new SongList();
		private static bool songFound = false;
		private static SongModel foundSong = null;

		static MusicLogic()
		{
			songList = JsonConvert.DeserializeObject<SongList>(File.ReadAllText($"{AppConfig.folderLocation}\\SongList.json"));
		}

		public static SongSearchResult searchForSong(string search)
		{
			SongSearchResult result = new SongSearchResult();
			result.success = false;
			result.message = "SONG_NOT_FOUND";
			result.url = baseUrl;
			songFound = false;
			foundSong = null;

			var start = DateTime.Now;
			Parallel.ForEach(songList.songList, (song, ParallelLoopState) =>
			{
				var found = checkSong(song, search);
				if(found != null)
				{
					foundSong = found;
					songFound = true;
					ParallelLoopState.Stop();
				}
			});

			if (songFound && foundSong != null)
			{
				result.url = ($"{baseUrl}/{foundSong.Directory}/{foundSong.Url}");
				result.success = true;
				result.message = "";
			}
			else
			{
				result.success = false;
				result.message = "SONG_NOT_FOUND";
			}
			var end = DateTime.Now;
			var length = (end - start).TotalMilliseconds;

			return result;
		}

		public static SongModel checkSong(SongModel song, string search)
		{
			Regex searchReg = new Regex(search.ToLowerInvariant());
			var titleCat = $"{song.English.ToLowerInvariant()}|{song.Roman.ToLowerInvariant()}|{song.Japanese.ToLowerInvariant()}|{song.Hiragana.ToLowerInvariant()}";
			if (searchReg.Match(titleCat).Success)
			{
				return song;
			}
			return null;
		}

		public static SongStatResult getSongStats()
		{
			SongStatResult result = new SongStatResult();
			SongStatResult cacheResult = GeneralCache.getCacheObject("SongStatCache", "GeneralSongStats") as SongStatResult;
			if (cacheResult != null)
				return cacheResult;

			result.message = "";
			result.success = false;
			result.songCounts = new Dictionary<string, int>();

			var logPath = "";
			List<string> logFiles = new List<string>();
			try
			{
				logFiles = Directory.GetFiles(AppConfig.config.iisLogLocation).ToList();
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
				result.message = e.Message;
				return result;
			}

			foreach (var file in logFiles)
			{
				try
				{
					var tempFile = $"{file}.temp";
					File.Copy(file, tempFile);
					var logParse = W3CEnumerable.FromFile(tempFile);
					var songs = logParse.Where(x => x.cs_method == "GET" && x.cs_uri_stem.Contains(".ogg") && Int32.Parse(x.sc_bytes) > 3000).ToList();
					foreach (var song in songs)
					{
						var fullUrl = song.cs_uri_stem.Replace('+', ' ');
						var splitUrl = fullUrl.Split('/');
						var title = splitUrl[splitUrl.Length - 1].Replace(".ogg", string.Empty);
						if (result.songCounts.ContainsKey(title))
						{
							++result.songCounts[title];
						}
						else
						{
							result.songCounts.Add(title, 1);
						}
					}
					File.Delete(tempFile);
				}
				catch (Exception e)
				{
					ErrorLog.writeLog(e.Message);
					result.message = e.Message;
					return result;
				}
			}

			GeneralCache.newCacheObject("SongStatCache", "GeneralSongStats", result, new TimeSpan(12, 0, 0));
			result.success = true;
			result.message = "";
			return result;
		}
	}
}