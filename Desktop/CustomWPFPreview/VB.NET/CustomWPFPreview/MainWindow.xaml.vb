Imports Path = System.IO.Path
Imports PaperSize = System.Drawing.Printing.PaperSize
Imports PaperOrientation = GrapeCity.ActiveReports.PageReportModel.PaperOrientation
Imports OpenFileDialog = Microsoft.Win32.OpenFileDialog
Imports MessageBox = System.Windows.MessageBox
Imports Cursors = System.Windows.Input.Cursors
Imports GrapeCity.Viewer.Common
Imports GrapeCity.ActiveReports.Extensibility.Rendering.IO
Imports GrapeCity.ActiveReports.Extensibility.Rendering.Components
Imports GrapeCity.ActiveReports.Extensibility.Rendering
Imports System.Windows.Xps.Packaging
Imports System.Windows.Forms
Imports System.Threading
Imports System.IO
Imports System.Collections.Specialized
Imports PrinterSettings = System.Drawing.Printing.PrinterSettings
Imports System.Drawing.Printing
Imports GrapeCity.ActiveReports.Extensibility.Printing

Public Class MainWindow
	Private _xpsFile As String
	Private _xpsDocument As XpsDocument

	Public Sub New()
		InitializeComponent()

		If Not PrinterSettings.InstalledPrinters.Cast(Of String)().Contains(My.Resources.XpsPrinterName) Then
			MessageBox.Show(Me, My.Resources.NoXpsPrinterError, Title, MessageBoxButton.OK, MessageBoxImage.[Error])
			Close()
		End If

	End Sub

	Protected Overrides Sub OnClosed(e As EventArgs)
		CloseDocument()
		MyBase.OnClosed(e)
	End Sub

	Private Sub OnExit(sender As Object, e As EventArgs)
		Close()
	End Sub

	Private Sub OnOpen(target As Object, args As ExecutedRoutedEventArgs)
		Dim dialog = New OpenFileDialog() With {
			.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			.CheckFileExists = True,
			.Filter = My.Resources.ReportFilesFilter
		}

		If dialog.ShowDialog() <> True Then
			Return
		End If

		OpenDocument(dialog.FileName)
	End Sub

	Private Sub OpenDocument(reportFile As String)
		If _xpsDocument IsNot Nothing Then
			CloseDocument()
		End If

		Dim isAborted As Boolean = False
		Dim isEndPrint As Boolean = False

		_xpsFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xps")
		Dim report = New PageReport(New FileInfo(reportFile))
		Dim keeper = New RenderingTreeKeeper()
		Dim task = New Task(Sub() report.Document.Render(keeper, Nothing))
		Mouse.OverrideCursor = Cursors.Wait
		task.RunSynchronously()
		task.Wait()
		Mouse.OverrideCursor = Cursors.Arrow
		If task.Exception IsNot Nothing Then
			MessageBox.Show(Me, task.Exception.Message, Title, MessageBoxButton.OK, MessageBoxImage.[Error])
			Return
		End If

		' Note: you can use https://www.codeproject.com/Tips/678644/Print-to-XPS-from-Code instead
		Dim printer = report.Document.Printer
		printer.PrinterSettings.PrintFileName = _xpsFile
		printer.PrinterSettings.PrintToFile = True
		printer.PrinterSettings.PrinterName = My.Resources.XpsPrinterName
		printer.DefaultPageSettings.Landscape = report.Report.PaperOrientation = PaperOrientation.Landscape
		If Not report.Report.PageWidth.IsUndefined AndAlso Not report.Report.PageHeight.IsUndefined Then
			printer.DefaultPageSettings.PaperSize = New PaperSize("Custom", CType(report.Report.PageWidth.ToInches() * 100, Integer), CType(report.Report.PageHeight.ToInches() * 100, Integer))
		Else
			printer.DefaultPageSettings.PaperSize = New PaperSize("Custom", CType(keeper.Report.PageWidth.ToInches() * 100, Integer), CType(keeper.Report.PageHeight.ToInches() * 100, Integer))
		End If

		Dim endPrintHandler As PrintEventHandler = Sub(_s, __)
													   isEndPrint = True
													   report.Dispose()
													   If Not isAborted Then
														   ShowReport()
													   End If

												   End Sub

		Dim printAbortedHadler As PrintAbortedEventHandler = Sub(_s, __)
																 RemoveHandler printer.EndPrint, endPrintHandler
																 isAborted = True
																 If Not isEndPrint Then
																	 SetDefaultState()
																 End If

															 End Sub

		AddHandler printer.EndPrint, endPrintHandler
		AddHandler report.Document.PrintAborted, printAbortedHadler
		Mouse.OverrideCursor = Cursors.Wait
		report.Document.Print(PrintingSettings.UsePrintingThread Or PrintingSettings.ShowPrintProgressDialog)
	End Sub

	Private Sub ShowReport()
		If Not File.Exists(_xpsFile) OrElse New FileInfo(_xpsFile).Length = 0 Then
			ShowReportAfterSleep()
			Return
		End If

		Try
			_xpsDocument = New XpsDocument(_xpsFile, FileAccess.Read)
		Catch
			' It is possible that printer  handles contol on xps file, so it is need some time to release control
			ShowReportAfterSleep()
			Return
		End Try
		docViewer.Document = If(_xpsDocument.GetFixedDocumentSequence(), New FixedDocumentSequence())

		SetMenuState(True)
		Mouse.OverrideCursor = Cursors.Arrow
	End Sub

	Private Sub ShowReportAfterSleep()
		Thread.Sleep(100)
		Dispatcher.BeginInvoke(New MethodInvoker(AddressOf ShowReport))
	End Sub

	Private Sub SetMenuState(enabled As Boolean)
		menuFileClose.IsEnabled = enabled
		menuFilePrint.IsEnabled = enabled

		menuViewIncreaseZoom.IsEnabled = enabled
		menuViewDecreaseZoom.IsEnabled = enabled
	End Sub

	Private Sub SetDefaultState()
		Try
			SetMenuState(True)
			Mouse.OverrideCursor = Cursors.Arrow
		Catch
			SetStateAfterSleep()
			Return
		End Try
	End Sub

	Private Sub SetStateAfterSleep()
		Thread.Sleep(100)
		Dispatcher.BeginInvoke(New MethodInvoker(AddressOf SetDefaultState))
	End Sub

	Private Sub OnClose(target As Object, args As ExecutedRoutedEventArgs)
		CloseDocument()
	End Sub

	Private Sub CloseDocument()
		If _xpsDocument IsNot Nothing Then
			_xpsDocument.Close()
			_xpsDocument = Nothing
			If File.Exists(_xpsFile) Then
				File.Delete(_xpsFile)
			End If
			docViewer.Document = Nothing
		End If

		SetMenuState(False)
	End Sub

	Private Sub OnPrint(target As Object, args As ExecutedRoutedEventArgs)
		PrintDocument()
	End Sub

	Public Sub PrintDocument()
		If (Not IsNothing(docViewer)) Then
			docViewer.Print()
		End If
	End Sub


	Private NotInheritable Class RenderingTreeKeeper
		Implements IRenderingExtension
		Public Property Report() As IReport
			Get
				Return m_Report
			End Get
			Private Set
				m_Report = Value
			End Set
		End Property
		Private m_Report As IReport

		Public Sub Render(report__1 As IReport, streams As StreamProvider) Implements IRenderingExtension.Render
			Report = report__1
		End Sub

		Public Sub Render(report__1 As IReport, streams As StreamProvider, settings As NameValueCollection) Implements IRenderingExtension.Render
			Report = report__1
		End Sub
	End Class

	Private Sub docViewer_Loaded(sender As Object, e As RoutedEventArgs)
		Dim reportFile = "..\..\..\..\Report\AnnualReport.rdlx"
		OpenDocument(reportFile)
	End Sub
End Class