// Cart functionality
(function() {
    'use strict';

    // Initialize cart on page load
    document.addEventListener('DOMContentLoaded', function() {
        loadCartCount();
    });

    // Load cart count from API
    async function loadCartCount() {
        try {
            const response = await fetch('/api/cart');
            if (!response.ok) return;
            
            const cart = await response.json();
            updateCartBadge(cart.summary.itemCount);
        } catch (error) {
            console.error('Failed to load cart count:', error);
        }
    }

    // Update cart badge
    function updateCartBadge(count) {
        const badge = document.getElementById('cartCount');
        if (badge) {
            badge.textContent = count;
            badge.style.display = count > 0 ? 'inline-block' : 'none';
        }
    }

    // Export functions for use in other scripts
    window.cartUtils = {
        updateCartBadge: updateCartBadge,
        loadCartCount: loadCartCount
    };

})();
