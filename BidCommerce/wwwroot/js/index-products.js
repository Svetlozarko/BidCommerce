// Toggle sidebar visibility
function toggleFilters() {
    const sidebar = document.getElementById('filtersSidebar');
    sidebar.classList.toggle('visible');
}

// View switch: grid or list
function setView(viewType) {
    const buttons = document.querySelectorAll('.view-btn');
    buttons.forEach(btn => btn.classList.remove('active'));
    event.target.classList.add('active');

    const grid = document.getElementById('productsGrid');
    grid.style.gridTemplateColumns = viewType === 'list'
        ? '1fr'
        : 'repeat(auto-fill, minmax(280px, 1fr))';
}

// Filters: Update and submit form
function submitFilters() {
    document.getElementById('filterForm').submit();
}

function updateSort() {
    const sortValue = document.getElementById('sortBy').value;
    document.getElementById('hiddenSortBy').value = sortValue;
    submitFilters();
}

function updatePriceFilter() {
    const min = document.getElementById('minPrice').value;
    const max = document.getElementById('maxPrice').value;
    document.getElementById('hiddenMinPrice').value = min;
    document.getElementById('hiddenMaxPrice').value = max;
    submitFilters();
}

function updatePriceSlider() {
    const value = document.getElementById('priceSlider').value;
    document.getElementById('maxPrice').value = value;
    document.getElementById('hiddenMaxPrice').value = value;
    submitFilters();
}

function updateListingType() {
    const selected = document.querySelector('input[name="listingType"]:checked');
    if (selected) {
        document.getElementById('hiddenListingType').value = selected.value;
        submitFilters();
    }
}

// Wishlist toggle with AJAX call
async function toggleWishlist(productId, button) {
    const icon = button.querySelector('i');
    const isAdding = icon.classList.contains('bi-heart');

    try {
        const url = isAdding
            ? `/Products/AddToWatchlist/${productId}`
            : `/Products/RemoveFromWatchlist/${productId}`;
        const method = 'POST';

        const response = await fetch(url, { method });
        if (!response.ok) throw new Error('Failed to update watchlist');

        // Toggle icon
        icon.classList.toggle('bi-heart');
        icon.classList.toggle('bi-heart-fill');

        // Optional color feedback
        button.style.color = isAdding ? '#dc2626' : '#6b7280';

    } catch (err) {
        console.error(err);
        alert('There was a problem updating your watchlist. Please try again.');
    }
}
