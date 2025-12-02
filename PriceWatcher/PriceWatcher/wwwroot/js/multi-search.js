// Multi-Platform Search JavaScript
(function() {
    'use strict';

    let currentFilters = {
        minPrice: null,
        maxPrice: null,
        minRating: null,
        freeShipping: false,
        officialStore: false
    };

    let currentSort = '';
    let currentKeyword = '';

    // Initialize on page load
    document.addEventListener('DOMContentLoaded', function() {
        initializeEventListeners();
        
        // Auto-search if keyword is provided
        const urlParams = new URLSearchParams(window.location.search);
        const keyword = urlParams.get('keyword') || document.getElementById('searchKeyword').value;
        if (keyword) {
            currentKeyword = keyword;
            performSearch();
        }
    });

    function initializeEventListeners() {
        // Search button
        document.getElementById('btnSearch').addEventListener('click', function() {
            currentKeyword = document.getElementById('searchKeyword').value.trim();
            if (currentKeyword) {
                performSearch();
            }
        });

        // Enter key in search box
        document.getElementById('searchKeyword').addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                currentKeyword = this.value.trim();
                if (currentKeyword) {
                    performSearch();
                }
            }
        });

        // Price range filters
        document.querySelectorAll('.price-range').forEach(item => {
            item.addEventListener('click', function(e) {
                e.preventDefault();
                currentFilters.minPrice = this.dataset.min || null;
                currentFilters.maxPrice = this.dataset.max || null;
                performSearch();
            });
        });

        // Sort options
        document.querySelectorAll('.sort-option').forEach(item => {
            item.addEventListener('click', function(e) {
                e.preventDefault();
                currentSort = this.dataset.sort;
                performSearch();
            });
        });

        // Filter checkboxes
        document.getElementById('filterFreeShip').addEventListener('change', function() {
            currentFilters.freeShipping = this.checked;
            performSearch();
        });

        document.getElementById('filterOfficial').addEventListener('change', function() {
            currentFilters.officialStore = this.checked;
            performSearch();
        });
    }

    async function performSearch() {
        if (!currentKeyword) return;

        // Get selected platforms
        const platforms = [];
        if (document.getElementById('platform-shopee').checked) platforms.push('shopee');
        if (document.getElementById('platform-lazada').checked) platforms.push('lazada');
        if (document.getElementById('platform-tiki').checked) platforms.push('tiki');

        if (platforms.length === 0) {
            alert('Vui lòng chọn ít nhất một sàn thương mại');
            return;
        }

        // Show loading
        showLoading();

        try {
            const requestBody = {
                keyword: currentKeyword,
                platforms: platforms,
                limit: 30,
                offset: 0,
                sortBy: currentSort,
                filters: {
                    minPrice: currentFilters.minPrice ? parseFloat(currentFilters.minPrice) : null,
                    maxPrice: currentFilters.maxPrice ? parseFloat(currentFilters.maxPrice) : null,
                    minRating: currentFilters.minRating,
                    freeShipping: currentFilters.freeShipping || null,
                    officialStore: currentFilters.officialStore || null
                }
            };

            const response = await fetch('/api/MultiPlatformSearch/search', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestBody)
            });

            if (!response.ok) {
                throw new Error('Search failed');
            }

            const data = await response.json();
            displayResults(data);

            // Load price comparison if results found
            if (data.totalResults > 0) {
                loadPriceComparison();
            }

        } catch (error) {
            console.error('Search error:', error);
            showError('Có lỗi xảy ra khi tìm kiếm. Vui lòng thử lại.');
        } finally {
            hideLoading();
        }
    }

    function displayResults(data) {
        const resultsContainer = document.getElementById('searchResults');
        const noResults = document.getElementById('noResults');
        const resultCount = document.getElementById('resultCount');

        if (!data.products || data.products.length === 0) {
            resultsContainer.innerHTML = '';
            noResults.style.display = 'block';
            resultCount.textContent = '';
            return;
        }

        noResults.style.display = 'none';
        resultCount.textContent = `Tìm thấy ${data.totalResults} sản phẩm`;

        // Build HTML for products
        let html = '';
        data.products.forEach(product => {
            html += createProductCard(product);
        });

        resultsContainer.innerHTML = html;

        // Attach cart event listeners
        attachCartEventListeners();

        // Show platform statistics
        displayPlatformStats(data.resultsByPlatform);
    }

    function attachCartEventListeners() {
        document.querySelectorAll('.add-to-cart-btn').forEach(btn => {
            btn.addEventListener('click', async function() {
                const productData = {
                    productId: parseInt(this.dataset.productId) || 0,
                    name: this.dataset.productName,
                    price: parseFloat(this.dataset.price),
                    originalPrice: this.dataset.originalPrice ? parseFloat(this.dataset.originalPrice) : null,
                    platformName: this.dataset.platform,
                    imageUrl: this.dataset.image,
                    productUrl: this.dataset.url,
                    quantity: 1
                };

                await addToCart(productData, this);
            });
        });
    }

    async function addToCart(productData, buttonElement) {
        const originalHtml = buttonElement.innerHTML;
        buttonElement.disabled = true;
        buttonElement.innerHTML = '<span class="spinner-border spinner-border-sm"></span>';

        try {
            const response = await fetch('/api/cart/items', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(productData)
            });

            if (!response.ok) {
                throw new Error('Failed to add to cart');
            }

            const cart = await response.json();
            
            // Show success feedback
            buttonElement.innerHTML = '<i class="bi bi-check-lg"></i> Đã thêm';
            buttonElement.classList.remove('btn-outline-primary');
            buttonElement.classList.add('btn-success');
            
            // Update cart count
            if (window.cartUtils) {
                window.cartUtils.updateCartBadge(cart.summary.itemCount);
            }
            
            // Show toast notification
            showToast('Đã thêm vào giỏ hàng', `${productData.name} đã được thêm vào giỏ hàng`, 'success');

            // Reset button after 2 seconds
            setTimeout(() => {
                buttonElement.innerHTML = originalHtml;
                buttonElement.classList.remove('btn-success');
                buttonElement.classList.add('btn-outline-primary');
                buttonElement.disabled = false;
            }, 2000);

        } catch (error) {
            console.error('Add to cart error:', error);
            buttonElement.innerHTML = originalHtml;
            buttonElement.disabled = false;
            showToast('Lỗi', 'Không thể thêm sản phẩm vào giỏ hàng', 'error');
        }
    }

    function showToast(title, message, type = 'info') {
        // Create toast container if it doesn't exist
        let toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'toastContainer';
            toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        const toastId = 'toast-' + Date.now();
        const bgClass = type === 'success' ? 'bg-success' : type === 'error' ? 'bg-danger' : 'bg-info';
        const icon = type === 'success' ? 'check-circle-fill' : type === 'error' ? 'exclamation-triangle-fill' : 'info-circle-fill';

        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert">
                <div class="d-flex">
                    <div class="toast-body">
                        <i class="bi bi-${icon} me-2"></i>
                        <strong>${title}</strong>
                        <div class="small">${message}</div>
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, { delay: 3000 });
        toast.show();

        // Remove toast element after it's hidden
        toastElement.addEventListener('hidden.bs.toast', function() {
            toastElement.remove();
        });
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    function createProductCard(product) {
        const discount = product.discountPercent 
            ? `<span class="badge bg-danger">${Math.round(product.discountPercent * 100)}%</span>` 
            : '';
        
        const originalPrice = product.priceBeforeDiscount 
            ? `<del class="text-muted small">${formatPrice(product.priceBeforeDiscount)}</del>` 
            : '';

        const rating = product.rating 
            ? `<span class="text-warning">★ ${product.rating.toFixed(1)}</span>` 
            : '';

        const sold = product.soldCount 
            ? `<span class="text-muted small">Đã bán ${formatNumber(product.soldCount)}</span>` 
            : '';

        const freeShip = product.isFreeShip 
            ? '<span class="badge bg-success">Freeship</span>' 
            : '';

        const official = product.isOfficialStore 
            ? '<span class="badge bg-primary">Chính hãng</span>' 
            : '';

        const platformBadge = getPlatformBadge(product.platform);

        return `
            <div class="col-md-4 col-lg-3">
                <div class="card h-100 product-card">
                    <div class="position-relative">
                        <img src="${product.productImage || '/images/no-image.png'}" 
                             class="card-img-top" 
                             alt="${product.productName}"
                             style="height: 200px; object-fit: cover;">
                        <div class="position-absolute top-0 end-0 m-2">
                            ${discount}
                        </div>
                        <div class="position-absolute top-0 start-0 m-2">
                            ${platformBadge}
                        </div>
                    </div>
                    <div class="card-body d-flex flex-column">
                        <h6 class="card-title text-truncate-2" title="${product.productName}">
                            ${product.productName}
                        </h6>
                        <div class="mt-auto">
                            <div class="d-flex align-items-center gap-2 mb-2">
                                <span class="text-danger fw-bold fs-5">${formatPrice(product.price)}</span>
                                ${originalPrice}
                            </div>
                            <div class="d-flex justify-content-between align-items-center mb-2">
                                <div>${rating}</div>
                                <div>${sold}</div>
                            </div>
                            <div class="mb-2">
                                <small class="text-muted">${product.shopName}</small>
                            </div>
                            <div class="d-flex gap-1 flex-wrap mb-2">
                                ${freeShip}
                                ${official}
                            </div>
                            <div class="d-flex gap-2">
                                <button class="btn btn-outline-primary btn-sm flex-grow-1 add-to-cart-btn"
                                        data-product-id="${product.productId || 0}"
                                        data-product-name="${escapeHtml(product.productName)}"
                                        data-price="${product.price}"
                                        data-original-price="${product.priceBeforeDiscount || ''}"
                                        data-platform="${product.platform}"
                                        data-image="${product.productImage || ''}"
                                        data-url="${product.productUrl}">
                                    <i class="bi bi-cart-plus"></i> Thêm
                                </button>
                                <a href="${product.productUrl}" 
                                   target="_blank" 
                                   class="btn btn-primary btn-sm flex-grow-1">
                                    Xem
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    function getPlatformBadge(platform) {
        const badges = {
            'shopee': '<span class="badge" style="background-color: #ee4d2d;">Shopee</span>',
            'lazada': '<span class="badge" style="background-color: #0f146d;">Lazada</span>',
            'tiki': '<span class="badge" style="background-color: #1a94ff;">Tiki</span>'
        };
        return badges[platform.toLowerCase()] || `<span class="badge bg-secondary">${platform}</span>`;
    }

    function displayPlatformStats(stats) {
        console.log('Platform statistics:', stats);
        // Could display a summary of results per platform
    }

    async function loadPriceComparison() {
        try {
            const response = await fetch(`/api/MultiPlatformSearch/compare?keyword=${encodeURIComponent(currentKeyword)}`);
            if (!response.ok) return;

            const comparisons = await response.json();
            if (comparisons && comparisons.length > 0) {
                displayPriceComparison(comparisons);
            }
        } catch (error) {
            console.error('Price comparison error:', error);
        }
    }

    function displayPriceComparison(comparisons) {
        const section = document.getElementById('priceComparisonSection');
        const container = document.getElementById('priceComparisonResults');

        let html = '';
        comparisons.forEach(comparison => {
            html += `
                <div class="card mb-3">
                    <div class="card-body">
                        <h6 class="card-title">${comparison.productName}</h6>
                        <div class="table-responsive">
                            <table class="table table-sm">
                                <thead>
                                    <tr>
                                        <th>Sàn</th>
                                        <th>Giá</th>
                                        <th>Phí ship</th>
                                        <th>Tổng</th>
                                        <th>Đánh giá</th>
                                        <th></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    ${comparison.prices.map(p => `
                                        <tr ${p.totalCost === comparison.lowestPrice ? 'class="table-success"' : ''}>
                                            <td>${getPlatformBadge(p.platform)}</td>
                                            <td>${formatPrice(p.price)}</td>
                                            <td>${formatPrice(p.shippingCost)}</td>
                                            <td class="fw-bold">${formatPrice(p.totalCost)}</td>
                                            <td>${p.rating ? '★ ' + p.rating.toFixed(1) : '-'}</td>
                                            <td>
                                                <a href="${p.productUrl}" target="_blank" class="btn btn-sm btn-outline-primary">
                                                    Xem
                                                </a>
                                            </td>
                                        </tr>
                                    `).join('')}
                                </tbody>
                            </table>
                        </div>
                        <div class="text-muted small">
                            💰 Giá thấp nhất: <strong>${formatPrice(comparison.lowestPrice)}</strong> 
                            | Giá cao nhất: ${formatPrice(comparison.highestPrice)} 
                            | Trung bình: ${formatPrice(comparison.averagePrice)}
                        </div>
                    </div>
                </div>
            `;
        });

        container.innerHTML = html;
        section.style.display = 'block';
    }

    function showLoading() {
        document.getElementById('loadingIndicator').style.display = 'block';
        document.getElementById('searchResults').innerHTML = '';
        document.getElementById('noResults').style.display = 'none';
        document.getElementById('priceComparisonSection').style.display = 'none';
    }

    function hideLoading() {
        document.getElementById('loadingIndicator').style.display = 'none';
    }

    function showError(message) {
        alert(message);
    }

    function formatPrice(price) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(price);
    }

    function formatNumber(num) {
        if (num >= 1000) {
            return (num / 1000).toFixed(1) + 'k';
        }
        return num.toString();
    }

})();
