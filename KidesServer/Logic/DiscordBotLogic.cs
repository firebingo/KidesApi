using KidesServer.DataBase;
using KidesServer.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using KidesServer.Helpers;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Net;

namespace KidesServer.Logic
{
	public static class DiscordBotLogic
	{
		#region message list / user info
		public static DiscordMessageListResult getMessageList(DiscordMessageListInput input)
		{
			DiscordMessageListResult result = new DiscordMessageListResult();
			DiscordMessageListResult cacheResult = GeneralCache.getCacheObject("MessageListCache", input.hash) as DiscordMessageListResult;
			if (cacheResult != null)
				return cacheResult;

			result.results = new List<DiscordMessageListRow>();
			try
			{
				var queryString = string.Empty;
				if (input.startDate.HasValue)
				{
					queryString = $@"SELECT mainquery.userID, mainquery.nickName, mainquery.userName, mainquery.roleIDs, mainquery.mesCount, mainquery.isDeleted, mainquery.rank, mainquery.isBanned
									 FROM
									 (SELECT prequery.userID, prequery.nickName, prequery.userName, prequery.roleIDs, prequery.mesCount, prequery.isDeleted, @rownum := @rownum +1 as rank, prequery.isBanned
									 FROM ( SELECT @rownum := 0 ) r,
									 (SELECT usersinservers.userID, usersinservers.nickName, users.userName, usersinservers.roleIDs, COUNT(usersinservers.userID) AS mesCount, usersinservers.isDeleted, usersinservers.isBanned
									 FROM users 
									 LEFT JOIN usersinservers ON users.userID=usersinservers.userID
									 LEFT JOIN messages on messages.userID=users.userID
									 WHERE messages.serverID=@serverId AND usersinservers.serverID=@serverId AND NOT messages.isDeleted AND messages.mesTime > @startDate
									 GROUP BY messages.userID
									 ORDER BY mesCount DESC) prequery) mainquery
									 ORDER BY {messageListSortOrderToParam(input.sort, input.isDesc)}";
				}
				else
				{
					queryString = $@"SELECT mainquery.userID, mainquery.nickName, mainquery.userName, mainquery.roleIDs, mainquery.mesCount, mainquery.isDeleted, mainquery.rank, mainquery.isBanned
									 FROM
									 (SELECT prequery.userID, prequery.nickName, prequery.userName, prequery.roleIDs, prequery.mesCount, prequery.isDeleted, @rownum := @rownum +1 as rank, prequery.isBanned
									 FROM ( SELECT @rownum := 0 ) r,
									 (SELECT usersinservers.userID, usersinservers.nickName, users.userName, usersinservers.roleIDs, usersInServers.mesCount, usersinservers.isDeleted, usersinservers.isBanned
									 FROM users 
									 LEFT JOIN usersinservers ON users.userID=usersinservers.userID
									 WHERE usersinservers.serverID=@serverId AND usersinservers.mesCount > 0
									 ORDER BY mesCount DESC) prequery) mainquery
									 ORDER BY {messageListSortOrderToParam(input.sort, input.isDesc)}";
				}
				var readList = new MessageListReadModel();
				readList.rows = new List<MessageListReadModelRow>();
				DataLayerShortcut.ExecuteReader<List<MessageListReadModelRow>>(readMessageList, readList.rows, queryString, new MySqlParameter("@serverId", input.serverId), new MySqlParameter("@startDate", input.startDate));
				var roles = loadRoleList(input.serverId);
				//Add the rows to the result
				foreach (var r in readList.rows)
				{
					var message = new DiscordMessageListRow();
					message.userName = $"{r.userName}{(r.nickName != null ? $" ({r.nickName})" : "")}";
					message.messageCount = r.messageCount;
					message.userId = r.userId.ToString();
					message.isDeleted = r.isDeleted;
					message.rank = r.rank;
					message.isBanned = r.isBanned;
					message.role = buildRoleList(r.roleIds, roles);
					message.roleIds = new List<string>(r.roleIds.ConvertAll<string>(x => x.ToString()));
					result.results.Add(message);
				}
				//Filter by username/nickname
				if (input.userFilter != string.Empty)
					result.results = result.results.Where(x => x.userName.ToLowerInvariant().Contains(input.userFilter.ToLowerInvariant())).ToList();
				//Create the total row for the filtering
				DiscordMessageListRow totalRow = null;
				if (input.includeTotal)
				{
					totalRow = new DiscordMessageListRow();
					totalRow.userName = "Total";
					totalRow.messageCount = result.results.Sum(x => x.messageCount);
					totalRow.userId = string.Empty;
					totalRow.isDeleted = false;
					totalRow.rank = result.results.Count;
					totalRow.isBanned = false;
					totalRow.role = string.Empty;
					totalRow.roleIds = new List<string>();
				}
				//Filter by role
				DiscordMessageListRow totalRoleRow = null;
				if (input.roleId.HasValue)
				{
					result.results = result.results.Where(x => x.roleIds.Contains(input.roleId.Value.ToString())).ToList();
					if (input.includeTotal)
					{
						totalRoleRow = new DiscordMessageListRow();
						totalRoleRow.userName = "Total (Role)";
						totalRoleRow.messageCount = result.results.Sum(x => x.messageCount);
						totalRoleRow.userId = string.Empty;
						totalRoleRow.isDeleted = false;
						totalRoleRow.rank = result.results.Count;
						totalRoleRow.isBanned = false;
						totalRoleRow.role = buildRoleList(new List<ulong>() { input.roleId.Value }, roles);
						totalRoleRow.roleIds = roles.results.FirstOrDefault(x => x.roleId == input.roleId.Value.ToString()) == null ? new List<string>() : new List<string>() { input.roleId.ToString() };
					}
				}
				//Set total count of results without paging
				result.totalCount = result.results.Count;
				//Paging
				var countToTake = input.count;
				if (input.start > result.results.Count)
					input.start = result.results.Count;
				if (countToTake > result.results.Count - input.start)
					countToTake = result.results.Count - input.start;
				result.results = result.results.GetRange(input.start, countToTake);

				if (input.roleId.HasValue && totalRoleRow != null)
					result.results.Add(totalRoleRow);
				if (input.includeTotal && totalRow != null)
					result.results.Add(totalRow);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
				return new DiscordMessageListResult()
				{
					success = false,
					message = e.Message
				};
			}

			GeneralCache.newCacheObject("MessageListCache", input.hash, result, new TimeSpan(0, 10, 0));
			result.success = true;
			result.message = string.Empty;
			return result;
		}

