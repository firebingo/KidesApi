using KidesServer.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace KidesServer.Controllers
{
	[RoutePrefix("api/v1")]
	public class MusicController : ApiController
	{
		[HttpGet, Route("song-url")]
		public async Task<HttpResponseMessage> getSongUrl([FromUri]string searchString)
		{
			var result = MusicLogic.searchForSong(searchString);

			if (result.success)
				return Request.CreateResponse(HttpStatusCode.OK, result);
			else
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.message);
		}

		[HttpGet, Route("song-stats")]
		public async Task<HttpResponseMessage> getSongStats()
		{
			var result = MusicLogic.getSongStats();

			if (result.success)
				return Request.CreateResponse(HttpStatusCode.OK, result);
			else
				return Request.CreateErrorResponse(HttpStatusCode.BadRequest, result.message);
		}
	}
}