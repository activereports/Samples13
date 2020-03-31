using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace JSViewer_MVC.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("")]
        [Route("index")]
        public ActionResult Index()
        {
            return Resource("index.html");
        }

        [HttpGet]
        [Route("{file}")]
	[Route("ViewerApp/{file}")]
        public ActionResult Resource(string file)
        {
            var stream = GetType().Assembly.GetManifestResourceStream("JSViewer_MVC.ViewerApp." + file);
            if (stream == null)
                return new HttpNotFoundResult();

            if (Path.GetExtension(file) == ".html")
                return new ContentResult() {Content = new StreamReader(stream).ReadToEnd(), ContentType = "text/html"};

            using (var streamReader = new StreamReader(stream))
                return new FileContentResult(System.Text.Encoding.UTF8.GetBytes(streamReader.ReadToEnd()),
                    GetType(file)) {FileDownloadName = file};
        }

        private string GetType(string file)
        {
            if (file.EndsWith(".css"))
                return "text/css";

            if (file.EndsWith(".js"))
                return "text/javascript";

            return "text/html";
        }

        [HttpGet]
        [Route("reports")]
        public ActionResult Reports()
        {
			string[] validExtensions = { ".rdl", ".rdlx", ".rdlx-master", ".rpx" };

            var reportsList = typeof(HomeController).Assembly.GetManifestResourceNames()
                .Where(x => x.StartsWith(Startup.EmbeddedReportsPrefix) && validExtensions.Any(ext => x.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase)))
                .Select(x => x.Substring(Startup.EmbeddedReportsPrefix.Length + 1))
                .ToArray();

            return new JsonResult
            {
                Data = reportsList,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet
            };
        }
    }
}