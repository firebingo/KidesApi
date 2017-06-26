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
		public async Task<HttpResponseMessage> getMessageList([FromUri]int count, [FromUri]ulong serverId, [FromUri]DateTime? startDate = null, [FromUri]MessageSort sort = MessageSort.messageCount, [FromUri]bool isDesc = true, [FromUri]string userFilter = "")
		{
			var result = DiscordBotLogic.getMesageList(count, serverId, startDate.HasValue ? startDate.Value.ToUniversalTime() : DateTime.MinValue, sort, isDesc, userFilter);

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
	}
}