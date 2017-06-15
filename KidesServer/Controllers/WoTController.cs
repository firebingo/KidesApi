using System.Linq;
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
	public class WoTController : ApiController
	{
		[HttpGet, Route("user-data")]
		public async Task<HttpResponseMessage> getUserData([FromUri]string username, [FromUri]string region = "na", [FromUri]string accessToken = null)
		{
			var success = true;
			var message = "";
			WotUserInfo data = null;
			WotBasicUser userInfo = null;
			try
			{
				userInfo = await WoTLogic.callInfoAPI(username, region);
				if (userInfo.status == "error")
				{
					var code = Int32.Parse(userInfo.error.code);
					return Request.CreateErrorResponse(HttpStatusCode.BadRequest, userInfo.error.message);
				}
			}
			catch (Exception e)
			{
				ErrorLog.writeLog(e.Message);
				return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e.Message);
			}
			if (userInfo != null && userInfo.data != null)
			{
				var accountId = "";
				//try to search for the exact username.
				accountId = userInfo.data.FirstOrDefault(acc => acc.nickname == username)?.account_id ?? "";
				//if the exact name isint found go for a simple contains and case removal.
				if(accountId == "")
					accountId = userInfo.data.FirstOrDefault(acc => acc.nickname.ToLower().Contains(username.ToLower()))?.account_id ?? "";
				if (accountId == "")
				{
					success = false;
					message = $"No user with name {username} found on {region} server.";
				}
				else
				{
					data = await WoTLogic.callDataAPI(accountId, accessToken, region);
					if(data?.data == null)
					{
						success = false;
						message = $"User {username} found, but data could not be found.";
					}
				}
			}
			else
			{
				success = false;
				message = $"No user with name {username} found on {region} server.";
			}
			if (success)
				return Request.CreateResponse(HttpStatusCode.OK, data);
			else
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, message);
		}
	}
}