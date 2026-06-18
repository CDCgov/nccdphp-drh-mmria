// Clear the going-offline modal gate so it can fire again after the next login.
// This runs every time the login page is loaded (i.e., after logout or session expiry).
try { localStorage.removeItem('offline_modal_shown'); } catch (e) { /* storage unavailable */ }

const login_button = document.getElementById('login_button');
const username_element = document.getElementById('login_name');
const password_element = document.getElementById('login_value');
const username_error_message_element = document.getElementById('username_error_message');
const password_error_message_element = document.getElementById('password_error_message');
const login_error_message_element = document.getElementById('login_error_message');
const show_hide_password_button = document.getElementById('show_hide_password');

const USERNAME_ERROR = 'USERNAME_ERROR';
const PASSWORD_ERROR = 'PASSWORD_ERROR';

var error_messages = [];

function show_hide_password(field_id) {
    const passwordField = document.getElementById(field_id);
    const button = passwordField.nextElementSibling.querySelector('button');
    const icon = button.querySelector('span');

    if (passwordField.type === 'password') {
        passwordField.type = 'text';
        icon.classList.remove('cdc-icon-eye-solid');
        icon.classList.remove('x22');
        icon.classList.add('x24');
        icon.classList.add('cdc-icon-minus');
    } else {
        passwordField.type = 'password';
        icon.classList.remove('cdc-icon-minus');
        icon.classList.remove('x24');
        icon.classList.add('x22');
        icon.classList.add('cdc-icon-eye-solid');
    }
}


// Helper functions to manage global error state
function add_error(code) {
    if (!error_messages.includes(code)) error_messages.push(code);
}
function remove_error(code) {
    const i = error_messages.indexOf(code);
    if (i > -1) error_messages.splice(i, 1);
}

function validate_login_fields() {
    // Run validators (each updates global error_messages)
    const user_valid = set_username_validation();
    const password_valid = set_password_validation();
    show_login_error_message();
    return user_valid && password_valid;
}

function set_username_validation() {
    let is_valid = true;
    if (username_element) {
        if (!username_element.value?.trim()) {
            username_element.classList.add('error-text');
            is_valid = false;
        } else {
            username_element.classList.remove('error-text');
        }
    }
    is_valid ? remove_error(USERNAME_ERROR) : add_error(USERNAME_ERROR);
    return is_valid;
}

function set_password_validation() {
    let is_valid = true;
    if (password_element) {
        if (!password_element.value) {
            password_element.classList.add('error-text');
            is_valid = false;
        } else {
            password_element.classList.remove('error-text');
        }
    }
    is_valid ? remove_error(PASSWORD_ERROR) : add_error(PASSWORD_ERROR);
    return is_valid;
}

function show_login_error_message() {
    if (error_messages.length >= 0) 
    {
        login_error_message_element.classList.add('d-none');
    }

    if (error_messages.includes(USERNAME_ERROR)) {
        username_error_message_element.classList.remove('d-none');
    } else {
        username_error_message_element.classList.add('d-none');
    }

    if (error_messages.includes(PASSWORD_ERROR)) {
        password_error_message_element.classList.remove('d-none');
    } else {
        password_error_message_element.classList.add('d-none');
    }
}

// Event wiring
if (login_button) {
    login_button.addEventListener('click', e => {
        if (!validate_login_fields()) {
            e.preventDefault();
        }
    });
}

if (username_element) {
    const usernameHandlers = () => { set_username_validation(); show_login_error_message(); };
    username_element.addEventListener('input', usernameHandlers);
}

if (password_element) {
    const passwordHandlers = () => { set_password_validation(); show_login_error_message(); };
    password_element.addEventListener('input', passwordHandlers);
}

if (show_hide_password_button) {
    show_hide_password_button.addEventListener('click', () => {
        show_hide_password('login_value');
    });
}
