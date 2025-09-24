document.addEventListener('click', function(event) {
    const username_element = document.getElementById('login_name');
    const password_element = document.getElementById('login_value');

    if(username_element.value === '' || username_element.value == null) 
    {
        username_element.classList.add('is-invalid');
    }
    else
    {
        username_element.classList.remove('is-invalid');
    }
    if(password_element.value === '' || password_element.value == null)
    {
        password_element.classList.add('is-invalid');
    }
    else
    {
        password_element.classList.remove('is-invalid');
    }
});