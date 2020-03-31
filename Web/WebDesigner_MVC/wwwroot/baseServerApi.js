/* eslint-disable prefer-arrow-callback, wrap-iife, no-var, prefer-template, object-shorthand */
/* eslint object-shorthand:0, no-var:0, prefer-template:0, global-strict:0, strict:0, space-before-function-paren:0 */

var baseServerApi = function ($) {
	'use strict';

	function makeURL(path, queryParams) {
		var queryString = queryParams
			? '?' + $.param(queryParams)
			: '';

		/* No Virtual Directory */
		return '/api/' + path + queryString;

		/* For Virtual Directory */
		// return '/VirtualDirectoryName/api/' + path + queryString;
	}

	function getDefaultHeaders(enableCache) {
		var headers = {
			Accept: 'application/json',
		};
		if (!enableCache) {
			headers['Cache-Control'] = 'no-cache, no-store, must-revalidate';
			headers.Pragma = 'no-cache';
			headers.Expires = 0;
		}
		return headers;
	}

	function getJson(url) {
		return $.ajax({
			url: url,
			type: 'GET',
			dataType: 'json',
			headers: getDefaultHeaders(),
			xhrFields: {
				withCredentials: true,
			},
		});
	}

	function mapReport(report) {
		if (!report) return {};

		return {
			_id: report.Id,
			Name: report.Name,
			Type: 'PageReport',
			IsCpl: report.Type === 'CPL',
			$effectivePermissions: 'All',
		};
	}

	/**
	 * Get list with reports
	 * @param {object} options
	 */
	function getReportsList() {
		var reportsListUrl = makeURL('reports/list');
		return getJson(reportsListUrl)
			.then(function (result) {
				return result.map(mapReport);
			});
	}

	/**
	 * Get list with report revisions
	 * @param {Object} options
	 * @param {string} options.id report id
	 */
	function getReportRevisions(options) {
		return new $.Deferred().resolve([mapReport({ Id: options.id, Name: decodeURIComponent(options.id) })]);
	}

	/**
	 * Get report content
	 * @param {Object} options
	 * @param {string} options.id report id
	 */
	function getReportContent(options) {
		var reportContentUrl = makeURL('reports/' + options.id + '/content');
		return getJson(reportContentUrl);
	}

	/**
	 * Save report as a new report
	 * @param {Object} options
	 * @param {string} options.name report name
	 * @param {Object} options.content report content
	 */
	function saveNewReport(options) {
		var queryParams = {
			name: options.name.endsWith('.rdlx') ? options.name : options.name + '.rdlx',
		};
		return $.ajax({
			type: 'POST',
			url: makeURL('reports/content', queryParams),
			data: JSON.stringify(options.content),
			dataType: 'json',
			contentType: 'application/json',
			processData: false,
			headers: getDefaultHeaders(),
			xhrFields: {
				withCredentials: true,
			},
		}).then(function(result) {
			return {
				Id: result.Id || result.id,
			};
		});
	}

	/**
	 * Save report as a new revision of an existing report
	 * @param {Object} options
	 * @param {string} options.id report id
	 * @param {Object} options.content report content
	 */
	function saveExistingReport(options) {
		var reportId = options.id.endsWith('.rdlx') ? options.id : options.id + '.rdlx';
		return $.ajax({
			type: 'PUT',
			url: makeURL('reports/' + reportId + '/content'),
			data: JSON.stringify(options.content),
			dataType: 'json',
			contentType: 'application/json',
			processData: false,
			headers: getDefaultHeaders(),
			xhrFields: {
				withCredentials: true,
			},
		}).then(function(result) {
			return {
				Id: result.Id || result.id,
			};
		});
	}

	/**
	 * Save a temporary report for previewing
	 * @param {Object} options options
	 * @param {string} options.name report name
	 * @param {Object} options.content report content
	 */
	function saveTemporaryReport(options) {
		return $.ajax({
			type: 'POST',
			url: makeURL('reports/content', { name: options.name, isTemporary: true }),
			data: JSON.stringify(options.content),
			dataType: 'json',
			contentType: 'application/json',
			headers: getDefaultHeaders(),
			xhrFields: {
				withCredentials: true,
			},
		}).then(function(result) {
			return {
				Id: result.Id || result.id,
			};
		});
	}

	/**
	 * Update route
	 * @param {Object} options
	 * @param {string} options.id report id
	 * @param {string} options.version report version
	 */
	function updateRoute(options) {
		var queryParamsPart = (window.location.href || '').split('?')[1] || '';
		var queryParamsString = queryParamsPart ? ('?' + queryParamsPart) : '';
		history.replaceState({}, '', '/');
		if (options.id) {
			history.replaceState({}, '', 'edit/' + options.id + queryParamsString);
			return;
		}
		history.replaceState({}, '', 'create' + queryParamsString);
	}

	/**
	 * Delete a temporary report after previewing
	 * @param {Object} options
	 * @param {string} options.id report id
	 */
	function deleteTemporaryReport(options) {
		return $.ajax({
			type: 'DELETE',
			url: makeURL('reports/' + options.id + '/content'),
			dataType: 'json',
			headers: getDefaultHeaders(),
		}).then(function(result) {
			return {
				Id: result.Id,
			};
		});
	}

	/**
	 * Get data set content
	 * @param {Object} options
	 * @param {string} options.id data set id
	 */
	function getDataSetContent(options) {
		var dataSetContentUrl = makeURL('datasets/' + options.id + '/content');
		return getJson(dataSetContentUrl);
	}

	/**
	 * Get list with data sets
	 */
	function getDataSetsList() {
		var dataSetsListUrl = makeURL('datasets/list');
		return getJson(dataSetsListUrl)
			.then(function (dataSetsList) {
				return dataSetsList.map(function(dataSet) {
					return {
						id: dataSet.Id,
						name: dataSet.Name,
						version: null,
					};
				});
			});
	}

	/**
	 * Get data sources and data sets
	 * @param {Object} options
	 * @param {Object} options.dataSetInfo data set info (element from data sets list)
	 * @param {string} options.dataSetInfo.id data set id
	 * @param {string} options.dataSetInfo.name data set name
	 * @param {string} options.dataSetInfo.version data set version
	 * @param {Object} options.dataSourceInfo data source info
	 * @param {Object[]} options.reportDataSources data sources used in the report
	 * @param {Object[]} options.reportDataSets data sets used in the report
	 * @returns {{dataSets: Object[], dataSources: Object[]}}
	 */
	function getDataSourcesAndDataSets(options) {
		var deferred = new $.Deferred();
		if (options.dataSetInfo.id) {
			getDataSetContent({ id: options.dataSetInfo.id }).then(function (dataSetContent) {
				deferred.resolve({ dataSets: [dataSetContent.DataSet], dataSources: [dataSetContent.DataSource] });
			}).fail(function () {
				deferred.reject();
			});
		}
		else deferred.reject();

		return deferred;
	}

	/**
	 * Create resource link
	 * @param {Object} options
	 * @param {string} options.id resource id
	 * @param {string} options.version resource version
	 * @param {string} options.type resource type
	 */
	function createResourceLink (options) {
		var resourceLink = '';
		if (options.id) {
			resourceLink = options.id.replace(/%2F/g, '/');
		}
		return resourceLink;
	}

	/**
	 * Parse resource link
	 * @param {string} resourceLink resource link
	 */
	function parseResourceLink (resourceLink) {
		var resourceId = resourceLink && resourceLink.replace(/\//g, '%2F');
		return {
			id: resourceId,
			version: null,
			type: null,
		};
	}

	/**
	 * Get list with themes
	 */
	function getThemesList() {
		var themesListUrl = makeURL('themes/list');
		var defaultThemeSuffix = '.default';
		var haveDefaultTheme = false;
		return getJson(themesListUrl).then(function (themesList) {
			if (!themesList || !themesList.length) return [];

			return themesList.map(function (theme) {
				var themeName = theme.Title;
				var isDefault = false;
				if (themeName.indexOf(defaultThemeSuffix, themeName.length - defaultThemeSuffix.length) >= 0) {
					themeName = themeName.substring(0, themeName.length - defaultThemeSuffix.length);
					if (!haveDefaultTheme) {
						isDefault = true;
						haveDefaultTheme = true;
					}
				}
				return {
					IsDefault: isDefault,
					Dark1: theme.Dark1,
					Light1: theme.Light1,
					Dark2: theme.Dark2,
					Light2: theme.Light2,
					Accent1: theme.Accent1,
					Accent2: theme.Accent2,
					Accent3: theme.Accent3,
					Accent4: theme.Accent4,
					Accent5: theme.Accent5,
					Accent6: theme.Accent6,
					MajorFontFamily: theme.MajorFontFamily,
					MinorFontFamily: theme.MinorFontFamily,
					_id: theme.Id,
					Name: themeName,
				};
			});
		});
	}

	/**
	 * Get theme content
	 * @param {Object} options
	 * @param {string} options.id theme id
	 */
	function getThemeContent(options) {
		var themeContentUrl = makeURL('themes/' + options.id + '/content');
		return getJson(themeContentUrl);
	}

	/**
	 * Get list with the templates
	*/
	function getTemplatesList() {
		return getJson(makeURL('templates/list')).then(function(templates) {
			return templates.map(function(template) {
				return {
					_id: template.Id,
					Name: template.Name,
				};
			});
		});
	}

	/**
	 * Get template content
	 * @param {Object} options
	 * @param {string} options.id template id
	 */
	function getTemplateContent(options) {
		return getJson(makeURL('templates/' + options.id + '/content'));
	}

	/**
	 * Get template thumbnail
	 * @param {Object} options
	 * @param {string} options.id template id
	 */
	function getTemplateThumbnail(options) {
		return $.ajax({
			url: makeURL('templates/' + options.id + '/thumbnail'),
			type: 'GET',
			dataType: 'json',
			headers: getDefaultHeaders(true),
			xhrFields: {
				withCredentials: true,
			},
		}).then(function(thumbnail) {
			if (thumbnail) {
				return 'data:' + thumbnail.MIMEType + ';base64,' + thumbnail.Data;
			}
			return '';
		});
	}

	/**
	 * Gets images list
	 */
	function getImagesList() {
		var url = makeURL('images/list');
		return getJson(url)
			.then(function (result) {
				return result.map(function(image) {
					return {
						_id: image.Id,
						Name: image.Name,
						MimeType: image.ContentType,
						Thumbnail: image.Thumbnail,
					};
				});
			});
	}

	/**
	 * Makes image URL
	 * @param {number} options.id id of the image
	 */
	function getImageUrl(options) {
		return makeURL('images/' + options.id + '/content');
	}

	return {
		createResourceLink: createResourceLink,
		parseResourceLink: parseResourceLink,
		updateRoute: updateRoute,

		getReportsList: getReportsList,
		getReportContent: getReportContent,
		getReportRevisions: getReportRevisions,

		saveNewReport: saveNewReport,
		saveExistingReport: saveExistingReport,
		saveTemporaryReport: saveTemporaryReport,
		deleteTemporaryReport: deleteTemporaryReport,

		getDataSetContent: getDataSetContent,
		getDataSetsList: getDataSetsList,
		getDataSourcesAndDataSets: getDataSourcesAndDataSets,

		getTemplatesList: getTemplatesList,
		getTemplateContent: getTemplateContent,
		getTemplateThumbnail: getTemplateThumbnail,

		getThemesList: getThemesList,
		getThemeContent: getThemeContent,

		getImagesList: getImagesList,
		getImageUrl: getImageUrl,
	};
}(jQuery);
