using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace JSViewer_MVCCore.Controllers
{
	[Route("/")]
	public class HomeController : Controller
	{
		public object Index() => Resource("index.html");
		
		[HttpGet("{file}")]
		public object Resource(string file)
		{
			var stream = GetType().Assembly.GetManifestResourceStream("JSViewer_MVC(Core).ViewerApp." + file);
			if(stream == null)
				return new NotFoundResult();

			if (Path.GetExtension(file) == ".html")
				return new ContentResult(){Content = new StreamReader(stream).ReadToEnd(), ContentType = "text/html"};
			
			using (var streamReader = new StreamReader(stream))
				return new FileContentResult(System.Text.Encoding.UTF8.GetBytes(streamReader.ReadToEnd()), GetType(file)) {FileDownloadName = file};
		}

		private string GetType(string file)
		{
			if (file.EndsWith(".css"))
				return "text/css";

			if (file.EndsWith(".js"))
				return "text/javascript";

			return "text/html";
		}

		[HttpGet("reports")]
		public ActionResult Reports()
		{
			string[] validExtensions = { ".rdl", ".rdlx", ".rdlx-master", ".rpx" };

			return new ObjectResult(
				typeof(HomeController).Assembly.GetManifestResourceNames()
				.Where(x => x.StartsWith(Startup.EmbeddedReportsPrefix) && validExtensions.Any(ext => x.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)))
				.Select(x => x.Substring(Startup.EmbeddedReportsPrefix.Length + 1))
				.ToArray());
		}
	}
}
