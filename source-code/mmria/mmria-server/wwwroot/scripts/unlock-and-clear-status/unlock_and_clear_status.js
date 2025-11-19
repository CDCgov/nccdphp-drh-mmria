function update_submit_button_disabled_state()
{
    const p_state_database_select = document.getElementById('StateDatabase');
    const p_record_id_input = document.getElementById('RecordId');
    const submit_button = document.getElementById('find-case-submit-button');
    if (p_state_database_select.value !== "" && (p_record_id_input.value !== null && p_record_id_input.value.trim() !== ""))
    {
        submit_button.disabled = false;
        submit_button.removeAttribute('aria-disabled');
    }
    else
    {
        submit_button.disabled = true;
        submit_button.setAttribute('aria-disabled', 'true');
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const stateSelect = document.getElementById('StateDatabase');
    const recordInput = document.getElementById('RecordId');

    // attach live validation listeners
    if (stateSelect) {
        stateSelect.addEventListener('change', update_submit_button_disabled_state);
        stateSelect.addEventListener('input', update_submit_button_disabled_state);
    }
    if (recordInput) {
        recordInput.addEventListener('input', update_submit_button_disabled_state);
    }

    // initialize button state on load
    update_submit_button_disabled_state();
});