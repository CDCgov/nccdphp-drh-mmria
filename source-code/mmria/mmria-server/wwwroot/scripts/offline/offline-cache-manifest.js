(function(root) {
    'use strict';

    const requiredStaticExpectations = [
        { path: '/css/index.css', validation: 'asset_non_empty', expectedStatus: 200, expectedContentType: 'text/css', blockOnFailure: true },
        { path: '/css/bootstrap.min.css', validation: 'asset_non_empty', expectedStatus: 200, expectedContentType: 'text/css', blockOnFailure: true },
        { path: '/scripts/mmria.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/create_default_object.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/editor/page_renderer.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/editor/page_renderer/app.mmria.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/case/index.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/offline/offline-logger.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/offline/offline-cache-manifest.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/offline/offline-integrity-validator.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/offline/offline-modals.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/offline/offline-sync-manager.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/scripts/offline/offline-transition-manager.js', validation: 'javascript_non_empty', expectedStatus: 200, expectedContentType: 'javascript', blockOnFailure: true },
        { path: '/img/offline-index.svg', validation: 'asset_non_empty', expectedStatus: 200, expectedContentType: 'image/svg+xml', blockOnFailure: false }
    ];

    const additionalRequiredStaticFiles = [
            '/css/animate.css',
            '/TemplatePackage/4.0/assets/css/app.min.css',
            '/TemplatePackage/4.0/assets/css/print.css',
            '/TemplatePackage/4.0/assets/vendor/css/bootstrap.css',
            '/styles/mmria-custom.css',
            '/styles/template-package-override.css',
            '/styles/mmria.css',
            '/styles/d3/c3.min.css',
            '/styles/jquery/jquery.timepicker.css',
            '/styles/jquery/jquery.datetimepicker.css',
            '/styles/bootstrap/bootstrap-datetimepicker.min.css',
            '/styles/bootstrap/jquery.bootstrap-touchspin.min.css',
            '/styles/bootstrap/bootstrap-timepicker.css',
            '/styles/flatpickr/flatpickr.min.css',
            '/styles/d3/c3/0.7.20/c3.min.css',
            '/styles/trumbowyg/trumbowyg.min.css',
            '/TemplatePackage/4.0/assets/fonts/open-sans-v15-latin-regular.woff2',
            '/TemplatePackage/4.0/assets/fonts/merriweather-v19-latin-regular.woff2',
            '/TemplatePackage/4.0/assets/fonts/cdciconfont.woff2',
            '/TemplatePackage/4.0/assets/fonts/cdciconfont.woff',
            '/TemplatePackage/4.0/assets/fonts/cdciconfont.ttf',
            '/TemplatePackage/4.0/assets/fonts/cdciconfont.eot',
            '/TemplatePackage/4.0/assets/fonts/fontawesome-webfont.woff',
            '/TemplatePackage/4.0/assets/fonts/fontawesome-webfont.ttf',
            '/TemplatePackage/4.0/assets/fonts/fontawesome-webfont.eot',
            '/TemplatePackage/4.0/assets/fonts/glyphicons-halflings-regular.woff',
            '/TemplatePackage/4.0/assets/fonts/glyphicons-halflings-regular.ttf',
            '/TemplatePackage/4.0/assets/fonts/glyphicons-halflings-regular.eot',
            '/TemplatePackage/4.0/assets/fonts/lato-regular-webfont.woff',
            '/TemplatePackage/4.0/assets/fonts/lato-regular-webfont.ttf',
            '/TemplatePackage/4.0/assets/fonts/lato-regular-webfont.eot',
            '/TemplatePackage/4.0/assets/fonts/cdciconfont.woff2?2747808d2c4ae8c1059745ae5eddb65e',
            '/TemplatePackage/4.0/assets/fonts/cdciconfont.woff?2747808d2c4ae8c1059745ae5eddb65e',
            '/TemplatePackage/4.0/assets/fonts/cdciconfont.ttf?2747808d2c4ae8c1059745ae5eddb65e',
            '/js/jquery.min.js',
            '/js/bootstrap.min.js',
            '/js/jquery.easing.min.js',
            '/js/wow.js',
            '/js/jquery.bxslider.min.js',
            '/TemplatePackage/4.0/assets/vendor/js/jquery.min.js',
            '/TemplatePackage/4.0/assets/vendor/js/bootstrap.min.js',
            '/scripts/jquery-3.1.1.min.js',
            '/scripts/jquery-ui.min.js',
            '/scripts/jquery/moment.js',
            '/scripts/jquery/jquery.timepicker.js',
            '/scripts/jquery/jquery.numeric.min.js',
            '/scripts/jquery/jquery.datetimepicker.js',
            '/scripts/bootstrap/bootstrap-datetimepicker.min.js',
            '/scripts/bootstrap/jquery.bootstrap-touchspin.min.js',
            '/scripts/bootstrap/bootstrap-timepicker.js',
            '/scripts/esprima.js',
            '/scripts/escodegen.browser.js',
            '/scripts/peg.js/0.10.0/peg.js',
            '/scripts/rxjs/7.5.5/rxjs.umd.min.js',
            '/scripts/d3/d3.min.js',
            '/scripts/d3/c3.min.js',
            '/scripts/d3/d3/v5/d3.v5.min.js',
            '/scripts/d3/c3/0.7.20/c3.min.js',
            '/scripts/trumbowyg/trumbowyg.min.js',
            '/scripts/trumbowyg/trumbowyg.colors.min.js',
            '/scripts/trumbowyg/trumbowyg.fontsize.min.js',
            '/scripts/mmria-custom.js',
            '/scripts/metadata_summary.js',
            '/scripts/editor/page_renderer/string.js',
            '/scripts/editor/page_renderer/number.js',
            '/scripts/editor/page_renderer/textarea.js',
            '/scripts/editor/page_renderer/html_area.js',
            '/scripts/editor/page_renderer/time.js',
            '/scripts/editor/page_renderer/boolean.js',
            '/scripts/editor/page_renderer/chart.js',
            '/scripts/editor/page_renderer/date.mmria.js',
            '/scripts/editor/page_renderer/datetime.js',
            '/scripts/editor/page_renderer/form.mmria.js',
            '/scripts/editor/page_renderer/form.pmss.attachment.js',
            '/scripts/editor/page_renderer/grid.js',
            '/scripts/editor/page_renderer/group.js',
            '/scripts/editor/page_renderer/hidden.js',
            '/scripts/editor/page_renderer/jurisdiction.js',
            '/scripts/editor/page_renderer/label.js',
            '/scripts/editor/page_renderer/list.js',
            '/scripts/editor/navigation_renderer.js',
            '/scripts/editor/apply_sort.js',
            '/scripts/case/tab-id.js',
            '/scripts/case/index.mmria.js',
            '/scripts/case/search_view.js',
            '/scripts/case/conversion-calculator.js',
            '/scripts/pdf-version/pdfmake.min.js',
            '/scripts/pdf-version/vfs_fonts.js',
            '/scripts/pdf-version/chart.min.js',
            '/scripts/pdf-version/index.js',
            '/scripts/data_access.js',
            '/scripts/url_monitor.js',
            '/scripts/flatpickr/flatpickr.js',
            '/scripts/offline/offline-utils.js',
            '/scripts/offline/offline-session-validator.js',
            '/scripts/offline/offline-network-monitor.js',
            '/scripts/offline/offline-change-tracker.js',
            '/scripts/offline/offline-case-manager.js',
            '/scripts/offline/offline-session-manager.js',
            '/scripts/offline/offline-navigation-manager.js',
            '/scripts/offline/offline-status-manager.js',
            '/scripts/offline/offline-ui-renderer.js',
            '/scripts/offline/offline-logout-button.js',
            '/scripts/offline/offline-home-page.js',
            '/scripts/offline/service-worker-manager.js',
            '/scripts/offline/offline-debug-modal.js',
            '/scripts/Home/index.js',
            '/favicon.ico',
            '/TemplatePackage/4.0/assets/imgs/favicon.ico',
            '/img/icon_pin.png',
            '/img/icon_unpin.png',
            '/img/online-go.svg',
            '/img/offline-info.svg',
            '/img/icon_error.svg',
            '/images/mmria-secondary.svg',
            '/images/mmria-secondary.png',
            '/scripts/Account/offline_key_login.js',
            '/scripts/shared/logout-handler.js'
        ];

    function getStaticValidation(path) {
        if (path.endsWith('.js')) {
            return {
                validation: 'javascript_non_empty',
                expectedContentType: 'javascript'
            };
        }

        if (path.endsWith('.css')) {
            return {
                validation: 'asset_non_empty',
                expectedContentType: 'text/css'
            };
        }

        if (path.endsWith('.svg')) {
            return {
                validation: 'asset_non_empty',
                expectedContentType: 'image/svg+xml'
            };
        }

        return {
            validation: 'asset_non_empty',
            expectedContentType: null
        };
    }

    const allRequiredStaticExpectations = requiredStaticExpectations.concat(
        additionalRequiredStaticFiles.map(path => {
            const validation = getStaticValidation(path);
            return {
                path: path,
                validation: validation.validation,
                expectedStatus: 200,
                expectedContentType: validation.expectedContentType,
                blockOnFailure: true
            };
        })
    );

    const requiredRouteExpectations = [
        {
            id: 'root',
            pattern: /^\/$/,
            validation: 'html_shell',
            expectedStatus: 200,
            expectedContentType: 'text/html',
            blockOnFailure: true
        },
        {
            id: 'home_index',
            pattern: /^\/Home\/Index\/?$/,
            validation: 'html_shell',
            expectedStatus: 200,
            expectedContentType: 'text/html',
            blockOnFailure: true
        },
        {
            id: 'case_index',
            pattern: /^\/Case\/?$/,
            validation: 'html_shell',
            expectedStatus: 200,
            expectedContentType: 'text/html',
            blockOnFailure: true
        },
        {
            id: 'offline_login',
            pattern: /^\/Account\/OfflineLogin\/?$/i,
            validation: 'html_shell',
            expectedStatus: 200,
            expectedContentType: 'text/html',
            blockOnFailure: true
        },
        {
            id: 'pdf_version',
            pattern: /^\/pdf-version\/?$/,
            validation: 'html_shell',
            expectedStatus: 200,
            expectedContentType: 'text/html',
            blockOnFailure: false
        }
    ];

    const requiredApiExpectations = [
        {
            id: 'cache_version',
            pattern: /^\/api\/OfflineCase\/cache-version/,
            validation: 'json_has_base_version',
            expectedStatus: 200,
            expectedContentType: 'application/json',
            blockOnFailure: true
        },
        {
            id: 'version_validation',
            pattern: /^\/api\/version\/.*\/validation$/,
            validation: 'javascript_non_empty',
            expectedStatus: 200,
            blockOnFailure: true
        },
        {
            id: 'ui_specification',
            pattern: /^\/api\/version\/.*\/ui_specification$/,
            validation: 'json_has_form_design',
            expectedStatus: 200,
            expectedContentType: 'application/json',
            blockOnFailure: true
        },
        {
            id: 'version_metadata',
            pattern: /^\/api\/version\/.*\/metadata$/,
            validation: 'json_has_children',
            expectedStatus: 200,
            expectedContentType: 'application/json',
            blockOnFailure: true
        },
        {
            id: 'release_version',
            pattern: /^\/api\/version\/release-version$/,
            validation: 'json_version_value',
            expectedStatus: 200,
            expectedContentType: null,
            blockOnFailure: true
        },
        {
            id: 'metadata',
            pattern: /^\/api\/metadata$/,
            validation: 'json_has_children',
            expectedStatus: 200,
            expectedContentType: 'application/json',
            blockOnFailure: true
        },
        {
            id: 'version_specification',
            pattern: /^\/api\/metadata\/version_specification$/,
            validation: 'javascript_non_empty',
            expectedStatus: 200,
            blockOnFailure: true
        },
        {
            id: 'my_roles',
            pattern: /^\/api\/user_role_jurisdiction_view\/my-roles/,
            validation: 'json_array_or_object',
            expectedStatus: 200,
            expectedContentType: 'application/json',
            blockOnFailure: true
        },
        {
            id: 'my_user',
            pattern: /^\/api\/user\/my-user$/,
            validation: 'json_object',
            expectedStatus: 200,
            expectedContentType: 'application/json',
            blockOnFailure: true
        },
        {
            id: 'jurisdiction_tree',
            pattern: /^\/api\/jurisdiction_tree$/,
            validation: 'json_has_children',
            expectedStatus: 200,
            expectedContentType: 'application/json',
            blockOnFailure: true
        },
        {
            id: 'form_access',
            pattern: /^\/_users\/GetFormAccess/,
            validation: 'json_array_or_object',
            expectedStatus: 200,
            expectedContentType: 'application/json',
            blockOnFailure: true
        },
        {
            id: 'duplicate_multiform_list',
            pattern: /^\/Case\/GetDuplicateMultiFormList/,
            validation: 'json_array_or_object',
            expectedStatus: 200,
            expectedContentType: 'application/json',
            blockOnFailure: false
        }
    ];

    const cachedRoutes = [
            /^\/$/,
            /^\/Home\/Index\/?$/,
            /^\/Case\/?$/,
            /^\/Account\/OfflineLogin\/?$/i,
            /^\/Account\/Login\/?$/i,
            /^\/Case\/([^\/]+)\/summary$/,
            /^\/Case\/([^\/]+)\/0\/home_record$/,
            /^\/Case\/([^\/]+)\/0\/death_certificate$/,
            /^\/Case\/([^\/]+)\/0\/birth_fetal_death_certificate_parent$/,
            /^\/Case\/([^\/]+)\/0\/birth_certificate_infant_fetal_section$/,
            /^\/Case\/([^\/]+)\/0\/cvs$/,
            /^\/Case\/([^\/]+)\/0\/social_and_environmental_profile$/,
            /^\/Case\/([^\/]+)\/0\/autopsy_report$/,
            /^\/Case\/([^\/]+)\/0\/prenatal$/,
            /^\/Case\/([^\/]+)\/0\/er_visit_and_hospital_medical_records$/,
            /^\/Case\/([^\/]+)\/0\/other_medical_office_visits$/,
            /^\/Case\/([^\/]+)\/0\/medical_transport$/,
            /^\/Case\/([^\/]+)\/0\/mental_health_profile$/,
            /^\/Case\/([^\/]+)\/0\/informant_interviews$/,
            /^\/Case\/([^\/]+)\/0\/case_narrative$/,
            /^\/Case\/([^\/]+)\/0\/committee_review$/,
            /^\/pdf-version\/?$/
        ];

    const cachedApiRoutes = [
            /^\/api\/case\?case_id=/,
            /^\/api\/case_view\/record-id-list/,
            /^\/api\/case_view\/offline-documents/,
            /^\/api\/case_view$/,
            /^\/api\/OfflineCase\/cache-version/,
            /^\/api\/version\/.*\/validation$/,
            /^\/api\/version\/.*\/ui_specification$/,
            /^\/api\/version\/.*\/metadata$/,
            /^\/api\/version\/release-version$/,
            /^\/api\/metadata$/,
            /^\/api\/metadata\/version_specification$/,
            /^\/api\/user_role_jurisdiction_view\/my-roles/,
            /^\/api\/user\/my-user$/,
            /^\/api\/jurisdiction_tree$/,
            /^\/api\/cvsAPI$/,
            /^\/_users\/GetFormAccess/,
            /^\/Case\/GetDuplicateMultiFormList/,
            /^\/broadcast-message\/GetBroadcastMessageList/
        ];

    root.OfflineCacheManifest = {
        requiredStaticExpectations: allRequiredStaticExpectations,
        requiredStaticFiles: allRequiredStaticExpectations.map(item => item.path),
        optionalStaticFiles: [],
        requiredRouteExpectations: requiredRouteExpectations,
        requiredRoutes: requiredRouteExpectations.map(item => item.pattern),
        requiredApiExpectations: requiredApiExpectations,
        requiredApiRoutes: requiredApiExpectations.map(item => item.pattern),
        cachedRoutes: cachedRoutes,
        cachedApiRoutes: cachedApiRoutes
    };
})(typeof self !== 'undefined' ? self : window);
