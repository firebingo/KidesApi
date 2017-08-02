using System;
using System.Collections.Generic;

namespace KidesServer.Models
{
	public class DiscordMessageListInput
	{
		public int count;
		public ulong serverId;
		public int start;
		public DateTime? startDate;
		public MessageSort sort;
		public bool isDesc;
		public string userFilter;
		public ulong? roleId;
		public bool includeTotal;
		public string hash 
		{
			get
			{
				return $"{count.ToString()}:{serverId.ToString()}:{start.ToString()}:{(startDate.HasValue ? startDate.Value.ToString() : "0")}:{sort.ToString()}:" +
					$"{isDesc.ToString()}:{userFilter}:{(roleId.HasValue ? roleId.ToString() : "0")}:{includeTotal.ToString()}";
			}
		}

		public DiscordMessageListInput(int count, ulong serverId, int start, DateTime? startDate, MessageSort sort, bool isDesc, string userFilter, ulong? roleId, bool includeTotal)
		{
			this.count = count;
			this.serverId = serverId;
			this.start = start;
			this.startDate = startDate;
			this.sort = sort;
			this.isDesc = isDesc;
			this.userFilter = userFilter;
			this.roleId = roleId;
			this.includeTotal = includeTotal;
		}
	}

	public class DiscordMessageListResult : BaseResult
	{
		public int totalCount;
		public List<DiscordMessageListRow> results;
	}

	public class DiscordMessageListRow
	{
		public string userName;
		public string role;
		public List<string> roleIds;
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

	public class DiscordEmojiListInput
	{
		public int count;
		public ulong serverId;
		public int start;
		public DateTime? startDate;
		public EmojiSort sort;
		public bool isDesc;
		public string nameFilter;
		public bool includeTotal;
		public ulong? userFilterId;
		public string hash
		{
			get
			{
				return $"{count.ToString()}:{serverId.ToString()}:{start.ToString()}:{(startDate.HasValue ? startDate.Value.ToString() : "0")}:{sort.ToString()}:" +
					$"{isDesc.ToString()}:{nameFilter}:{includeTotal.ToString()}:{(userFilterId.HasValue ? userFilterId.Value.ToString() : "0")}";
			}
		}

		public DiscordEmojiListInput(int count, ulong serverId, int start, DateTime? startDate, EmojiSort sort, bool isDesc, string nameFilter, bool includeTotal, ulong? userFilterId)
		{
			this.count = count;
			this.serverId = serverId;
			this.start = start;
			this.startDate = startDate;
			this.sort = sort;
			this.isDesc = isDesc;
			this.nameFilter = nameFilter;
			this.includeTotal = includeTotal;
			this.userFilterId = userFilterId;
		}
	}

	public class DiscordEmojiListResult : BaseResult
	{
		public int totalCount;
		public List<DiscordEmojiListRow> results;
	}

	public class DiscordEmojiListRow
	{
		public string emojiId;
		public string emojiName;
		public int useCount;
		public int rank;
		public string emojiImg;
	}

	public enum EmojiSort
	{
		emojiName,
		emojiCount
	}
}