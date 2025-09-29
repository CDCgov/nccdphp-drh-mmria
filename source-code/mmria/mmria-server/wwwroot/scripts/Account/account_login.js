const login_button = document.getElementById('login_button');
const username_element = document.getElementById('login_name');
const password_element = document.getElementById('login_value');
const username_error_message_element = document.getElementById('username_error_message');
const password_error_message_element = document.getElementById('password_error_message');
const default_error_message_element = document.getElementById('default_error_message');
const error_message = document.getElementById('login_error_message');

const USERNAME_ERROR = 'USERNAME_ERROR';
const PASSWORD_ERROR = 'PASSWORD_ERROR';

var error_messages = [];

// Helper functions to manage global error state
function addError(code) {
    if (!error_messages.includes(code)) error_messages.push(code);
}
function removeError(code) {
    const i = error_messages.indexOf(code);
    if (i > -1) error_messages.splice(i, 1);
}

function validate_login_fields() {
    // Run validators (each updates global error_messages)
    const userOk = set_username_validation();
    const passOk = set_password_validation();
    show_login_error_message();
    return userOk && passOk;
}

function set_username_validation() {
    let is_valid = true;
    if (username_element) {
        if (!username_element.value?.trim()) {
            username_element.classList.add('is-invalid');
            is_valid = false;
        } else {
            username_element.classList.remove('is-invalid');
        }
    }
    is_valid ? removeError(USERNAME_ERROR) : addError(USERNAME_ERROR);
    return is_valid;
}

function set_password_validation() {
    let is_valid = true;
    if (password_element) {
        if (!password_element.value) {
            password_element.classList.add('is-invalid');
            is_valid = false;
        } else {
            password_element.classList.remove('is-invalid');
        }
    }
    is_valid ? removeError(PASSWORD_ERROR) : addError(PASSWORD_ERROR);
    return is_valid;
}

function show_login_error_message() {
    const container = document.getElementById('login_error_message');
    if (!container) return;

    if (error_messages.length) {
        container.classList.remove('d-none');
        default_error_message_element.classList.add('d-none');
    } else {
        container.classList.add('d-none');
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
    username_element.addEventListener('blur', usernameHandlers);
}

if (password_element) {
    const passwordHandlers = () => { set_password_validation(); show_login_error_message(); };
    password_element.addEventListener('input', passwordHandlers);
    password_element.addEventListener('blur', passwordHandlers);
}