		private static void readMessageList(IDataReader reader, List<MessageListReadModelRow> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null && reader.FieldCount >= 7)
			{
				var mesObject = new MessageListReadModelRow();
				ulong? temp = reader.GetValue(0) as ulong?;
				mesObject.userId = temp.HasValue ? temp.Value : 0;
				mesObject.nickName = reader.GetValue(1) as string;
				mesObject.userName = reader.GetValue(2) as string;
				var tempString = reader.GetValue(3) as string;
				if (tempString != null)
					mesObject.roleIds = JsonConvert.DeserializeObject<List<ulong>>(tempString);
				else
					mesObject.roleIds = new List<ulong>();
				mesObject.messageCount = reader.GetInt32(4);
				mesObject.isDeleted = reader.GetBoolean(5);
				mesObject.rank = reader.GetInt32(6);
				mesObject.isBanned = reader.GetBoolean(7);
				data.Add(mesObject);
			}
		}

		public static DiscordUserInfo getUserInfo(ulong userId, ulong serverId)
		{
			var result = new DiscordUserInfo();

			try
			{
				var queryString = @"SELECT users.userID, users.isBot, users.userName, usersinservers.nickName, usersinservers.avatarUrl, usersinservers.joinedDate, usersinservers.roleIDs, usersinservers.isDeleted, usersinservers.isBanned
									FROM users
									LEFT JOIN usersinservers ON users.userID=usersinservers.userID
									WHERE users.userID=@userId and serverID=@serverId;";
				var readUser = new UserInfoReadModel();
				DataLayerShortcut.ExecuteReader<UserInfoReadModel>(readUserInfo, readUser, queryString, new MySqlParameter("@serverId", serverId), new MySqlParameter("@userId", userId));
				if (readUser.userId == 0)
					throw new Exception("User not found");
				queryString = @"SELECT COUNT(*), MONTH(mesTime), YEAR(mesTime) 
								FROM messages 
								WHERE userID=@userId AND serverID=@serverId AND NOT isDeleted AND mesTime IS NOT NULL
								GROUP BY DATE_FORMAT(mesTime, '%Y%m')
								ORDER BY mesTime DESC;";
				List<DiscordUserMessageDensity> density = new List<DiscordUserMessageDensity>();
				DataLayerShortcut.ExecuteReader<List<DiscordUserMessageDensity>>(readUserMessageDensity, density, queryString, new MySqlParameter("@serverId", serverId), new MySqlParameter("@userId", userId));
				var roles = loadRoleList(serverId);
				result.userId = readUser.userId.ToString();
				result.userName = readUser.userName;
				result.nickName = readUser.nickName;
				result.isBot = readUser.isBot;
				result.avatarUrl = readUser.avatarUrl != null ? readUser.avatarUrl.Replace("size=128", "size=256") : null;
				result.joinedDate = readUser.joinedDate;
				result.isDeleted = readUser.isDeleted;
				result.isBanned = readUser.isBanned;
				result.messageDensity = density;
				result.role = buildRoleList(readUser.roleIds, roles);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
				return new DiscordUserInfo()
				{
					success = false,
					message = e.Message
				};
			}

			result.success = true;
			result.message = string.Empty;
			return result;
		}

