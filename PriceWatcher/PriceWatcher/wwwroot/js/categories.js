let selectedCategoryId = null;
let allCategories = [];

document.addEventListener('DOMContentLoaded', function() {
    loadCategories();
});

async function loadCategories() {
    try {
        const response = await fetch('/api/category');
        if (!response.ok) throw new Error('Failed to load categories');
        
        allCategories = await response.json();
        renderCategories(allCategories);
        populateParentCategoryDropdowns();
    } catch (error) {
        console.error('Error loading categories:', error);
        showError('Failed to load categories');
    }
}

function renderCategories(categories, parentElement = null, level = 0) {
    const container = parentElement || document.getElementById('categoryList');
    
    if (!parentElement) {
        container.innerHTML = '';
    }

    categories.forEach(category => {
        const item = document.createElement('a');
        item.href = '#';
        item.className = 'list-group-item list-group-item-action';
        item.style.paddingLeft = `${20 + (level * 20)}px`;
        item.onclick = (e) => {
            e.preventDefault();
            selectCategory(category.categoryId);
        };

        const icon = category.iconUrl 
            ? `<img src="${category.iconUrl}" alt="" style="width: 20px; height: 20px; margin-right: 8px;">`
            : `<i class="bi bi-folder${level === 0 ? '' : '-fill'}" style="margin-right: 8px;"></i>`;

        item.innerHTML = `
            ${icon}
            <span>${category.categoryName}</span>
            ${category.subCategories && category.subCategories.length > 0 
                ? `<span class="badge bg-secondary float-end">${category.subCategories.length}</span>` 
                : ''}
        `;

        container.appendChild(item);

        if (category.subCategories && category.subCategories.length > 0) {
            renderCategories(category.subCategories, container, level + 1);
        }
    });
}

async function selectCategory(categoryId) {
    selectedCategoryId = categoryId;
    
    // Highlight selected category
    document.querySelectorAll('#categoryList .list-group-item').forEach(item => {
        item.classList.remove('active');
    });
    event.target.closest('.list-group-item').classList.add('active');

    // Load category details
    try {
        const response = await fetch(`/api/category/${categoryId}`);
        if (!response.ok) throw new Error('Failed to load category');
        
        const category = await response.json();
        document.getElementById('categoryTitle').textContent = category.categoryName;
        document.getElementById('categoryActions').style.display = 'block';

        // Load products in this category
        await loadCategoryProducts(categoryId);
    } catch (error) {
        console.error('Error loading category:', error);
        showError('Failed to load category details');
    }
}

async function loadCategoryProducts(categoryId) {
    const container = document.getElementById('productList');
    container.innerHTML = '<div class="text-center p-4"><div class="spinner-border text-primary"></div></div>';

    try {
        const response = await fetch(`/api/category/${categoryId}/products`);
        if (!response.ok) throw new Error('Failed to load products');
        
        const products = await response.json();

        if (products.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted p-5">
                    <i class="bi bi-inbox" style="font-size: 3rem;"></i>
                    <p class="mt-3">No products in this category yet</p>
                </div>
            `;
            return;
        }

        container.innerHTML = '<div class="row g-3"></div>';
        const row = container.querySelector('.row');

        products.forEach(product => {
            const col = document.createElement('div');
            col.className = 'col-md-6';
            col.innerHTML = `
                <div class="card h-100">
                    <div class="row g-0">
                        <div class="col-4">
                            <img src="${product.imageUrl || '/images/placeholder.png'}" 
                                 class="img-fluid rounded-start" 
                                 alt="${product.productName}"
                                 style="height: 120px; object-fit: cover;">
                        </div>
                        <div class="col-8">
                            <div class="card-body p-2">
                                <h6 class="card-title text-truncate" title="${product.productName}">
                                    ${product.productName}
                                </h6>
                                <p class="card-text mb-1">
                                    <span class="text-danger fw-bold">${formatPrice(product.currentPrice)}</span>
                                    ${product.originalPrice ? `<small class="text-muted text-decoration-line-through ms-1">${formatPrice(product.originalPrice)}</small>` : ''}
                                </p>
                                <p class="card-text">
                                    <small class="text-muted">
                                        <span class="badge bg-info">${product.platform}</span>
                                        ${product.shopName ? `<span class="ms-1">${product.shopName}</span>` : ''}
                                    </small>
                                </p>
                                <button class="btn btn-sm btn-outline-danger" onclick="removeFromCategory(${product.productId})">
                                    <i class="bi bi-x"></i> Remove
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            row.appendChild(col);
        });
    } catch (error) {
        console.error('Error loading products:', error);
        container.innerHTML = `
            <div class="alert alert-danger">
                Failed to load products. Please try again.
            </div>
        `;
    }
}

