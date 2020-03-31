using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HTML5_WebApiWithAngularJS.Startup))]
namespace HTML5_WebApiWithAngularJS
{
	public partial class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			ConfigureAuth(app);
		}
	}
}