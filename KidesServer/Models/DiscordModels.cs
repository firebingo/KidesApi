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
		public ulong userId;
		public bool isDeleted;
	}

	public class DiscordRoleList : BaseResult
	{
		public List<DiscordRoleListRow> results;
	}

	public class DiscordRoleListRow
	{
		public ulong roleId;
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