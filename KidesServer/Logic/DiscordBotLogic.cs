using KidesServer.DataBase;
using KidesServer.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace KidesServer.Logic
{
	public static class DiscordBotLogic
	{
		public static DiscordMessageListResult getMesageList(int count, ulong serverId, DateTime startDate, MessageSort sort, bool isDesc, string userFilter)
		{
			DiscordMessageListResult result = new DiscordMessageListResult();
			result.results = new List<DiscordMessageListRow>();
			try
			{
				var queryString = $@"SELECT mainquery.userID, mainquery.nickName, mainquery.userName, mainquery.roleIDs, mainquery.mesCount, mainquery.isDeleted, mainquery.rank, mainquery.isBanned
									 FROM
									 (SELECT prequery.userID, prequery.nickName, prequery.userName, prequery.roleIDs, prequery.mesCount, prequery.isDeleted, @rownum := @rownum +1 as rank, prequery.isBanned
									 FROM ( SELECT @rownum := 0 ) r,
									 (SELECT messages.userID, usersinservers.nickName, users.userName, usersinservers.roleIDs, COUNT(messages.userID) AS mesCount, usersinservers.isDeleted, usersinservers.isBanned
									 FROM messages 
									 LEFT JOIN usersinservers ON messages.userID=usersinservers.userID
									 LEFT JOIN users on usersinservers.userID=users.userID
									 WHERE messages.serverID=@serverId AND usersinservers.serverID=@serverId AND NOT messages.isDeleted AND messages.mesTime > @startDate AND COALESCE(nickName, userName) LIKE @userFilter GROUP BY userID
									 ORDER BY mesCount DESC) prequery) mainquery
									 ORDER BY {messageListSortOrderToParam(sort, isDesc)}
									 LIMIT @limit;";
				var readList = new MessageListReadModel();
				readList.rows = new List<MessageListReadModelRow>();
				DataLayerShortcut.ExecuteReader<List<MessageListReadModelRow>>(readMessageList, readList.rows, queryString, new MySqlParameter("@serverId", serverId), 
					new MySqlParameter("@limit", count), new MySqlParameter("@userFilter", (userFilter == null ? string.Empty : $"%{userFilter}%")), new MySqlParameter("@startDate", startDate));
				var roles = loadRoleList(serverId);
				foreach(var r in readList.rows)
				{
					var message = new DiscordMessageListRow();
					message.userName = $"{r.userName}{(r.nickName != null ? $" ({r.nickName})" : "")}";
					message.messageCount = r.messageCount;
					message.userId = r.userId;
					message.isDeleted = r.isDeleted;
					message.rank = r.rank;
					message.isBanned = r.isBanned;
					message.role = buildRoleList(r.roleIds, roles);
					result.results.Add(message);
				}
				if(userFilter != string.Empty)
					result.results = result.results.Where(x => x.userName.ToLowerInvariant().Contains(userFilter.ToLowerInvariant())).ToList();
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
				queryString = @"SELECT COUNT(*), MONTH(mesTime), YEAR(mesTime) 
								FROM messages 
								WHERE userID=@userId AND serverID=@serverId AND NOT isDeleted 
								GROUP BY DATE_FORMAT(mesTime, '%Y%m')
								ORDER BY mesTime DESC;";
				List<DiscordUserMessageDensity> density = new List<DiscordUserMessageDensity>();
				DataLayerShortcut.ExecuteReader<List<DiscordUserMessageDensity>>(readUserMessageDensity, density, queryString, new MySqlParameter("@serverId", serverId), new MySqlParameter("@userId", userId));
				var roles = loadRoleList(serverId);
				result.userId = readUser.userId;
				result.userName = readUser.userName;
				result.nickName = readUser.nickName;
				result.isBot = readUser.isBot;
				result.avatarUrl = readUser.avatarUrl.Replace("size=128", "size=256");
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

		private static DiscordRoleList loadRoleList(ulong serverId)
		{
			var results = new DiscordRoleList();
			results.results = new List<DiscordRoleListRow>();
			try
			{
				var queryString = @"SELECT roles.roleId, roles.roleName, roles.roleColor, roles.isEveryone
									FROM roles
									WHERE roles.serverID=@serverId";
				DataLayerShortcut.ExecuteReader<List<DiscordRoleListRow>>(readRoleList, results.results, queryString, new MySqlParameter("@serverId", serverId));
			}
			catch(Exception e)
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
				roleObject.roleId = temp.HasValue ? temp.Value : 0;
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
				var role = roles.results.FirstOrDefault(x => x.roleId == Id);
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
}