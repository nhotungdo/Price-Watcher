// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Login Form Handler
document.addEventListener('DOMContentLoaded', function() {
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const email = document.getElementById('loginEmail').value;
            const password = document.getElementById('loginPassword').value;
            
            // TODO: Implement actual login API call
            console.log('Login attempt:', { email, password });
            alert('Tính năng đăng nhập đang được phát triển...');
            
            // Example: Close modal on success
            // const loginModal = bootstrap.Modal.getInstance(document.getElementById('loginModal'));
            // loginModal.hide();
        });
    }

    // Register Form Handler
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const name = document.getElementById('registerName').value;
            const email = document.getElementById('registerEmail').value;
            const password = document.getElementById('registerPassword').value;
            const confirmPassword = document.getElementById('registerConfirmPassword').value;
            
            // Validate passwords match
            if (password !== confirmPassword) {
                alert('Mật khẩu xác nhận không khớp!');
                return;
            }
            
            // TODO: Implement actual register API call
            console.log('Register attempt:', { name, email, password });
            alert('Tính năng đăng ký đang được phát triển...');
            
            // Example: Close modal on success
            // const registerModal = bootstrap.Modal.getInstance(document.getElementById('registerModal'));
            // registerModal.hide();
        });
    }

    // Google Login/Register Buttons
    var loginBtnEl = document.getElementById('googleLoginBtn');
    if (loginBtnEl && !loginBtnEl.dataset.bound) {
        loginBtnEl.dataset.bound = 'true';
        loginBtnEl.addEventListener('click', function() {
            var returnUrl = '/';
            window.location.href = '/auth/google?returnUrl=' + encodeURIComponent(returnUrl);
        });
    }

    var registerBtnEl = document.getElementById('googleRegisterBtn');
    if (registerBtnEl && !registerBtnEl.dataset.bound) {
        registerBtnEl.dataset.bound = 'true';
        registerBtnEl.addEventListener('click', function() {
            var returnUrl = '/';
            window.location.href = '/auth/google?returnUrl=' + encodeURIComponent(returnUrl);
        });
    }

    // Switch from register to login modal
    const switchToLoginLink = document.querySelector('[data-bs-target="#loginModal"]');
    if (switchToLoginLink) {
        switchToLoginLink.addEventListener('click', function() {
            const registerModal = bootstrap.Modal.getInstance(document.getElementById('registerModal'));
            if (registerModal) {
                registerModal.hide();
            }
        });
    }
});
