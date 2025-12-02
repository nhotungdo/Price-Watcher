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

    // Filter and Sort functionality
    const sortSelect = document.getElementById('sortSelect');
    const applyFiltersBtn = document.getElementById('applyFilters');
    const clearFiltersBtn = document.getElementById('clearFilters');

    function applyFiltersAndSort() {
        const query = inputEl?.value || initialState.query;
        const url = new URL(window.location.origin + '/search/results');
        url.searchParams.set('q', query);

        // Get selected platforms
        const platforms = Array.from(document.querySelectorAll('.filter-platform:checked'))
            .map(cb => cb.value);
        if (platforms.length > 0) {
            url.searchParams.set('platforms', platforms.join(','));
        }

        // Get price range
        const priceMin = document.getElementById('priceMin')?.value;
        const priceMax = document.getElementById('priceMax')?.value;
        if (priceMin) url.searchParams.set('minPrice', priceMin);
        if (priceMax) url.searchParams.set('maxPrice', priceMax);

        // Get rating
        const rating = document.querySelector('.filter-rating:checked')?.value;
        if (rating) url.searchParams.set('minRating', rating);

        // Get other options
        const hasDiscount = document.getElementById('filterDiscount')?.checked;
        const hasFreeShip = document.getElementById('filterFreeship')?.checked;
        if (hasDiscount) url.searchParams.set('hasDiscount', 'true');
        if (hasFreeShip) url.searchParams.set('freeShip', 'true');

        // Get sort
        const sort = sortSelect?.value;
        if (sort) url.searchParams.set('sort', sort);

        window.location.href = url.toString();
    }

    sortSelect?.addEventListener('change', () => {
        applyFiltersAndSort();
    });

    applyFiltersBtn?.addEventListener('click', () => {
        applyFiltersAndSort();
    });

    clearFiltersBtn?.addEventListener('click', () => {
        // Clear all filters
        document.querySelectorAll('.filter-platform').forEach(cb => cb.checked = false);
        document.querySelectorAll('.filter-rating').forEach(rb => rb.checked = false);
        document.querySelectorAll('.filter-option').forEach(cb => cb.checked = false);
        document.getElementById('priceMin').value = '';
        document.getElementById('priceMax').value = '';
        if (sortSelect) sortSelect.value = '';

        // Reload without filters
        const query = inputEl?.value || initialState.query;
        goToPage(query, 1);
    });

    // Load filters from URL on page load
    const urlParams = new URLSearchParams(window.location.search);
    const platforms = urlParams.get('platforms')?.split(',') || [];
    platforms.forEach(p => {
        const checkbox = document.querySelector(`.filter-platform[value="${p}"]`);
        if (checkbox) checkbox.checked = true;
    });

    const minPrice = urlParams.get('minPrice');
    const maxPrice = urlParams.get('maxPrice');
    if (minPrice) document.getElementById('priceMin').value = minPrice;
    if (maxPrice) document.getElementById('priceMax').value = maxPrice;

    const minRating = urlParams.get('minRating');
    if (minRating) {
        const ratingRadio = document.querySelector(`.filter-rating[value="${minRating}"]`);
        if (ratingRadio) ratingRadio.checked = true;
    }

    if (urlParams.get('hasDiscount') === 'true') {
        document.getElementById('filterDiscount').checked = true;
    }
    if (urlParams.get('freeShip') === 'true') {
        document.getElementById('filterFreeship').checked = true;
    }

    const sort = urlParams.get('sort');
    if (sort && sortSelect) {
        sortSelect.value = sort;
    }
});

