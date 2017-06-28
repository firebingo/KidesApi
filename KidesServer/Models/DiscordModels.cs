using System;
using System.Collections.Generic;

namespace KidesServer.Models
{
	public class DiscordMessageListResult : BaseResult
	{
		public List<DiscordMessageListRow> results;
	}

	public class DiscordMessageListRow
	{
		public string userName;
		public string role;
		public int messageCount;
		public string userId;
		public bool isDeleted;
		public int rank;
		public bool isBanned;
	}

	public class DiscordUserInfo : BaseResult
	{
		public string userId;
		public string userName;
		public string nickName;
		public bool isBot;
		public string role;
		public string avatarUrl;
		public DateTime? joinedDate;
		public bool isDeleted;
		public bool isBanned;
		public List<DiscordUserMessageDensity> messageDensity;
	}

	public class DiscordUserMessageDensity
	{
		public int messageCount;
		public DateTime date;
	}

	public class DiscordRoleList : BaseResult
	{
		public List<DiscordRoleListRow> results;
	}

	public class DiscordRoleListRow
	{
		public string roleId;
		public string roleName;
		public string roleColor;
		public bool isEveryone;
	}

	public enum MessageSort
	{
		userName,
		messageCount
	}
}