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
