// Visual Search - Drag & Drop Image Search
(function() {
    'use strict';

    // DOM Elements
    const dropZone = document.getElementById('dropZone');
    const dropOverlay = document.getElementById('dropOverlay');
    const fileInput = document.getElementById('fileInput');
    const browseBtn = document.getElementById('browseBtn');
    const imagePreview = document.getElementById('imagePreview');
    const previewImage = document.getElementById('previewImage');
    const removeImageBtn = document.getElementById('removeImageBtn');
    const searchBtn = document.getElementById('searchBtn');
    const imageUrlInput = document.getElementById('imageUrlInput');
    const searchUrlBtn = document.getElementById('searchUrlBtn');
    const loadingState = document.getElementById('loadingState');
    const errorMessage = document.getElementById('errorMessage');
    const errorText = document.getElementById('errorText');
    const closeErrorBtn = document.getElementById('closeErrorBtn');
    const resultsSection = document.getElementById('resultsSection');
    const resultsCount = document.getElementById('resultsCount');
    const resultsGrid = document.getElementById('resultsGrid');
    const noResults = document.getElementById('noResults');

    let selectedFile = null;
    let allResults = [];

    // Initialize
    init();

    function init() {
        setupDragAndDrop();
        setupFileInput();
        setupButtons();
        setupFilters();
    }

    // Drag and Drop Setup
    function setupDragAndDrop() {
        // Prevent default drag behaviors
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, preventDefaults, false);
            document.body.addEventListener(eventName, preventDefaults, false);
        });

        // Highlight drop zone when item is dragged over it
        ['dragenter', 'dragover'].forEach(eventName => {
            dropZone.addEventListener(eventName, highlight, false);
        });

        ['dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, unhighlight, false);
        });

        // Handle dropped files
        dropZone.addEventListener('drop', handleDrop, false);

        // Click to browse
        dropZone.addEventListener('click', () => fileInput.click());
    }

    function preventDefaults(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    function highlight() {
        dropZone.classList.add('drag-over');
    }

    function unhighlight() {
        dropZone.classList.remove('drag-over');
    }

    function handleDrop(e) {
        const dt = e.dataTransfer;
        const files = dt.files;

        if (files.length > 0) {
            handleFile(files[0]);
        }
    }

    // File Input Setup
    function setupFileInput() {
        browseBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            fileInput.click();
        });

        fileInput.addEventListener('change', (e) => {
            if (e.target.files.length > 0) {
                handleFile(e.target.files[0]);
            }
        });
    }

    // Handle File
    function handleFile(file) {
        // Validate file type
        const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
        if (!validTypes.includes(file.type)) {
            showError('Định dạng file không hợp lệ. Vui lòng chọn file JPG, PNG hoặc WebP.');
            return;
        }

        // Validate file size (10MB)
        const maxSize = 10 * 1024 * 1024;
        if (file.size > maxSize) {
            showError('Kích thước file vượt quá 10MB. Vui lòng chọn file nhỏ hơn.');
            return;
        }

        selectedFile = file;

        // Show preview
        const reader = new FileReader();
        reader.onload = (e) => {
            previewImage.src = e.target.result;
            dropZone.style.display = 'none';
            imagePreview.style.display = 'block';
            hideError();
            hideResults();
        };
        reader.readAsDataURL(file);
    }

    // Button Setup
    function setupButtons() {
        removeImageBtn.addEventListener('click', resetUpload);
        searchBtn.addEventListener('click', performImageSearch);
        searchUrlBtn.addEventListener('click', performUrlSearch);
        closeErrorBtn.addEventListener('click', hideError);
    }

    function resetUpload() {
        selectedFile = null;
        fileInput.value = '';
        previewImage.src = '';
        dropZone.style.display = 'block';
        imagePreview.style.display = 'none';
        hideResults();
        hideError();
    }

    // Perform Image Search
    async function performImageSearch() {
        if (!selectedFile) {
            showError('Vui lòng chọn hình ảnh để tìm kiếm.');
            return;
        }

        const formData = new FormData();
        formData.append('file', selectedFile);

        setSearching(true);
        hideError();
        hideResults();

        try {
            const response = await fetch('/api/visualsearch/upload', {
                method: 'POST',
                body: formData
            });

            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.error || 'Có lỗi xảy ra khi tìm kiếm');
            }

            displayResults(data);
        } catch (error) {
            console.error('Search error:', error);
            showError(error.message || 'Không thể kết nối đến server. Vui lòng thử lại.');
        } finally {
            setSearching(false);
        }
    }

    // Perform URL Search
    async function performUrlSearch() {
        const imageUrl = imageUrlInput.value.trim();

        if (!imageUrl) {
            showError('Vui lòng nhập URL hình ảnh.');
            return;
        }

        // Validate URL
        try {
            new URL(imageUrl);
        } catch {
            showError('URL không hợp lệ. Vui lòng nhập URL đầy đủ (bắt đầu với http:// hoặc https://).');
            return;
        }

        setSearching(true);
        hideError();
        hideResults();

        try {
            const response = await fetch('/api/visualsearch/url', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ imageUrl })
            });

            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.error || 'Có lỗi xảy ra khi tìm kiếm');
            }

            displayResults(data);
        } catch (error) {
            console.error('Search error:', error);
            showError(error.message || 'Không thể kết nối đến server. Vui lòng thử lại.');
        } finally {
            setSearching(false);
        }
    }

    // Display Results
    function displayResults(data) {
        allResults = data.results || [];

        if (allResults.length === 0) {
            resultsSection.style.display = 'block';
            resultsGrid.style.display = 'none';
            noResults.style.display = 'block';
            resultsCount.textContent = 'Không tìm thấy kết quả';
            return;
        }

        resultsSection.style.display = 'block';
        resultsGrid.style.display = 'grid';
        noResults.style.display = 'none';
        resultsCount.textContent = `Tìm thấy ${allResults.length} sản phẩm`;

        renderResults(allResults);

        // Scroll to results
        resultsSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    function renderResults(results) {
        resultsGrid.innerHTML = '';

        results.forEach(result => {
            const card = createResultCard(result);
            resultsGrid.appendChild(card);
        });
    }

    function createResultCard(result) {
        const card = document.createElement('div');
        card.className = 'result-card';
        card.dataset.platform = result.platform || 'unknown';

        const platformIcon = getPlatformIcon(result.platform);
        const platformName = getPlatformName(result.platform);
        const price = formatPrice(result.price, result.priceValue);

        card.innerHTML = `
            <img src="${escapeHtml(result.thumbnailUrl || '/images/placeholder.png')}" 
                 alt="${escapeHtml(result.title)}" 
                 class="result-card-image"
                 onerror="this.src='/images/placeholder.png'" />
            <div class="result-card-content">
                <div class="result-platform">
                    ${platformIcon}
                    <span>${platformName}</span>
                </div>
                <h3 class="result-title">${escapeHtml(result.title)}</h3>
                ${price ? `<div class="result-price">${price}</div>` : ''}
                <div class="result-source">${escapeHtml(result.source || result.sourceUrl)}</div>
            </div>
        `;

        card.addEventListener('click', () => {
            window.open(result.sourceUrl, '_blank');
        });

        return card;
    }

    // Platform Filters
    function setupFilters() {
        const filterBtns = document.querySelectorAll('.filter-btn');

        filterBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                // Update active state
                filterBtns.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');

                // Filter results
                const platform = btn.dataset.platform;
                filterResults(platform);
            });
        });
    }

    function filterResults(platform) {
        if (platform === 'all') {
            renderResults(allResults);
            resultsCount.textContent = `Tìm thấy ${allResults.length} sản phẩm`;
        } else {
            const filtered = allResults.filter(r => r.platform === platform);
            renderResults(filtered);
            resultsCount.textContent = `Tìm thấy ${filtered.length} sản phẩm từ ${getPlatformName(platform)}`;
        }
    }

    // Helper Functions
    function setSearching(isSearching) {
        if (isSearching) {
            searchBtn.disabled = true;
            searchBtn.querySelector('.btn-text').style.display = 'none';
            searchBtn.querySelector('.btn-loading').style.display = 'inline-block';
            searchUrlBtn.disabled = true;
            loadingState.style.display = 'block';
        } else {
            searchBtn.disabled = false;
            searchBtn.querySelector('.btn-text').style.display = 'inline';
            searchBtn.querySelector('.btn-loading').style.display = 'none';
            searchUrlBtn.disabled = false;
            loadingState.style.display = 'none';
        }
    }

    function showError(message) {
        errorText.textContent = message;
        errorMessage.style.display = 'flex';
    }

    function hideError() {
        errorMessage.style.display = 'none';
    }

    function hideResults() {
        resultsSection.style.display = 'none';
    }

    function getPlatformIcon(platform) {
        const icons = {
            'shopee': '<img src="/images/platforms/shopee-icon.png" alt="Shopee" />',
            'tiki': '<img src="/images/platforms/tiki-icon.png" alt="Tiki" />',
            'lazada': '<img src="/images/platforms/lazada-icon.png" alt="Lazada" />'
        };
        return icons[platform] || '';
    }

    function getPlatformName(platform) {
        const names = {
            'shopee': 'Shopee',
            'tiki': 'Tiki',
            'lazada': 'Lazada'
        };
        return names[platform] || 'Unknown';
    }

    function formatPrice(priceText, priceValue) {
        if (priceValue) {
            return new Intl.NumberFormat('vi-VN', {
                style: 'currency',
                currency: 'VND'
            }).format(priceValue);
        }
        return priceText || '';
    }

    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text ? text.replace(/[&<>"']/g, m => map[m]) : '';
    }
})();
