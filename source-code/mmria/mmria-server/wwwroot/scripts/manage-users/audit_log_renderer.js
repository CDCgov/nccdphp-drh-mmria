function audit_log_renderer()
{
    g_is_audit_log_view = true;
    role_user_name = "";
    init_audit_history();
    const result = `
        <div class="d-flex">
            <div class='horizontal-control mb-3 mt-3 pl-0 col-md-4'>
                <input id="audit_history_filter" onkeyup="filter_audit_history(this.value)" class='form-control' type='text' placeholder='Filter account history...'>
                <button disabled aria-disabled="true" id="clear_filter_button" onclick="clear_audit_filter()" class="btn btn-link ml-2 pl-0" aria-label="clear filter" value="clear filter">Clear filter</button>
            </div>
            <div class="d-flex ml-auto audit_navigation_container">
                ${render_account_log_history_table_navigation_view()}
            </div>
        </div>
        <div>
            <table class='table table-layout-fixed align-cell-top'><caption class='table-caption'>User account history table</caption>
                <thead>
                    <tr class='header-level-2'>
                        <th width='175' scope='colgroup'>User</th>
                        <th width='200' scope='colgroup'>Date/Time</th>
                        <th width="175">Editor</th>
                        <th width='150' scope='colgroup;'>Action</th>
                        <th>Field</th>
                        <th width="150">Old Value</th>
                        <th width="150">New Value</th>
                    </tr>
                </thead>
                <tbody id="audit_history_table_body">
                    ${render_audit_log_history_body()}
                </tbody>
            </table>
        </div>
    `;
    show_hide_user_management_back_button(true);
    set_page_title("View Audit Log");
    document.getElementById('form_content_id').innerHTML = result;
}