using System.Net;
using System.Net.Http;
using System.Web.Http;
using KidesServer.Models;
using KidesServer.Logic;
using System.Threading.Tasks;
using System;

namespace KidesServer.Controllers
{
	[RoutePrefix("api/v1")]
	public class DiscordBotController : ApiController
	{
		[HttpGet, Route("message-count/list")]
		public async Task<HttpResponseMessage> getMessageList([FromUri]int count, [FromUri]ulong serverId, [FromUri]DateTime? startDate = null, [FromUri]MessageSort sort = MessageSort.messageCount, [FromUri]bool isDesc = true, [FromUri]string userFilter = "")
		{
			var result = DiscordBotLogic.GetMesageList(count, serverId, startDate.HasValue ? startDate.Value : DateTime.MinValue, sort, isDesc, userFilter);

			if (result.success)
				return Request.CreateResponse(HttpStatusCode.OK, result);
			else
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.message);
		}
	}
}