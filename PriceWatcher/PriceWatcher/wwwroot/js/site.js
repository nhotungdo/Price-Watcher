// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Login Form Handler
document.addEventListener('DOMContentLoaded', function() {
    const loginForm = document.getElementById('loginModalForm');
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
    const registerForm = document.getElementById('registerModalForm');
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
            var regEl = document.getElementById('registerModal');
            if (regEl && window.bootstrap && bootstrap.Modal && typeof bootstrap.Modal.getInstance === 'function') {
                const registerModal = bootstrap.Modal.getInstance(regEl);
                if (registerModal) {
                    registerModal.hide();
                }
            }
        });
    }

    // ===== Client JS Logger =====
    (function setupJsLogger() {
        const endpoint = '/metrics/js';
        const page = window.location.pathname + window.location.search;
        const ua = navigator.userAgent;

        async function send(payload) {
            try {
                await fetch(endpoint, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload),
                    credentials: 'same-origin'
                });
            } catch { /* swallow */ }
        }

        const originalError = console.error.bind(console);
        const originalWarn = console.warn.bind(console);
        console.error = function (...args) {
            originalError(...args);
            const msg = args.map(x => (typeof x === 'string' ? x : (x && x.message) || JSON.stringify(x))).join(' ');
            send({ level: 'error', message: msg, page, userAgent: ua, timestamp: new Date().toISOString() });
        };
        console.warn = function (...args) {
            originalWarn(...args);
            const msg = args.map(x => (typeof x === 'string' ? x : (x && x.message) || JSON.stringify(x))).join(' ');
            send({ level: 'warning', message: msg, page, userAgent: ua, timestamp: new Date().toISOString() });
        };

        window.addEventListener('error', function (e) {
            send({ level: 'error', message: e.message, stack: e.error && e.error.stack, source: e.filename, line: e.lineno, column: e.colno, page, userAgent: ua, timestamp: new Date().toISOString() });
        });
        window.addEventListener('unhandledrejection', function (e) {
            const reason = e.reason || {};
            const msg = typeof reason === 'string' ? reason : (reason.message || 'Unhandled rejection');
            send({ level: 'error', message: msg, stack: reason.stack, page, userAgent: ua, timestamp: new Date().toISOString() });
        });
    })();
});