async function createCategory() {
    const name = document.getElementById('categoryName').value.trim();
    if (!name) {
        alert('Please enter a category name');
        return;
    }

    const parentId = document.getElementById('parentCategory').value;
    const iconUrl = document.getElementById('iconUrl').value.trim();

    try {
        const response = await fetch('/api/category', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                categoryName: name,
                parentCategoryId: parentId ? parseInt(parentId) : null,
                iconUrl: iconUrl || null
            })
        });

        if (!response.ok) throw new Error('Failed to create category');

        const modal = bootstrap.Modal.getInstance(document.getElementById('createCategoryModal'));
        modal.hide();
        document.getElementById('createCategoryForm').reset();

        await loadCategories();
        showSuccess('Category created successfully');
    } catch (error) {
        console.error('Error creating category:', error);
        showError('Failed to create category');
    }
}

function editCategory() {
    if (!selectedCategoryId) return;

    const category = findCategoryById(allCategories, selectedCategoryId);
    if (!category) return;

    document.getElementById('editCategoryId').value = category.categoryId;
    document.getElementById('editCategoryName').value = category.categoryName;
    document.getElementById('editParentCategory').value = category.parentCategoryId || '0';
    document.getElementById('editIconUrl').value = category.iconUrl || '';

    const modal = new bootstrap.Modal(document.getElementById('editCategoryModal'));
    modal.show();
}

async function updateCategory() {
    const categoryId = document.getElementById('editCategoryId').value;
    const name = document.getElementById('editCategoryName').value.trim();
    const parentId = document.getElementById('editParentCategory').value;
    const iconUrl = document.getElementById('editIconUrl').value.trim();

    try {
        const response = await fetch(`/api/category/${categoryId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                categoryName: name,
                parentCategoryId: parentId === '0' ? null : parseInt(parentId),
                iconUrl: iconUrl || null
            })
        });

        if (!response.ok) throw new Error('Failed to update category');

        const modal = bootstrap.Modal.getInstance(document.getElementById('editCategoryModal'));
        modal.hide();

        await loadCategories();
        if (selectedCategoryId) {
            await selectCategory(selectedCategoryId);
        }
        showSuccess('Category updated successfully');
    } catch (error) {
        console.error('Error updating category:', error);
        showError('Failed to update category');
    }
}

async function deleteCategory() {
    if (!selectedCategoryId) return;

    if (!confirm('Are you sure you want to delete this category? Products will be unassigned.')) {
        return;
    }

    try {
        const response = await fetch(`/api/category/${selectedCategoryId}`, {
            method: 'DELETE'
        });

        if (!response.ok) throw new Error('Failed to delete category');

        selectedCategoryId = null;
        document.getElementById('categoryTitle').textContent = 'Select a category';
        document.getElementById('categoryActions').style.display = 'none';
        document.getElementById('productList').innerHTML = `
            <div class="text-center text-muted p-5">
                <i class="bi bi-box-seam" style="font-size: 3rem;"></i>
                <p class="mt-3">Select a category to view products</p>
            </div>
        `;

        await loadCategories();
        showSuccess('Category deleted successfully');
    } catch (error) {
        console.error('Error deleting category:', error);
        showError('Failed to delete category');
    }
}

async function removeFromCategory(productId) {
    if (!confirm('Remove this product from the category?')) return;

    try {
        const response = await fetch(`/api/category/0/products/${productId}`, {
            method: 'POST'
        });

        if (!response.ok) throw new Error('Failed to remove product');

        await loadCategoryProducts(selectedCategoryId);
        showSuccess('Product removed from category');
    } catch (error) {
        console.error('Error removing product:', error);
        showError('Failed to remove product');
    }
}

function populateParentCategoryDropdowns() {
    const createSelect = document.getElementById('parentCategory');
    const editSelect = document.getElementById('editParentCategory');

    createSelect.innerHTML = '<option value="">None (Root Category)</option>';
    editSelect.innerHTML = '<option value="0">None (Root Category)</option>';

    function addOptions(categories, level = 0) {
        categories.forEach(category => {
            const indent = '&nbsp;'.repeat(level * 4);
            const option = `<option value="${category.categoryId}">${indent}${category.categoryName}</option>`;
            createSelect.innerHTML += option;
            editSelect.innerHTML += option;

            if (category.subCategories && category.subCategories.length > 0) {
                addOptions(category.subCategories, level + 1);
            }
        });
    }

    addOptions(allCategories);
}

function findCategoryById(categories, id) {
    for (const category of categories) {
        if (category.categoryId === id) return category;
        if (category.subCategories) {
            const found = findCategoryById(category.subCategories, id);
            if (found) return found;
        }
    }
    return null;
}

function formatPrice(price) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(price);
}

function showSuccess(message) {
    // Simple toast notification
    const toast = document.createElement('div');
    toast.className = 'toast align-items-center text-white bg-success border-0 position-fixed top-0 end-0 m-3';
    toast.setAttribute('role', 'alert');
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;
    document.body.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    setTimeout(() => toast.remove(), 3000);
}

function showError(message) {
    const toast = document.createElement('div');
    toast.className = 'toast align-items-center text-white bg-danger border-0 position-fixed top-0 end-0 m-3';
    toast.setAttribute('role', 'alert');
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;
    document.body.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    setTimeout(() => toast.remove(), 3000);
}
