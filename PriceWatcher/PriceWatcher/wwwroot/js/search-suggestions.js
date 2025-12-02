// Advanced Search Suggestions with Debouncing and Real-time Updates
(function() {
    'use strict';

    const DEBOUNCE_DELAY = 300; // ms
    const MIN_QUERY_LENGTH = 1;
    const MAX_SUGGESTIONS = 10;

    let debounceTimer = null;
    let currentQuery = '';
    let isDropdownOpen = false;

    // DOM Elements
    const searchInput = document.getElementById('headerSearch');
    const searchBtn = document.getElementById('searchSubmitBtn');
    const dropdown = document.getElementById('searchAutocomplete');
    const autocompleteContent = dropdown?.querySelector('.autocomplete-content');
    const previewContent = dropdown?.querySelector('.preview-content');

    // Initialize
    document.addEventListener('DOMContentLoaded', function() {
        if (!searchInput || !dropdown) return;

        initializeSearchListeners();
        loadTrendingOnFocus();
    });

    function initializeSearchListeners() {
        // Input event with debouncing
        searchInput.addEventListener('input', function(e) {
            const query = e.target.value.trim();
            currentQuery = query;

            clearTimeout(debounceTimer);
            
            if (query.length === 0) {
                showTrendingAndRecent();
                return;
            }

            if (query.length < MIN_QUERY_LENGTH) {
                hideDropdown();
                return;
            }

            // Debounce the search
            debounceTimer = setTimeout(() => {
                fetchSuggestions(query);
            }, DEBOUNCE_DELAY);
        });

        // Focus event - show trending/recent
        searchInput.addEventListener('focus', function() {
            if (searchInput.value.trim().length === 0) {
                showTrendingAndRecent();
            } else {
                fetchSuggestions(searchInput.value.trim());
            }
        });

        // Click outside to close
        document.addEventListener('click', function(e) {
            if (!searchInput.contains(e.target) && !dropdown.contains(e.target)) {
                hideDropdown();
            }
        });

        // Enter key to search
        searchInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                performSearch(searchInput.value.trim());
            }
        });

        // Search button click
        if (searchBtn) {
            searchBtn.addEventListener('click', function() {
                performSearch(searchInput.value.trim());
            });
        }

        // Keyboard navigation
        searchInput.addEventListener('keydown', handleKeyboardNavigation);
    }

    async function fetchSuggestions(query) {
        if (!query) return;

        try {
            const response = await fetch(`/api/search/suggestions?q=${encodeURIComponent(query)}&limit=${MAX_SUGGESTIONS}`);
            
            if (!response.ok) {
                console.error('Failed to fetch suggestions:', response.status);
                return;
            }

            const data = await response.json();
            
            if (data.success) {
                renderSuggestions(data);
            }
        } catch (error) {
            console.error('Error fetching suggestions:', error);
        }
    }

    async function showTrendingAndRecent() {
        try {
            const response = await fetch('/api/search/suggestions?q=&limit=10');
            
            if (!response.ok) return;

            const data = await response.json();
            
            if (data.success) {
                renderTrendingAndRecent(data);
            }
        } catch (error) {
            console.error('Error loading trending:', error);
        }
    }

    function renderSuggestions(data) {
        if (!autocompleteContent || !previewContent) return;

        // Clear previous content
        autocompleteContent.innerHTML = '';
        previewContent.innerHTML = '';

        const { suggestions, detectedType } = data;

        if (!suggestions || suggestions.length === 0) {
            autocompleteContent.innerHTML = '<div class="suggestion-empty">Không tìm thấy kết quả</div>';
            showDropdown();
            return;
        }

        // Render suggestions based on type
        if (detectedType === 'url') {
            renderUrlSuggestion(suggestions[0]);
        } else {
            renderTextSuggestions(suggestions);
        }

        // Add trending keywords if available
        if (data.trendingKeywords && data.trendingKeywords.length > 0) {
            renderTrendingSection(data.trendingKeywords);
        }

        showDropdown();
    }

    function renderTextSuggestions(suggestions) {
        const html = suggestions.map(suggestion => {
            const icon = getSuggestionIcon(suggestion.type);
            const priceHtml = suggestion.price ? 
                `<span class="suggestion-price">${formatPrice(suggestion.price)}</span>` : '';
            const imageHtml = suggestion.imageUrl ? 
                `<img src="${suggestion.imageUrl}" alt="" class="suggestion-image">` : '';
            const platformBadge = suggestion.platform ? 
                `<span class="suggestion-platform">${suggestion.platform}</span>` : '';

            return `
                <div class="suggestion-item" data-url="${suggestion.url || ''}" data-text="${escapeHtml(suggestion.text)}">
                    ${imageHtml}
                    <div class="suggestion-icon">${icon}</div>
                    <div class="suggestion-content">
                        <div class="suggestion-text">${highlightQuery(suggestion.text, currentQuery)}</div>
                        ${suggestion.secondaryText ? `<div class="suggestion-secondary">${suggestion.secondaryText}</div>` : ''}
                    </div>
                    ${platformBadge}
                    ${priceHtml}
                    <div class="suggestion-arrow">→</div>
                </div>
            `;
        }).join('');

        autocompleteContent.innerHTML = html;

        // Add click handlers
        autocompleteContent.querySelectorAll('.suggestion-item').forEach(item => {
            item.addEventListener('click', function() {
                const url = this.dataset.url;
                const text = this.dataset.text;
                
                if (url) {
                    recordSearch(text);
                    window.location.href = url;
                } else {
                    searchInput.value = text;
                    performSearch(text);
                }
            });
        });
    }

    function renderUrlSuggestion(suggestion) {
        autocompleteContent.innerHTML = `
            <div class="suggestion-url-detected">
                <div class="suggestion-icon">🔗</div>
                <div class="suggestion-content">
                    <div class="suggestion-text">${suggestion.text}</div>
                    <div class="suggestion-secondary">${suggestion.secondaryText}</div>
                </div>
                <button class="btn btn-primary btn-sm" onclick="window.location.href='${suggestion.url}'">
                    Tìm kiếm
                </button>
            </div>
        `;
    }

    function renderTrendingAndRecent(data) {
        if (!autocompleteContent) return;

        let html = '';

        // Recent searches
        if (data.recentSearches && data.recentSearches.length > 0) {
            html += '<div class="suggestion-section">';
            html += '<div class="suggestion-section-title">🕐 Tìm kiếm gần đây</div>';
            html += data.recentSearches.map(item => `
                <div class="suggestion-item" data-url="${item.url}" data-text="${escapeHtml(item.text)}">
                    <div class="suggestion-icon">🔍</div>
                    <div class="suggestion-content">
                        <div class="suggestion-text">${escapeHtml(item.text)}</div>
                    </div>
                    <div class="suggestion-arrow">→</div>
                </div>
            `).join('');
            html += '</div>';
        }

        // Trending keywords
        if (data.trendingKeywords && data.trendingKeywords.length > 0) {
            html += '<div class="suggestion-section">';
            html += '<div class="suggestion-section-title">🔥 Xu hướng tìm kiếm</div>';
            html += data.trendingKeywords.map(item => `
                <div class="suggestion-item" data-url="${item.url}" data-text="${escapeHtml(item.text)}">
                    <div class="suggestion-icon">📈</div>
                    <div class="suggestion-content">
                        <div class="suggestion-text">${escapeHtml(item.text)}</div>
                        <div class="suggestion-secondary">${item.secondaryText}</div>
                    </div>
                    <div class="suggestion-arrow">→</div>
                </div>
            `).join('');
            html += '</div>';
        }

        if (html) {
            autocompleteContent.innerHTML = html;
            
            // Add click handlers
            autocompleteContent.querySelectorAll('.suggestion-item').forEach(item => {
                item.addEventListener('click', function() {
                    const url = this.dataset.url;
                    const text = this.dataset.text;
                    
                    if (url) {
                        recordSearch(text);
                        window.location.href = url;
                    }
                });
            });

            showDropdown();
        }
    }

    function renderTrendingSection(trending) {
        if (!previewContent) return;

        const html = `
            <div class="trending-section">
                <div class="trending-title">🔥 Xu hướng</div>
                <div class="trending-tags">
                    ${trending.map(item => `
                        <span class="trending-tag" data-keyword="${escapeHtml(item.text)}">
                            ${escapeHtml(item.text)}
                        </span>
                    `).join('')}
                </div>
            </div>
        `;

        previewContent.innerHTML = html;

        // Add click handlers
        previewContent.querySelectorAll('.trending-tag').forEach(tag => {
            tag.addEventListener('click', function() {
                const keyword = this.dataset.keyword;
                searchInput.value = keyword;
                performSearch(keyword);
            });
        });
    }

    function performSearch(query) {
        if (!query) return;

        recordSearch(query);
        hideDropdown();

        // Redirect to search page
        window.location.href = `/MultiSearch?keyword=${encodeURIComponent(query)}`;
    }

    async function recordSearch(keyword) {
        if (!keyword) return;

        try {
            await fetch('/api/search/record', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ keyword })
            });
        } catch (error) {
            console.error('Error recording search:', error);
        }
    }

    function loadTrendingOnFocus() {
        // Pre-load trending keywords for faster display
        fetch('/api/search/trending?limit=5')
            .then(response => response.json())
            .then(data => {
                if (data.success && data.trending) {
                    // Cache trending data
                    window.cachedTrending = data.trending;
                }
            })
            .catch(error => console.error('Error pre-loading trending:', error));
    }

    function handleKeyboardNavigation(e) {
        if (!isDropdownOpen) return;

        const items = autocompleteContent?.querySelectorAll('.suggestion-item');
        if (!items || items.length === 0) return;

        const currentIndex = Array.from(items).findIndex(item => item.classList.contains('active'));

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            const nextIndex = currentIndex < items.length - 1 ? currentIndex + 1 : 0;
            setActiveItem(items, nextIndex);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            const prevIndex = currentIndex > 0 ? currentIndex - 1 : items.length - 1;
            setActiveItem(items, prevIndex);
        } else if (e.key === 'Enter' && currentIndex >= 0) {
            e.preventDefault();
            items[currentIndex].click();
        }
    }

    function setActiveItem(items, index) {
        items.forEach((item, i) => {
            item.classList.toggle('active', i === index);
        });
        items[index].scrollIntoView({ block: 'nearest' });
    }

    function showDropdown() {
        if (dropdown) {
            dropdown.style.display = 'block';
            isDropdownOpen = true;
        }
    }

    function hideDropdown() {
        if (dropdown) {
            dropdown.style.display = 'none';
            isDropdownOpen = false;
        }
    }

    function getSuggestionIcon(type) {
        const icons = {
            'product': '📦',
            'keyword': '🔍',
            'category': '📂',
            'history': '🕐',
            'trending': '🔥',
            'url': '🔗'
        };
        return icons[type] || '🔍';
    }

    function highlightQuery(text, query) {
        if (!query) return escapeHtml(text);
        
        const escapedText = escapeHtml(text);
        const escapedQuery = escapeHtml(query);
        const regex = new RegExp(`(${escapedQuery})`, 'gi');
        
        return escapedText.replace(regex, '<strong>$1</strong>');
    }

    function formatPrice(price) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(price);
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // Export for external use
    window.searchSuggestions = {
        performSearch,
        recordSearch
    };

})();
