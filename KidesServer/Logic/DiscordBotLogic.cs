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
		public static DiscordMessageListResult GetMesageList(int count, ulong serverId, DateTime startDate, MessageSort sort, bool isDesc, string userFilter)
		{
			DiscordMessageListResult result = new DiscordMessageListResult();
			result.results = new List<DiscordMessageListRow>();
			try
			{
				var queryString = $@"SELECT messages.userID, usersinservers.nickName, users.userName, usersinservers.roleIDs, count(messages.userID) AS mesCount, usersinservers.isDeleted
									 FROM messages 
									 LEFT JOIN usersinservers ON messages.userID=usersinservers.userID
									 LEFT JOIN users on usersinservers.userID=users.userID 
									 WHERE messages.serverID=@serverId AND usersinservers.serverID=@serverId AND NOT messages.isDeleted 
									 GROUP BY userID 
									 ORDER BY {messageListSortOrderToParam(sort)} {(isDesc ? "DESC" : "ASC")}
									 LIMIT @limit";
				var readList = new MessageListReadModel();
				readList.rows = new List<MessageListReadModelRow>();
				DataLayerShortcut.ExecuteReader<List<MessageListReadModelRow>>(readMessageList, readList.rows, queryString, new MySqlParameter("@serverId", serverId), new MySqlParameter("@limit", count));
				var roles = loadRoleList(serverId);
				foreach(var r in readList.rows)
				{
					var message = new DiscordMessageListRow();
					message.userName = $"{r.userName}{(r.nickName != null ? $" ({r.nickName})" : "")}";
					message.messageCount = r.messageCount;
					message.userId = r.userId;
					message.isDeleted = r.isDeleted;
					var roleBuilder = new StringBuilder();
					foreach (var Id in r.roleIds)
					{
						var role = roles.results.FirstOrDefault(x => x.roleId == Id);
						if (role != null)
						{
							if (role.isEveryone || role.roleId == 229598038438445056)
								continue;
							var span = $"<span style=\"color:{role.roleColor};\">{role.roleName}</span>";
							if (roleBuilder.Length == 0)
								roleBuilder.Append(span);
							else
								roleBuilder.Append($", {span}");
						}
					}
					message.role = roleBuilder.ToString();
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
			if (reader != null && reader.FieldCount >= 6)
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
				data.Add(mesObject);
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

		private static string messageListSortOrderToParam(MessageSort sort)
		{
			switch (sort)
			{
				default:
				case MessageSort.messageCount:
					return "mesCount";
				case MessageSort.userName:
					return "COALESCE(nickName, userName)";
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
	}
}