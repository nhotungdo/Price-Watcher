document.addEventListener('DOMContentLoaded', () => {
    const formEl = document.getElementById('searchForm');
    const inputEl = document.getElementById('searchInput');
    const alertContainer = document.getElementById('searchAlerts');
    const paginationButtons = document.querySelectorAll('.pagination .page-link');
    const suggestionButtons = document.querySelectorAll('.search-suggestion');
    const initialState = window.__initialSearchState || { query: '', page: 1 };

    function showAlert(message, type = 'warning') {
        if (!alertContainer) return;
        const alert = document.createElement('div');
        alert.className = `alert alert-${type} d-flex align-items-center`;
        alert.innerHTML = `<span class="me-2">ℹ️</span><span>${message}</span>`;
        alertContainer.innerHTML = '';
        alertContainer.appendChild(alert);
    }

    function goToPage(query, page = 1) {
        const sanitized = (query || '').trim();
        if (!sanitized) {
            showAlert('Vui lòng nhập từ khóa hoặc URL sản phẩm.', 'warning');
            return;
        }
        const target = new URL(window.location.origin + '/search/results');
        target.searchParams.set('q', sanitized);
        if (page > 1) {
            target.searchParams.set('page', page);
        }
        window.location.href = target.toString();
    }

    formEl?.addEventListener('submit', (evt) => {
        evt.preventDefault();
        goToPage(inputEl?.value ?? '', 1);
    });

    inputEl?.addEventListener('keydown', (evt) => {
        if (evt.key === 'Enter') {
            evt.preventDefault();
            goToPage(inputEl.value, 1);
        }
    });

    paginationButtons.forEach((btn) => {
        btn.addEventListener('click', (evt) => {
            evt.preventDefault();
            const page = Number(btn.dataset.page);
            if (!Number.isFinite(page) || btn.closest('.page-item')?.classList.contains('disabled')) {
                return;
            }
            goToPage(inputEl?.value || initialState.query, page);
        });
    });

    suggestionButtons.forEach((btn) => {
        btn.addEventListener('click', () => {
            const value = btn.dataset.value || '';
            if (inputEl) {
                inputEl.value = value;
            }
            goToPage(value, 1);
        });
    });
});

