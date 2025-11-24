// Shopping Cart Manager - Client-side cart functionality
class ShoppingCart {
    constructor() {
        this.storageKey = 'pricewatcher_cart';
        this.items = this.loadCart();
        this.updateCartBadge();
    }

    // Load cart from localStorage
    loadCart() {
        try {
            const cartData = localStorage.getItem(this.storageKey);
            return cartData ? JSON.parse(cartData) : [];
        } catch (error) {
            console.error('Error loading cart:', error);
            return [];
        }
    }

    // Save cart to localStorage
    saveCart() {
        try {
            localStorage.setItem(this.storageKey, JSON.stringify(this.items));
            this.updateCartBadge();
            this.triggerCartUpdate();
        } catch (error) {
            console.error('Error saving cart:', error);
            this.showNotification('Không thể lưu giỏ hàng', 'error');
        }
    }

    // Add item to cart
    addItem(product) {
        // Validate product data
        if (!this.validateProduct(product)) {
            this.showNotification('Thông tin sản phẩm không hợp lệ', 'error');
            return false;
        }

        // Check if product already exists
        const existingIndex = this.items.findIndex(item =>
            item.productId === product.productId &&
            item.platformId === product.platformId
        );

        if (existingIndex !== -1) {
            // Product exists - increase quantity
            const newQuantity = this.items[existingIndex].quantity + (product.quantity || 1);

            // Check max quantity limit (default 99)
            if (newQuantity > 99) {
                this.showNotification('Số lượng tối đa là 99', 'warning');
                return false;
            }

            this.items[existingIndex].quantity = newQuantity;
            this.showNotification(`Đã cập nhật số lượng: ${this.items[existingIndex].name}`, 'success');
        } else {
            // New product - add to cart
            const cartItem = {
                productId: product.productId,
                platformId: product.platformId || 1,
                platformName: product.platformName || 'Tiki',
                name: product.name,
                price: parseFloat(product.price),
                originalPrice: product.originalPrice ? parseFloat(product.originalPrice) : null,
                imageUrl: product.imageUrl,
                url: product.url,
                quantity: product.quantity || 1,
                addedAt: new Date().toISOString()
            };

            this.items.push(cartItem);
            this.showNotification(`Đã thêm vào giỏ hàng: ${product.name}`, 'success');
        }

        this.saveCart();
        return true;
    }

    // Remove item from cart
    removeItem(productId, platformId) {
        const index = this.items.findIndex(item =>
            item.productId === productId && item.platformId === platformId
        );

        if (index !== -1) {
            const removedItem = this.items.splice(index, 1)[0];
            this.saveCart();
            this.showNotification(`Đã xóa: ${removedItem.name}`, 'info');
            return true;
        }
        return false;
    }

    // Update item quantity
    updateQuantity(productId, platformId, quantity) {
        const item = this.items.find(item =>
            item.productId === productId && item.platformId === platformId
        );

        if (item) {
            const newQuantity = parseInt(quantity);

            if (newQuantity < 1) {
                this.removeItem(productId, platformId);
                return;
            }

            if (newQuantity > 99) {
                this.showNotification('Số lượng tối đa là 99', 'warning');
                return;
            }

            item.quantity = newQuantity;
            this.saveCart();
        }
    }

    // Clear entire cart
    clearCart() {
        if (confirm('Bạn có chắc muốn xóa toàn bộ giỏ hàng?')) {
            this.items = [];
            this.saveCart();
            this.showNotification('Đã xóa toàn bộ giỏ hàng', 'info');
        }
    }

    // Get cart items
    getItems() {
        return this.items;
    }

    // Get cart count
    getCount() {
        return this.items.reduce((total, item) => total + item.quantity, 0);
    }

    // Get cart total
    getTotal() {
        return this.items.reduce((total, item) => total + (item.price * item.quantity), 0);
    }

    // Validate product data
    validateProduct(product) {
        if (!product) return false;
        if (!product.productId) return false;
        if (!product.name || product.name.trim() === '') return false;
        if (!product.price || isNaN(product.price) || product.price <= 0) return false;
        return true;
    }

    // Update cart badge in header
    updateCartBadge() {
        const badge = document.getElementById('cartCount');
        if (badge) {
            const count = this.getCount();
            badge.textContent = count;
            badge.style.display = count > 0 ? 'flex' : 'none';
        }
    }

    // Trigger custom event for cart updates
    triggerCartUpdate() {
        const event = new CustomEvent('cartUpdated', {
            detail: {
                items: this.items,
                count: this.getCount(),
                total: this.getTotal()
            }
        });
        window.dispatchEvent(event);
    }

    // Show notification
    showNotification(message, type = 'info') {
        // Create toast notification
        const toast = document.createElement('div');
        toast.className = `cart-toast cart-toast-${type}`;
        toast.innerHTML = `
            <div class="cart-toast-content">
                <span class="cart-toast-icon">${this.getToastIcon(type)}</span>
                <span class="cart-toast-message">${message}</span>
            </div>
        `;

        // Add to body
        document.body.appendChild(toast);

        // Animate in
        setTimeout(() => toast.classList.add('show'), 10);

        // Remove after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    // Get icon for toast type
    getToastIcon(type) {
        const icons = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };
        return icons[type] || icons.info;
    }
}

// Initialize cart globally
window.cart = new ShoppingCart();

// Add to cart button handler
document.addEventListener('click', function (e) {
    const addToCartBtn = e.target.closest('.add-to-cart-btn');
    if (addToCartBtn) {
        e.preventDefault();

        // Get product data from button's parent card
        const productCard = addToCartBtn.closest('[data-product-id]');
        if (!productCard) return;

        const product = {
            productId: parseInt(productCard.dataset.productId),
            platformId: parseInt(productCard.dataset.platformId || '1'),
            platformName: productCard.dataset.platformName || 'Tiki',
            name: productCard.dataset.productName || productCard.querySelector('.product-name')?.textContent,
            price: parseFloat(productCard.dataset.productPrice || productCard.querySelector('.product-price')?.dataset.price || '0'),
            originalPrice: parseFloat(productCard.dataset.productOriginalPrice || '0') || null,
            imageUrl: productCard.dataset.productImage || productCard.querySelector('.product-image')?.src,
            url: productCard.dataset.productUrl || window.location.href,
            quantity: 1
        };

        // Add visual feedback
        addToCartBtn.disabled = true;
        const originalText = addToCartBtn.innerHTML;
        addToCartBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang thêm...';

        // Simulate async operation
        setTimeout(() => {
            const success = window.cart.addItem(product);

            addToCartBtn.disabled = false;
            addToCartBtn.innerHTML = originalText;

            // Add success animation
            if (success) {
                addToCartBtn.classList.add('btn-success');
                setTimeout(() => addToCartBtn.classList.remove('btn-success'), 1000);
            }
        }, 300);
    }
});

// Listen for cart updates
window.addEventListener('cartUpdated', function (e) {
    console.log('Cart updated:', e.detail);
});
