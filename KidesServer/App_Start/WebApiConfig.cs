using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Web.Http;

namespace KidesServer
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "KidesApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

			// Return in JSON format
			config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
			config.Formatters.JsonFormatter.SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
			{
				DateTimeZoneHandling = DateTimeZoneHandling.Utc,
				DateFormatHandling = DateFormatHandling.IsoDateFormat
			};
		}
    }
}
