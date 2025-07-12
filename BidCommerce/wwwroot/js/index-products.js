    // Filter and view functionality
    function toggleFilters() {
        const sidebar = document.getElementById('filtersSidebar');
    sidebar.style.display = sidebar.style.display === 'none' ? 'block' : 'none';
    }

    function setView(viewType) {
        const buttons = document.querySelectorAll('.view-btn');
        buttons.forEach(btn => btn.classList.remove('active'));
    event.target.classList.add('active');

    const grid = document.getElementById('productsGrid');
    if (viewType === 'list') {
        grid.style.gridTemplateColumns = '1fr';
        } else {
        grid.style.gridTemplateColumns = 'repeat(auto-fill, minmax(280px, 1fr))';
        }
    }

    function submitFilters() {
        document.getElementById('filterForm').submit();
    }

    function updateSort() {
        const sortValue = document.getElementById('sortBy').value;
    document.getElementById('hiddenSortBy').value = sortValue;
    submitFilters();
    }

    function updatePriceFilter() {
        const minPrice = document.getElementById('minPrice').value;
    const maxPrice = document.getElementById('maxPrice').value;
    document.getElementById('hiddenMinPrice').value = minPrice;
    document.getElementById('hiddenMaxPrice').value = maxPrice;
    }

    function updatePriceSlider() {
        const sliderValue = document.getElementById('priceSlider').value;
    document.getElementById('maxPrice').value = sliderValue;
    document.getElementById('hiddenMaxPrice').value = sliderValue;
    }

    function updateListingType() {
        const selectedType = document.querySelector('input[name="listingType"]:checked').value;
    document.getElementById('hiddenListingType').value = selectedType;
    }

    // Product actions
    function toggleWishlist(productId) {
        const btn = event.target.closest('.wishlist-btn');
    const icon = btn.querySelector('i');

    if (icon.classList.contains('bi-heart')) {
        icon.classList.remove('bi-heart');
    icon.classList.add('bi-heart-fill');
    btn.style.color = '#dc2626';
        } else {
        icon.classList.remove('bi-heart-fill');
    icon.classList.add('bi-heart');
    btn.style.color = '#6b7280';
        }

        // Here you would typically make an AJAX call to save the wishlist state
        // fetch(`/api/wishlist/${productId}`, {method: 'POST' });
    }


function submitFilters() {
    const form = document.getElementById('filterForm');
    form.submit();
}

function updateSort() {
    const sortBy = document.getElementById('sortBy').value;
    document.getElementById('hiddenSortBy').value = sortBy;
    submitFilters();
}

function updateListingType() {
    const listingTypeInputs = document.querySelectorAll('input[name="listingType"]');
    let selectedValue = "";
    listingTypeInputs.forEach(input => {
        if (input.checked) {
            selectedValue = input.value;
        }
    });
    document.getElementById('hiddenListingType').value = selectedValue;
    submitFilters();
}

function updatePriceFilter() {
    const minPrice = document.getElementById('minPrice').value;
    const maxPrice = document.getElementById('maxPrice').value;
    document.getElementById('hiddenMinPrice').value = minPrice;
    document.getElementById('hiddenMaxPrice').value = maxPrice;
    submitFilters();
}

function updatePriceSlider() {
    const priceSlider = document.getElementById('priceSlider');
    const maxPrice = priceSlider.value;
    document.getElementById('maxPrice').value = maxPrice;
    document.getElementById('hiddenMaxPrice').value = maxPrice;
    submitFilters();
}

function toggleFilters() {
    const sidebar = document.getElementById('filtersSidebar');
    sidebar.classList.toggle('visible');
}

function setView(view) {
    // Implement view toggle logic here if needed
}

function toggleWishlist(productId, button) {
    // Example placeholder for toggling wishlist heart icon
    const icon = button.querySelector('i');
    if (icon.classList.contains('bi-heart')) {
        icon.classList.remove('bi-heart');
        icon.classList.add('bi-heart-fill');
    } else {
        icon.classList.remove('bi-heart-fill');
        icon.classList.add('bi-heart');
    }
    // TODO: Add AJAX call to update wishlist server-side
}