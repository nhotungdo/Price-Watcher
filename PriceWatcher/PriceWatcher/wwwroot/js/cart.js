// Cart functionality
(function() {
    'use strict';

    // Cart Client - Full implementation
    class CartClient {
        constructor() {
            this.items = [];
            this.summary = { subtotal: 0, discount: 0, total: 0, itemCount: 0 };
            this.isReady = false;
        }

        async init() {
            await this.loadCart();
            this.isReady = true;
            window.dispatchEvent(new CustomEvent('cart:ready'));
        }

        async loadCart() {
            try {
                const response = await fetch('/api/cart');
                if (!response.ok) {
                    console.error('Failed to load cart:', response.status);
                    return;
                }
                
                const cart = await response.json();
                this.items = cart.items || [];
                this.summary = cart.summary || { subtotal: 0, discount: 0, total: 0, itemCount: 0 };
                
                updateCartBadge(this.summary.itemCount);
                window.dispatchEvent(new CustomEvent('cart:updated', { detail: cart }));
            } catch (error) {
                console.error('Failed to load cart:', error);
            }
        }

        async addItem(productData) {
            try {
                const response = await fetch('/api/cart/items', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(productData)
                });

                if (!response.ok) {
                    throw new Error('Failed to add item to cart');
                }

                const cart = await response.json();
                this.items = cart.items || [];
                this.summary = cart.summary || { subtotal: 0, discount: 0, total: 0, itemCount: 0 };
                
                updateCartBadge(this.summary.itemCount);
                window.dispatchEvent(new CustomEvent('cart:updated', { detail: cart }));
                
                return cart;
            } catch (error) {
                console.error('Failed to add item to cart:', error);
                throw error;
            }
        }

        async updateQuantity(cartItemId, quantity) {
            try {
                const url = `/api/cart/items/${cartItemId}`;
                const response = await fetch(url, {
                    method: 'PATCH',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ quantity: parseInt(quantity) })
                });

                if (!response.ok) {
                    throw new Error('Failed to update quantity');
                }

                const cart = await response.json();
                this.items = cart.items || [];
                this.summary = cart.summary || { subtotal: 0, discount: 0, total: 0, itemCount: 0 };
                
                updateCartBadge(this.summary.itemCount);
                window.dispatchEvent(new CustomEvent('cart:updated', { detail: cart }));
                
                return cart;
            } catch (error) {
                console.error('Failed to update quantity:', error);
                throw error;
            }
        }

        async removeItem(cartItemId) {
            try {
                const url = `/api/cart/items/${cartItemId}`;
                const response = await fetch(url, {
                    method: 'DELETE'
                });

                if (!response.ok) {
                    throw new Error('Failed to remove item');
                }

                const cart = await response.json();
                this.items = cart.items || [];
                this.summary = cart.summary || { subtotal: 0, discount: 0, total: 0, itemCount: 0 };
                
                updateCartBadge(this.summary.itemCount);
                window.dispatchEvent(new CustomEvent('cart:updated', { detail: cart }));
                
                return cart;
            } catch (error) {
                console.error('Failed to remove item:', error);
                throw error;
            }
        }
    }

    // Initialize cart on page load
    document.addEventListener('DOMContentLoaded', function() {
        window.cartClient = new CartClient();
        window.cartClient.init();
    });

    // Load cart count from API (legacy function for compatibility)
    async function loadCartCount() {
        if (window.cartClient) {
            await window.cartClient.loadCart();
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
