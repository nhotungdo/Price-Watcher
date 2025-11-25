// ========================================
// HOMEPAGE JAVASCRIPT - TIKI.VN INSPIRED
// ========================================

(function () {
    'use strict';

    // ========================================
    // 1. SEARCH AUTOCOMPLETE
    // ========================================
    const searchInput = document.getElementById('headerSearch');
    const autocompleteDiv = document.getElementById('searchAutocomplete');
    let searchTimeout;

    if (searchInput && autocompleteDiv) {
        searchInput.addEventListener('input', function (e) {
            const query = e.target.value.trim();

            // Clear previous timeout
            clearTimeout(searchTimeout);

            if (query.length < 2) {
                autocompleteDiv.style.display = 'none';
                return;
            }

            // Debounce search
            searchTimeout = setTimeout(() => {
                fetchAutocomplete(query);
            }, 300);
        });

        // Close autocomplete when clicking outside
        document.addEventListener('click', function (e) {
            if (!searchInput.contains(e.target) && !autocompleteDiv.contains(e.target)) {
                autocompleteDiv.style.display = 'none';
            }
        });
    }

    async function fetchAutocomplete(query) {
        try {
            const response = await fetch(`/search/autocomplete?q=${encodeURIComponent(query)}`);
            const data = await response.json();

            if (data && data.length > 0) {
                displayAutocomplete(data);
            } else {
                autocompleteDiv.style.display = 'none';
            }
        } catch (error) {
            console.error('Autocomplete error:', error);
            autocompleteDiv.style.display = 'none';
        }
    }

    function displayAutocomplete(items) {
        const content = autocompleteDiv.querySelector('.autocomplete-content');
        content.innerHTML = items.map(item => `
            <div class="autocomplete-item" data-value="${item.name}">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="11" cy="11" r="8"></circle>
                    <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
                </svg>
                <span>${item.name}</span>
            </div>
        `).join('');

        // Add click handlers
        content.querySelectorAll('.autocomplete-item').forEach(item => {
            item.addEventListener('click', function () {
                searchInput.value = this.dataset.value;
                autocompleteDiv.style.display = 'none';
                searchInput.closest('form').submit();
            });
        });

        autocompleteDiv.style.display = 'block';
    }

    // ========================================
    // 2. COUNTDOWN TIMER
    // ========================================
    function initCountdown() {
        const hoursEl = document.getElementById('hours');
        const minutesEl = document.getElementById('minutes');
        const secondsEl = document.getElementById('seconds');

        if (!hoursEl || !minutesEl || !secondsEl) return;

        // Set end time (e.g., 2 hours from now)
        const endTime = new Date().getTime() + (2 * 60 * 60 * 1000);

        function updateCountdown() {
            const now = new Date().getTime();
            const distance = endTime - now;

            if (distance < 0) {
                hoursEl.textContent = '00';
                minutesEl.textContent = '00';
                secondsEl.textContent = '00';
                return;
            }

            const hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
            const minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
            const seconds = Math.floor((distance % (1000 * 60)) / 1000);

            hoursEl.textContent = String(hours).padStart(2, '0');
            minutesEl.textContent = String(minutes).padStart(2, '0');
            secondsEl.textContent = String(seconds).padStart(2, '0');
        }

        updateCountdown();
        setInterval(updateCountdown, 1000);
    }

    // ========================================
    // 3. SHOPPING CART (synced via server cart client)
    // ========================================
    window.addEventListener('cart:ready', () => {
        if (window.cartClient) {
            window.cartClient.updateCartBadge();
        }
    });

    // ========================================
    // 4. LAZY LOADING IMAGES
    // ========================================
    function initLazyLoading() {
        const images = document.querySelectorAll('img[loading="lazy"]');

        if ('IntersectionObserver' in window) {
            const imageObserver = new IntersectionObserver((entries, observer) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        const img = entry.target;
                        img.src = img.dataset.src || img.src;
                        img.classList.add('loaded');
                        observer.unobserve(img);
                    }
                });
            }, {
                rootMargin: '50px'
            });

            images.forEach(img => imageObserver.observe(img));
        } else {
            // Fallback for browsers without IntersectionObserver
            images.forEach(img => {
                img.src = img.dataset.src || img.src;
                img.classList.add('loaded');
            });
        }
    }

    // ========================================
    // 5. CAROUSEL AUTO-PLAY WITH PAUSE ON HOVER
    // ========================================
    function initCarousel() {
        const carousel = document.getElementById('heroCarousel');
        if (!carousel) return;

        const bsCarousel = new bootstrap.Carousel(carousel, {
            interval: 5000,
            ride: 'carousel',
            pause: 'hover',
            wrap: true
        });

        const indicator = document.getElementById('heroSlideIndicator');
        function updateIndicator() {
            const items = carousel.querySelectorAll('.carousel-item');
            const active = carousel.querySelector('.carousel-item.active');
            const index = Array.from(items).indexOf(active) + 1;
            if (indicator) indicator.textContent = `${index}/${items.length}`;
        }
        carousel.addEventListener('slid.bs.carousel', updateIndicator);
        updateIndicator();

        // Touch/swipe support
        let touchStartX = 0;
        let touchEndX = 0;

        carousel.addEventListener('touchstart', e => {
            touchStartX = e.changedTouches[0].screenX;
        });

        carousel.addEventListener('touchend', e => {
            touchEndX = e.changedTouches[0].screenX;
            handleSwipe();
        });

        function handleSwipe() {
            if (touchEndX < touchStartX - 50) {
                bsCarousel.next();
            }
            if (touchEndX > touchStartX + 50) {
                bsCarousel.prev();
            }
        }
    }

    // ========================================
    // 6. PRODUCT IMAGE ZOOM ON HOVER
    // ========================================
    function initProductImageZoom() {
        const productCards = document.querySelectorAll('.product-card');

        productCards.forEach(card => {
            const image = card.querySelector('.product-image');
            if (!image) return;

            card.addEventListener('mouseenter', () => {
                image.style.transform = 'scale(1.1)';
            });

            card.addEventListener('mouseleave', () => {
                image.style.transform = 'scale(1)';
            });
        });
    }

    // ========================================
    // 7. SMOOTH SCROLL FOR ANCHOR LINKS
    // ========================================
    function initSmoothScroll() {
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', function (e) {
                const href = this.getAttribute('href');
                if (href === '#') return;

                const target = document.querySelector(href);
                if (target) {
                    e.preventDefault();
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    // ========================================
    // 8. PERFORMANCE MONITORING
    // ========================================
    function monitorPerformance() {
        if ('PerformanceObserver' in window) {
            // Monitor Largest Contentful Paint
            const lcpObserver = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                const lastEntry = entries[entries.length - 1];
                console.log('LCP:', lastEntry.renderTime || lastEntry.loadTime);
            });
            lcpObserver.observe({ entryTypes: ['largest-contentful-paint'] });

            // Monitor First Input Delay
            const fidObserver = new PerformanceObserver((list) => {
                const entries = list.getEntries();
                entries.forEach(entry => {
                    console.log('FID:', entry.processingStart - entry.startTime);
                });
            });
            fidObserver.observe({ entryTypes: ['first-input'] });
        }
    }

    // ========================================
    // INITIALIZATION
    // ========================================
    document.addEventListener('DOMContentLoaded', function () {
        initCountdown();
        initLazyLoading();
        initCarousel();
        initProductImageZoom();
        initSmoothScroll();

        // Monitor performance in development
        if (window.location.hostname === 'localhost') {
            monitorPerformance();
        }

        console.log('Homepage initialized successfully');
    });

    // Make cart client available globally
    if (window.cartClient) {
        window.ShoppingCart = window.cartClient;
    }

})();

// ========================================
// TOAST NOTIFICATION STYLES (Add to CSS)
// ========================================
const style = document.createElement('style');
style.textContent = `
    .cart-toast {
        position: fixed;
        bottom: 20px;
        right: 20px;
        background: #38383d;
        color: white;
        padding: 1rem 1.5rem;
        border-radius: 8px;
        display: flex;
        align-items: center;
        gap: 0.75rem;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        transform: translateY(100px);
        opacity: 0;
        transition: all 0.3s ease;
        z-index: 9999;
    }

    .cart-toast.show {
        transform: translateY(0);
        opacity: 1;
    }

    .cart-toast svg {
        color: #4caf50;
    }
`;
document.head.appendChild(style);