		private static void readUserInfo(IDataReader reader, UserInfoReadModel data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null && reader.FieldCount >= 8)
			{
				ulong? temp = reader.GetValue(0) as ulong?;
				data.userId = temp.HasValue ? temp.Value : 0;
				data.isBot = reader.GetBoolean(1);
				data.userName = reader.GetValue(2) as string;
				data.nickName = reader.GetValue(3) as string;
				data.avatarUrl = reader.GetValue(4) as string;
				data.joinedDate = reader.GetValue(5) as DateTime?;
				var tempString = reader.GetValue(6) as string;
				if (tempString != null)
					data.roleIds = JsonConvert.DeserializeObject<List<ulong>>(tempString);
				else
					data.roleIds = new List<ulong>();
				data.isDeleted = reader.GetBoolean(7);
				data.isBanned = reader.GetBoolean(8);
			}
		}

		private static void readUserMessageDensity(IDataReader reader, List<DiscordUserMessageDensity> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null && reader.FieldCount >= 3)
			{
				var dObject = new DiscordUserMessageDensity();
				dObject.messageCount = reader.GetInt32(0);
				var month = reader.GetInt32(1);
				var year = reader.GetInt32(2);
				dObject.date = new DateTime(year, month, 1);
				dObject.date.ToUniversalTime();
				data.Add(dObject);
			}
		}

		private static string messageListSortOrderToParam(MessageSort sort, bool isDesc)
		{
			switch (sort)
			{
				default:
				case MessageSort.messageCount:
					return $"mainquery.rank {(isDesc ? "ASC" : "DESC")}";
				case MessageSort.userName:
					return $"COALESCE(mainquery.nickName, mainquery.userName) {(isDesc ? "DESC" : "ASC")}";
			}
		}
		#endregion

		#region role list
		public static DiscordRoleList getRoleList(ulong serverId)
		{
			var result = loadRoleList(serverId);
			return result;
		}

		private static DiscordRoleList loadRoleList(ulong serverId)
		{
			var results = new DiscordRoleList();
			results.results = new List<DiscordRoleListRow>();
			try
			{
				var queryString = @"SELECT roles.roleId, roles.roleName, roles.roleColor, roles.isEveryone
									FROM roles
									WHERE roles.serverID=@serverId AND NOT isDeleted
									ORDER BY roles.roleName";
				DataLayerShortcut.ExecuteReader<List<DiscordRoleListRow>>(readRoleList, results.results, queryString, new MySqlParameter("@serverId", serverId));
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
				return new DiscordRoleList()
				{
					success = false,
					message = e.Message
				};
			}

			results.success = true;
			results.message = string.Empty;
			return results;
		}

		private static void readRoleList(IDataReader reader, List<DiscordRoleListRow> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null && reader.FieldCount >= 4)
			{
				var roleObject = new DiscordRoleListRow();
				ulong? temp = reader.GetValue(0) as ulong?;
				roleObject.roleId = temp.HasValue ? temp.Value.ToString() : "0";
				roleObject.roleName = reader.GetString(1);
				roleObject.roleColor = reader.GetString(2);
				roleObject.isEveryone = reader.GetBoolean(3);
				data.Add(roleObject);
			}
		}

		private static string buildRoleList(List<ulong> roleIds, DiscordRoleList roles)
		{
			var roleBuilder = new StringBuilder();
			foreach (var Id in roleIds)
			{
				var role = roles.results.FirstOrDefault(x => ulong.Parse(x.roleId) == Id);
				if (role != null)
				{
					if (role.isEveryone)
						continue;
					var span = $"<span style=\"color:{role.roleColor};\">{role.roleName}</span>";
					if (roleBuilder.Length == 0)
						roleBuilder.Append(span);
					else
						roleBuilder.Append($", {span}");
				}
			}
			return roleBuilder.ToString();
		}
		#endregion

		#region emoji list
		public static DiscordEmojiListResult getEmojiList(DiscordEmojiListInput input)
		{
			DiscordEmojiListResult result = new DiscordEmojiListResult();
			DiscordEmojiListResult cacheResult = GeneralCache.getCacheObject("EmojiListCache", input.hash) as DiscordEmojiListResult;
			if (cacheResult != null)
				return cacheResult;

			try
			{
				result.results = new List<DiscordEmojiListRow>();

				var queryString = $@"SELECT mainquery.emojiID, mainquery.emojiName, mainquery.emCount, mainquery.rank
									 FROM
									 (SELECT prequery.emojiID, prequery.emojiName, prequery.emCount, @rownum := @rownum +1 as rank
									 FROM ( SELECT @rownum := 0 ) r,
									 (SELECT emojiID, emojiName, COUNT(*) AS emCount
									 FROM emojiuses
									 LEFT JOIN usersinservers on emojiuses.userID=usersinservers.userID
									 LEFT JOIN messages on emojiuses.messageID=messages.messageID
									 WHERE {(input.userFilterId.HasValue ? "usersinservers.userID=@userID AND" : "")} usersinservers.serverID=@serverId AND emojiuses.serverID=@serverId
									 AND messages.mesTime > @startDate AND emojiuses.userID!=@botId AND NOT emojiuses.isDeleted AND NOT messages.isDeleted AND messages.mesText NOT LIKE '%emojicount%' 
									 GROUP BY emojiID
									 ORDER BY emCount DESC) prequery) mainquery
									 ORDER BY {emojiListSortOrderToParam(input.sort, input.isDesc)}";
				DataLayerShortcut.ExecuteReader<List<DiscordEmojiListRow>>(readEmojiList, result.results, queryString, new MySqlParameter("@serverId", input.serverId),
					new MySqlParameter("@startDate", input.startDate), new MySqlParameter("@botId", AppConfig.config.botId), new MySqlParameter("@userID", (input.userFilterId.HasValue ? input.userFilterId.Value : 0)));

				//Filter by emojiname
				if (input.nameFilter != string.Empty)
					result.results = result.results.Where(x => x.emojiName.ToLowerInvariant().Contains(input.nameFilter.ToLowerInvariant())).ToList();

				//Create the total row for the filtering
				DiscordEmojiListRow totalRow = null;
				if (input.includeTotal)
				{
					totalRow = new DiscordEmojiListRow();
					totalRow.emojiName = "Total";
					totalRow.useCount = result.results.Sum(x => x.useCount);
					totalRow.emojiId = string.Empty;
					totalRow.emojiImg = string.Empty;
					totalRow.rank = result.results.Count;
				}

				//Set total count of results without paging
				result.totalCount = result.results.Count;
				//Paging
				var countToTake = input.count;
				if (input.start > result.results.Count)
					input.start = result.results.Count;
				if (countToTake > result.results.Count - input.start)
					countToTake = result.results.Count - input.start;
				result.results = result.results.GetRange(input.start, countToTake);

				if (input.includeTotal && totalRow != null)
					result.results.Add(totalRow);
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
				return new DiscordEmojiListResult()
				{
					success = false,
					message = e.Message
				};
			}

			GeneralCache.newCacheObject("EmojiListCache", input.hash, result, new TimeSpan(0, 10, 0));
			result.success = true;
			result.message = string.Empty;
			return result;
		}

		private static void readEmojiList(IDataReader reader, List<DiscordEmojiListRow> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null && reader.FieldCount >= 4)
			{
				var emObject = new DiscordEmojiListRow();
				ulong? temp = reader.GetValue(0) as ulong?;
				emObject.emojiId = (temp.HasValue ? temp.Value : 0).ToString();
				emObject.emojiName = reader.GetValue(1) as string;
				emObject.useCount = reader.GetInt32(2);
				emObject.rank = reader.GetInt32(3);
				//For now im going to check these on the frontend because it takes a long time to try to request and check each image.
				//var imageGif = $"https://cdn.discordapp.com/emojis/{emObject.emojiId}.gif";
				emObject.emojiImg = $"https://cdn.discordapp.com/emojis/{emObject.emojiId}.png";
				data.Add(emObject);
			}
		}

		private static bool doesImageGifExist(string url)
		{
			HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
			request.Timeout = 750;
			request.Method = "HEAD";
			request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
			request.Headers.Add("accept-encoding", "gzip, deflate, br");
			try
			{
				using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
				{
					return (response.StatusCode == HttpStatusCode.OK);
				}
			}
			catch
			{
				return false;
			}
		}

		private static string emojiListSortOrderToParam(EmojiSort sort, bool isDesc)
		{
			switch (sort)
			{
				default:
				case EmojiSort.emojiCount:
					return $"mainquery.rank {(isDesc ? "ASC" : "DESC")}";
				case EmojiSort.emojiName:
					return $"mainquery.emojiName {(isDesc ? "DESC" : "ASC")}";
			}
		}
		#endregion

		#region word counts
		public static async Task<DiscordWordListResult> getWordCountList(DiscordWordListInput input)
		{
			DiscordWordListResult result = new DiscordWordListResult();
			DiscordWordListResult cacheResult = GeneralCache.getCacheObject("WordCountListCache", input.hash) as DiscordWordListResult;
			if (cacheResult != null)
				return cacheResult;

			var messages = await loadMessagesText(input.startDate);
			ConcurrentDictionary<string, ConcurrentDictionary<ulong, int>> words = null;
			var discordMentionReg = new Regex(@"<@\d+>");
			var discordEmojiReg = new Regex(@"<:.+:\d+>");
			var discordIdenReg = new Regex(@"^.+#\d{4}");
			var fileReg = new Regex(@"^.+\..+$");
			var foreignERegex = new Regex(@"[^\u0000-\u007F]");
			var foreignIRegex = new Regex(@"\p{L}");
			var ignoreChars = new List<char>() { '`', '~', '$', '%', '^', '|', '+', '=', '<', '>', '\n', '\r', '\t', '\b', '\v', '\a', '\0', 'ﾟ', 'ˆ', 'ᵃ', 'ｰ', 'ー', 'ˈ', 'ː' };
			var parOpts = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 };

			var wordsCached = GeneralCache.containsCacheObject("MessageTextCache", $"WordsList:{(input.startDate.HasValue ? input.startDate.Value.Ticks.ToString() : "_")}:{input.englishOnly.ToString()}");
			if (wordsCached)
				words = GeneralCache.getCacheObject("MessageTextCache", $"WordsList:{(input.startDate.HasValue ? input.startDate.Value.Ticks.ToString() : "_")}:{input.englishOnly.ToString()}") as ConcurrentDictionary<string, ConcurrentDictionary<ulong, int>>;

			if (words == null)
			{
				//remove code blocks
				messages = messages.Where(x => !x.mesText.Contains("```")).ToList();
				words = new ConcurrentDictionary<string, ConcurrentDictionary<ulong, int>>();
				Parallel.ForEach(messages, parOpts, (message) =>
				{
					//split the messages on spaces/new lines.
					var split = message.mesText.Trim().Split(new[] { "\r\n", "\r", "\n", " " }, StringSplitOptions.RemoveEmptyEntries);
					//remove words with http (probably links), discord formats, or files
					split = split.Where(x => !x.Contains("http") && !discordMentionReg.IsMatch(x) && !discordEmojiReg.IsMatch(x) && !discordIdenReg.IsMatch(x) && !fileReg.IsMatch(x)).ToArray();
					//remove puncuation/unicode and lowercase all words.
					for (var i = 0; i < split.Length; ++i)
					{
						if (input.englishOnly)
							split[i] = foreignERegex.Replace(split[i], "");
						else
							split[i] = foreignIRegex.Matches(split[i]).Cast<Match>().Aggregate("", (s, e) => s + e.Value, s => s);
						split[i] = new string(split[i].Where(x => !char.IsPunctuation(x) && !ignoreChars.Contains(x) && !char.IsNumber(x) && !char.IsSurrogate(x)).ToArray());
						split[i] = split[i].ToLowerInvariant();
					}
					//remove empty entries.
					split = split.Where(x => x.Trim() != string.Empty).ToArray();
					//if the word is just a string of the same characters
					split = split.Where(x => (x.Length > 3 ? x.Distinct().Count() > 1 : true)).ToArray();
					//remove long words that are just 2 characters
					split = split.Where(x => (x.Length > 6 ? x.Distinct().Count() > 2 : true)).ToArray();
					//remove longer words that are just 3 characters
					split = split.Where(x => (x.Length >= 10 ? x.Distinct().Count() > 3 : true)).ToArray();
					//this is getting dumb
					split = split.Where(x => (x.Length > 50 ? x.Distinct().Count() > 4 : true)).ToArray();
					//lets put a "resonable" cap on this
					split = split.Where(x => x.Length <= 125).ToArray();
					for (var i = 0; i < split.Length; ++i)
					{
						//add each word with its own dictionary with the userid and count for that user.
						words.AddOrUpdate(split[i], (newKey) =>
							{
								var ret = new ConcurrentDictionary<ulong, int>();
								ret.TryAdd(message.userId, 1);
								return ret;
							},
							(key, oldValue) =>
							{
								oldValue.AddOrUpdate(message.userId, 1, (keyI, oldValueI) => ++oldValueI);
								return oldValue;
							});
					}
				});
			}

			if (!wordsCached)
				GeneralCache.newCacheObject("MessageTextCache", $"WordsList:{(input.startDate.HasValue ? input.startDate.Value.Ticks.ToString() : "_")}:{input.englishOnly.ToString()}", words, new TimeSpan(12, 0, 0));

			//Generate total row
			DiscordWordListRow totalRow = null;
			if (input.includeTotal)
			{
				totalRow = new DiscordWordListRow();
				var totalCount = words.Sum(x =>
				{
					return x.Value.Sum(y => y.Value);
				});
				totalRow.rank = words.Count;
				totalRow.useCount = totalCount;
				totalRow.word = "(Total)";
			}
			//User filtering and user total row
			DiscordWordListRow totalUserRow = null;
			Dictionary<string, Dictionary<ulong, int>> userWords = null;
			if (input.userFilterId.HasValue)
			{
				totalUserRow = new DiscordWordListRow();
				userWords = new Dictionary<string, Dictionary<ulong, int>>(words.Where(x => x.Value.Count != 0).ToDictionary(k => k.Key, v => v.Value.ToDictionary(i => i.Key, j => j.Value)));
				foreach(var word in userWords)
				{
					var ids = word.Value.Keys.ToArray();
					ids = ids.Where(x => x != input.userFilterId.Value).ToArray();
					foreach (var id in ids)
					{
						word.Value.Remove(id);
					}
				}
				userWords = userWords.Where(x => x.Value.Count != 0).ToDictionary(k => k.Key, v => v.Value);
				var totalCount = userWords.Sum(x =>
				{
					return x.Value.Sum(y => y.Value);
				});
				totalUserRow.rank = userWords.Count;
				totalUserRow.useCount = totalCount;
				totalUserRow.word = "(Total (User))";
			}

			//add the words to the result list.
			result.results = new List<DiscordWordListRow>();
			if (userWords != null)
			{
				foreach (var word in userWords)
				{
					var row = new DiscordWordListRow();
					row.word = word.Key;
					row.useCount = word.Value.Sum(x => x.Value);
					result.results.Add(row);
				}
			}
			else
			{
				foreach (var word in words)
				{
					var row = new DiscordWordListRow();
					row.word = word.Key;
					row.useCount = word.Value.Sum(x => x.Value);
					result.results.Add(row);
				}
			}

			//Sort by count desc and rank
			result.results.Sort((x, y) =>
			{
				return y.useCount.CompareTo(x.useCount);
			});
			Parallel.For(0, result.results.Count, parOpts, (i) =>
			{
				result.results[i].rank = i + 1;
			});

			//Word Filtering
			if (!string.IsNullOrWhiteSpace(input.wordFilter))
				result.results = result.results.Where(x => x.word.StartsWith(input.wordFilter.ToLowerInvariant())).ToList();

			//Length Filtering
			if (input.lengthFloor > 0)
				result.results = result.results.Where(x => x.word.Length >= input.lengthFloor).ToList();

			//Sort
			switch (input.sort)
			{
				case WordCountSort.count:
					result.results.Sort((x, y) =>
					{
						if (input.isDesc)
							return x.rank.CompareTo(y.rank);
						else
							return y.rank.CompareTo(x.rank);

					});
					break;
				case WordCountSort.word:
					result.results.Sort((x, y) =>
					{
						if (!input.isDesc)
							return x.word.CompareTo(y.word);
						else
							return y.word.CompareTo(x.word);
					});
					break;
				default:
					break;
			}

			//Set total count of results without paging
			result.totalCount = result.results.Count;
			//Paging
			var countToTake = input.count;
			if (input.start > result.results.Count)
				input.start = result.results.Count;
			if (countToTake > result.results.Count - input.start)
				countToTake = result.results.Count - input.start;
			result.results = result.results.GetRange(input.start, countToTake);

			//Add total rows
			if (input.includeTotal)
			{
				if (input.userFilterId.HasValue)
					result.results.Add(totalUserRow);
				result.results.Add(totalRow);
			}

			GeneralCache.newCacheObject("WordCountListCache", input.hash, result, new TimeSpan(12, 0, 0));
			GC.Collect();
			result.success = true;
			result.message = string.Empty;
			return result;
		}

		public static async Task<List<MessageTextModel>> loadMessagesText(DateTime? startDate)
		{
			var result = new List<MessageTextModel>();
			List<MessageTextModel> cacheResult = GeneralCache.getCacheObject("MessageTextCache", $"MessageTextList{(startDate.HasValue ? startDate.Value.Ticks.ToString() : "0")}") as List<MessageTextModel>;
			if (cacheResult != null)
				return cacheResult;

			var query = $@"SELECT txt, userID FROM 
						  (SELECT COALESCE(mesText, editedMesText) AS txt, userID
						  FROM messages 
						  WHERE NOT isDeleted AND userId != @botId AND mesTime > @startDate) x
						  WHERE txt != '' AND txt NOT LIKE '%@botId%'";
			DataLayerShortcut.ExecuteReader<List<MessageTextModel>>(readMessagesText, result, query,
				new MySqlParameter("@botId", AppConfig.config.botId), new MySqlParameter("@startDate", startDate.HasValue ? startDate.Value : DateTime.MinValue));

			GeneralCache.newCacheObject("MessageTextCache", $"MessageTextList{(startDate.HasValue ? startDate.Value.Ticks.ToString() : "0")}", result, new TimeSpan(12, 0, 0));
			return result;
		}

		private static void readMessagesText(IDataReader reader, List<MessageTextModel> data)
		{
			reader = reader as MySqlDataReader;
			if (reader != null)
			{
				var message = new MessageTextModel(reader.GetString(0), (reader.GetValue(1) as ulong?).Value);
				data.Add(message);
			}
		}
		#endregion
	}

	public class MessageListReadModel
	{
		public List<MessageListReadModelRow> rows;
	}

	public class MessageListReadModelRow
	{
		public string userName;
		public string nickName;
		public List<ulong> roleIds;
		public int messageCount;
		public ulong userId;
		public bool isDeleted;
		public int rank;
		public bool isBanned;
	}

	public class UserInfoReadModel
	{
		public ulong userId;
		public string userName;
		public string nickName;
		public bool isBot;
		public List<ulong> roleIds;
		public string avatarUrl;
		public DateTime? joinedDate;
		public bool isDeleted;
		public bool isBanned;
	}

	[Serializable]
	public struct MessageTextModel
	{
		private readonly string _mesText;
		private readonly ulong _userId;
		public string mesText { get { return _mesText; } }
		public ulong userId { get { return _userId; } }

		public MessageTextModel(string t, ulong userId)
		{
			_mesText = t;
			_userId = userId;
		}
	}
}