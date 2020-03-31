using System.Collections.Generic;
using System.IO;
using System.Web.Services;
using System.Xml;
using GrapeCity.ActiveReports.Samples.HTML5_MVC.Models;

namespace GrapeCity.ActiveReports.Samples.HTML5_MVC
{
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	public class ActiveReportsService : Web.ReportService
	{
		protected override object OnCreateReportHandler(string reportPath)
		{
			var data = reportPath.Split(';');
			if (data.Length != 2)
				return base.OnCreateReportHandler(reportPath);
			switch (data[0])
			{
				case "Section":
					var sectionReport = new SectionReport();
					sectionReport.LoadLayout(XmlReader.Create(Server.MapPath("~/Reports/OrderReport.rpx")));
					sectionReport.DataSource = Repository.GetOrders(data[1]);
					var xvalues = new List<string>();
					var yvalues = new List<double>();
					foreach (Order order in Repository.GetOrders(data[1]))
					{
						xvalues.Add(order.ShippedDate);
						yvalues.Add((double)order.Freight);
					}
					((SectionReportModel.ChartControl)sectionReport.Sections["reportFooter1"].Controls[0]).Series[0].Points.DataBindXY(xvalues.ToArray(), yvalues.ToArray());
					return sectionReport;
				case "Page":
					var pageReport = new PageReport(new FileInfo(Server.MapPath("~/Reports/OrderDetailsReport.rdlx")));
					pageReport.Document.LocateDataSource += delegate (object sender, LocateDataSourceEventArgs args)
					{
						args.Data = Repository.GetDetails(data[1]);
					};
					return pageReport;
			}
			return base.OnCreateReportHandler(reportPath);
		}
	}
}