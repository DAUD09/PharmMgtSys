using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace PharmMgtSys.App_Start
{
	public class WebApiConfig
	{

		public static void Register(HttpConfiguration config)
		{
			// Attribute routing (optional, but useful)
			config.MapHttpAttributeRoutes();

			// Default API route (if not already present)
			config.Routes.MapHttpRoute(
				name: "DefaultApi",
				routeTemplate: "api/{controller}/{id}",
				defaults: new { id = RouteParameter.Optional }
			);

			
		}
	}
}