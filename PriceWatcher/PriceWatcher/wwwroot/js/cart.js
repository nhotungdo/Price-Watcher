class CartClient {
    constructor() {
        this.items = [];
        this.summary = { subtotal: 0, discount: 0, total: 0, itemCount: 0 };
        this.readyPromise = this.refresh();
    }

    async refresh() {
        try {
            const res = await fetch('/api/cart', { credentials: 'same-origin' });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const data = await res.json();
            this.consume(data, { silent: true });
        } catch (error) {
            console.error('Unable to load cart:', error);
        } finally {
            window.dispatchEvent(new CustomEvent('cart:ready'));
        }
    }

    async addItem(product) {
        await this.readyPromise;
        const payload = {
            productId: product.productId,
            platformId: product.platformId,
            platformName: product.platformName,
            name: product.name,
            price: product.price,
            originalPrice: product.originalPrice,
            imageUrl: product.imageUrl,
            productUrl: product.url,
            quantity: product.quantity || 1
        };
        return this.send('/api/cart/items', 'POST', payload, `Đã thêm vào giỏ hàng: ${product.name}`);
    }

    async updateQuantity(productId, platformId, quantity) {
        await this.readyPromise;
        const route = this.buildItemRoute(productId, platformId);
        return this.send(route, 'PATCH', { quantity }, 'Đã cập nhật số lượng');
    }

    async removeItem(productId, platformId) {
        await this.readyPromise;
        const route = this.buildItemRoute(productId, platformId);
        return this.send(route, 'DELETE', null, 'Đã xóa sản phẩm');
    }

    buildItemRoute(productId, platformId) {
        const params = new URLSearchParams();
        if (typeof platformId === 'number' && !Number.isNaN(platformId)) {
            params.set('platformId', platformId);
        }
        const qs = params.toString();
        return qs ? `/api/cart/items/${productId}?${qs}` : `/api/cart/items/${productId}`;
    }

    async send(url, method, body, successMessage) {
        try {
            const res = await fetch(url, {
                method,
                headers: body ? { 'Content-Type': 'application/json' } : undefined,
                body: body ? JSON.stringify(body) : null,
                credentials: 'same-origin'
            });
            if (!res.ok) {
                const message = await res.text();
                throw new Error(message || `HTTP ${res.status}`);
            }
            const data = await res.json();
            this.consume(data);
            if (successMessage) {
                this.showNotification(successMessage, 'success');
            }
            return data;
        } catch (error) {
            console.error('Cart action failed:', error);
            this.showNotification('Không thể cập nhật giỏ hàng', 'error');
            throw error;
        }
    }

    consume(data, options = { silent: false }) {
        if (!data) return;
        this.items = data.items || [];
        this.summary = data.summary || { subtotal: 0, discount: 0, total: 0, itemCount: 0 };
        this.updateCartBadge();
        if (!options.silent) {
            window.dispatchEvent(new CustomEvent('cart:updated', {
                detail: { items: this.items, summary: this.summary }
            }));
        }
    }

    updateCartBadge() {
        const badge = document.getElementById('cartCount');
        if (badge) {
            const count = this.summary.itemCount || 0;
            badge.textContent = count;
            badge.style.display = count > 0 ? 'flex' : 'none';
        }
    }

    showNotification(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `cart-toast cart-toast-${type}`;
        toast.innerHTML = `
            <div class="cart-toast-content">
                <span class="cart-toast-icon">${this.getToastIcon(type)}</span>
                <span class="cart-toast-message">${message}</span>
            </div>
        `;
        document.body.appendChild(toast);
        setTimeout(() => toast.classList.add('show'), 10);
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    getToastIcon(type) {
        const icons = { success: '✓', error: '✕', warning: '⚠', info: 'ℹ' };
        return icons[type] || icons.info;
    }
}

window.cartClient = new CartClient();

document.addEventListener('click', function (e) {
    const addToCartBtn = e.target.closest('.add-to-cart-btn');
    if (!addToCartBtn) return;
    e.preventDefault();

    const productCard = addToCartBtn.closest('[data-product-id]');
    if (!productCard) return;

    const product = {
        productId: parseInt(productCard.dataset.productId),
        platformId: productCard.dataset.platformId ? parseInt(productCard.dataset.platformId) : null,
        platformName: productCard.dataset.platformName || 'Tiki',
        name: productCard.dataset.productName || productCard.querySelector('.product-name')?.textContent,
        price: parseFloat(productCard.dataset.productPrice || productCard.querySelector('.product-price')?.dataset.price || '0'),
        originalPrice: productCard.dataset.productOriginalPrice ? parseFloat(productCard.dataset.productOriginalPrice) : null,
        imageUrl: productCard.dataset.productImage || productCard.querySelector('.product-image')?.src,
        url: productCard.dataset.productUrl || window.location.href,
        quantity: 1
    };

    addToCartBtn.disabled = true;
    const originalText = addToCartBtn.innerHTML;
    addToCartBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang thêm...';

    window.cartClient.addItem(product)
        .then(() => {
            addToCartBtn.classList.add('btn-success');
            setTimeout(() => addToCartBtn.classList.remove('btn-success'), 1000);
        })
        .finally(() => {
            addToCartBtn.disabled = false;
            addToCartBtn.innerHTML = originalText;
        });
});
