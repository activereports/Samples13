import { Component, ElementRef, Input, ViewEncapsulation } from '@angular/core';
declare var GrapeCity: any;

@Component({
  selector: 'report-viewer',
  templateUrl: './report-viewer.component.html',
  styleUrls: ['./report-viewer.component.css'],
  encapsulation: ViewEncapsulation.None,
})
export class ReportViewerComponent {
  private viewer: any;
  private dropdownRoot: HTMLElement;

  private moveDropdownRoot() {
    const innerDropdownRoot = this.el.nativeElement.querySelector('#dropdown-root');

    if (this.dropdownRoot != null) {
      return innerDropdownRoot.parentElement.removeChild(innerDropdownRoot);
    }

    this.dropdownRoot = innerDropdownRoot.parentNode.removeChild(innerDropdownRoot);
    document.body.appendChild(this.dropdownRoot);
  }

  @Input() set reportId(reportId: string) {
    const viewerRoot = this.el.nativeElement.querySelector('#viewerPlaceHolder');

    if (this.viewer != null) {
      this.viewer.destroy();
    }

    this.viewer = GrapeCity.ActiveReports.JSViewer.create({
      element: viewerRoot,
      reportService: {},
      reportID: reportId,
      settings: {
        zoomType: 'FitPage'
      }
    });

    this.moveDropdownRoot();
  }
  constructor(private el: ElementRef) {
  }
}
