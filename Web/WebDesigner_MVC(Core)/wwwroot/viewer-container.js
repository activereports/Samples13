/* eslint-disable prefer-arrow-callback, wrap-iife, no-var, prefer-template, object-shorthand */
/* eslint object-shorthand:0, no-var:0, prefer-template:0, global-strict:0, strict:0, space-before-function-paren:0 */

function empty(node) {
	while (node.firstChild) {
		node.removeChild(node.firstChild);
	}
}

function renderViewerContainer(elementId, options, onClose) {
	var root = document.getElementById(elementId);
	empty(root);

	var previewUrl = options.makeViewerUrl();

	var header = document.createElement('div');
	header.className = 'viewer-container-app-header';
	root.appendChild(header);

	var button = document.createElement('button');
	button.className = 'viewer-container-back-button';

	var icon = document.createElement('i');
	icon.className = 'mdi mdi-chevron-left';
	header.appendChild(button);
	button.appendChild(icon);
	button.id = 'viewer-container-button-back';
	button.onclick = onClose;

	var appTitle = document.createElement('div');
	appTitle.innerText = options.applicationTitle;
	header.appendChild(appTitle);

	var reportNamePanel = document.createElement('div');
	reportNamePanel.className = 'viewer-container-header-name-panel';
	var reportName = document.createElement('span');
	reportName.innerText = options.reportInfo.name + (options.reportInfo.isTemporary ? ' *' : '');
	header.appendChild(reportNamePanel);
	reportNamePanel.appendChild(reportName);

	var frameContainer = document.createElement('div');
	frameContainer.className = 'viewer-container-iframe-container';
	var frame = document.createElement('iframe');
	frame.src = previewUrl;
	root.appendChild(frameContainer);
	frameContainer.appendChild(frame);

	document.getElementById('viewer-container-button-back').onclick = function() {
		onClose();
		empty(root);
	};
}

var viewerContainer = {
	renderViewerContainer: renderViewerContainer,
};
