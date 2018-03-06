using System.Net;
using System.Net.Http;
using System.Web.Http;
using KidesServer.Models;
using KidesServer.Logic;
using System.Threading.Tasks;
using System;

namespace KidesServer.Controllers
{
	[RoutePrefix("api/v1/discord")]
	public class DiscordBotController : ApiController
	{
		[HttpGet, Route("message-count/list")]
		public async Task<HttpResponseMessage> getMessageList([FromUri]int count, [FromUri]ulong serverId, [FromUri]int start, [FromUri]DateTime? startDate = null, [FromUri]MessageSort sort = MessageSort.messageCount,
			[FromUri]bool isDesc = true, [FromUri]string userFilter = "", [FromUri]ulong? roleId = null, [FromUri]bool includeTotal = false)
		{
			var input = new DiscordMessageListInput(count, serverId, start, (startDate.HasValue ? startDate : null), sort, isDesc, userFilter, roleId, includeTotal);
			var result = DiscordBotLogic.getMessageList(input);

			if (result.success)
				return Request.CreateResponse(HttpStatusCode.OK, result);
			else
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.message);
		}

		[HttpGet, Route("user-info")]
		public async Task<HttpResponseMessage> getUserInfo([FromUri]ulong userId, [FromUri]ulong serverId)
		{
			var result = DiscordBotLogic.getUserInfo(userId, serverId);

			if (result.success)
				return Request.CreateResponse(HttpStatusCode.OK, result);
			else
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.message);
		}

		[HttpGet, Route("roles")]
		public async Task<HttpResponseMessage> getRoles([FromUri]ulong serverId)
		{
			var result = DiscordBotLogic.getRoleList(serverId);

			if (result.success)
				return Request.CreateResponse(HttpStatusCode.OK, result);
			else
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.message);
		}

		[HttpGet, Route("emoji-count/list")]
		public async Task<HttpResponseMessage> getEmojiList([FromUri]int count, [FromUri]ulong serverId, [FromUri]int start, [FromUri]DateTime? startDate = null, [FromUri]EmojiSort sort = EmojiSort.emojiCount,
			[FromUri]bool isDesc = true, [FromUri]string nameFilter = "", [FromUri]bool includeTotal = false, [FromUri]ulong? userFilterId = null)
		{
			var input = new DiscordEmojiListInput(count, serverId, start, (startDate.HasValue ? startDate.Value : DateTime.MinValue), sort, isDesc, nameFilter, includeTotal, userFilterId);
			var result = DiscordBotLogic.getEmojiList(input);

			if (result.success)
				return Request.CreateResponse(HttpStatusCode.OK, result);
			else
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.message);
		}

		[HttpGet, Route("word-count/list")]
		public async Task<HttpResponseMessage> getWordCountList([FromUri]int count, [FromUri]ulong serverId, [FromUri]int start, [FromUri]DateTime? startDate = null, [FromUri]WordCountSort sort = WordCountSort.count,
			[FromUri]bool isDesc = true, [FromUri]string wordFilter = "", [FromUri]bool includeTotal = false, [FromUri]ulong? userFilterId = null, int lengthFloor = 0, bool englishOnly = false)
		{
			var input = new DiscordWordListInput(count, serverId, start, startDate, sort, isDesc, wordFilter, includeTotal, userFilterId, lengthFloor, englishOnly);
			var result = await DiscordBotLogic.getWordCountList(input);

			if (result.success)
				return Request.CreateResponse(HttpStatusCode.OK, result);
			else
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.message);
		}
	}
}