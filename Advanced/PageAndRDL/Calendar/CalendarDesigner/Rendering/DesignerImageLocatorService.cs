using System.ComponentModel.Design;
using GrapeCity.ActiveReports.Calendar.Design.Designers;
using GrapeCity.ActiveReports.Diagnostics;

namespace GrapeCity.ActiveReports.Calendar.Rendering
{
	/// <summary>
	/// DesignerImageLocatorService
	/// </summary>
	public sealed class DesignerImageLocatorService : ImageLocatorService
	{
		public DesignerImageLocatorService(CalendarDesigner calendarDesigner)
		{
			IDesignerHost host = calendarDesigner.ReportItem.Site.GetService(typeof(IDesignerHost)) as IDesignerHost;
			if (host == null)
			{
				DiagContext.Designer.Fail("Can get IDesignerHost for calendar report item");
				return;
			}
			_parentPageReport = host.RootComponent as PageReport;

			InitializeServices();
		}
	}
}