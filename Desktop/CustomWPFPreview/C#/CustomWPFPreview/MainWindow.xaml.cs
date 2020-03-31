using System;
using System.Collections.Specialized;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Xps.Packaging;
using GrapeCity.ActiveReports.Extensibility.Printing;
using GrapeCity.ActiveReports.Extensibility.Rendering;
using GrapeCity.ActiveReports.Extensibility.Rendering.Components;
using GrapeCity.ActiveReports.Extensibility.Rendering.IO;
using GrapeCity.Viewer.Common;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using PaperOrientation = GrapeCity.ActiveReports.PageReportModel.PaperOrientation;
using PaperSize = System.Drawing.Printing.PaperSize;
using Path = System.IO.Path;
using PrinterSettings = System.Drawing.Printing.PrinterSettings;

namespace GrapeCity.ActiveReports.Samples.CustomWpfPreview
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string _xpsFile;
		private XpsDocument _xpsDocument;

		public MainWindow()
		{
			InitializeComponent();
			Title = Properties.Resources.AplicationTitle;
			if (!PrinterSettings.InstalledPrinters.Cast<string>().Contains(Properties.Resources.XpsPrinterName))
			{
				MessageBox.Show(this, Properties.Resources.NoXpsPrinterError, Title, MessageBoxButton.OK, MessageBoxImage.Error);
				Close();
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			CloseDocument();
			base.OnClosed(e);
		}

		private void OnExit(object sender, EventArgs e)
		{
			Close();
		}

		private void OnOpen(object target, ExecutedRoutedEventArgs args)
		{
			var dialog = new OpenFileDialog
			{
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				CheckFileExists = true,
				Filter = Properties.Resources.ReportFilesFilter
			};

			if (dialog.ShowDialog() != true) return;

			OpenDocument(dialog.FileName);
		}

		private void OpenDocument(string reportFile)
		{
			if (_xpsDocument != null) CloseDocument();
			var isAborted = false;
			var isEndPrint = false;
			_xpsFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xps");
			var report = new PageReport(new FileInfo(reportFile));
			var keeper = new RenderingTreeKeeper();
			var task = new Task(() => report.Document.Render(keeper, null));
			Mouse.OverrideCursor = Cursors.Wait;
			task.RunSynchronously();
			task.Wait();
			Mouse.OverrideCursor = Cursors.Arrow;
			if (task.Exception != null)
			{
				MessageBox.Show(this, task.Exception.Message, Title, MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// Note: you can use https://www.codeproject.com/Tips/678644/Print-to-XPS-from-Code instead
			var printer = report.Document.Printer;
			printer.PrinterSettings.PrintFileName = _xpsFile;
			printer.PrinterSettings.PrintToFile = true;
			printer.PrinterSettings.PrinterName = Properties.Resources.XpsPrinterName;
			printer.DefaultPageSettings.Landscape = report.Report.PaperOrientation == PaperOrientation.Landscape;
			if (!report.Report.PageWidth.IsUndefined && !report.Report.PageHeight.IsUndefined)
				printer.DefaultPageSettings.PaperSize = new PaperSize("Custom", (int)(report.Report.PageWidth.ToInches() * 100), (int)(report.Report.PageHeight.ToInches() * 100));
			else
				printer.DefaultPageSettings.PaperSize = new PaperSize("Custom", (int)(keeper.Report.PageWidth.ToInches() * 100), (int)(keeper.Report.PageHeight.ToInches() * 100)); ;

			PrintEventHandler endPrintHandler = (_, __) =>
			{
				isEndPrint = true;
				report.Dispose();
				if (!isAborted)
				{
					ShowReport();
				}
			};

			PrintAbortedEventHandler printAbortedHadler  = (_, __) =>
			{
				printer.EndPrint -= endPrintHandler;
				isAborted = true;
				if (!isEndPrint)
				{
					SetDefaultState();
				}
			};

			printer.EndPrint += endPrintHandler;
			report.Document.PrintAborted += printAbortedHadler;
			Mouse.OverrideCursor = Cursors.Wait;
			report.Document.Print( (PrintingSettings.UsePrintingThread | PrintingSettings.ShowPrintProgressDialog));
		}

		private void ShowReport()
		{
			if (!File.Exists(_xpsFile) || new FileInfo(_xpsFile).Length == 0)
			{
				ShowReportAfterSleep();
				return;
			}

			try
			{
				_xpsDocument = new XpsDocument(_xpsFile, FileAccess.Read);
			}
			catch
			{
				// It is possible that printer  handles contol on xps file, so it is need some time to release control
				ShowReportAfterSleep();
				return;
			}

			DocViewer.Document = _xpsDocument.GetFixedDocumentSequence() ?? new FixedDocumentSequence();

			SetDefaultState();
		}

		private void ShowReportAfterSleep()
		{
			Thread.Sleep(100);
			Dispatcher.BeginInvoke(new MethodInvoker(() => ShowReport()));
		}

		private void SetDefaultState()
		{
			try
			{
				SetMenuState(true);
				Mouse.OverrideCursor = Cursors.Arrow;
			}
			catch
			{
				SetStateAfterSleep();
				return;
			}
		}

		private void SetStateAfterSleep()
		{
			Thread.Sleep(100);
			Dispatcher.BeginInvoke(new MethodInvoker(() => SetDefaultState()));
		}

		private void SetMenuState(bool enabled)
		{
			menuFileClose.IsEnabled = enabled;
			menuFilePrint.IsEnabled = enabled;

			menuViewIncreaseZoom.IsEnabled = enabled;
			menuViewDecreaseZoom.IsEnabled = enabled;
		}

		private void OnClose(object target, ExecutedRoutedEventArgs args)
		{
			CloseDocument();
		}

		private void CloseDocument()
		{
			if (_xpsDocument != null)
			{
				_xpsDocument.Close();
				_xpsDocument = null;
				if (File.Exists(_xpsFile)) File.Delete(_xpsFile);
				DocViewer.Document = null;
			}

			SetMenuState(false);
		}

		private void OnPrint(object target, ExecutedRoutedEventArgs args)
		{
			PrintDocument();
		}

		public void PrintDocument()
		{
			if (DocViewer != null)
				DocViewer.Print();
		}

		public DocumentViewer DocViewer
		{
			get
			{
				return docViewer;
			}
		}

		private sealed class RenderingTreeKeeper : IRenderingExtension
		{
			public IReport Report { get; private set; }

			public void Render(IReport report, StreamProvider streams)
			{
				Report = report;
			}

			public void Render(IReport report, StreamProvider streams, NameValueCollection settings)
			{
				Report = report;
			}
		}

		private void docViewer_Loaded(object sender, RoutedEventArgs e)
		{
			var reportFile = @"..\..\..\..\Report\AnnualReport.rdlx";
			OpenDocument(reportFile);
		}
	}
}