 document.addEventListener('DOMContentLoaded', function () {
            const auctionCard = document.getElementById('auctionCard');
            const fixedPriceCard = document.getElementById('fixedPriceCard');
            const auctionFields = document.getElementById('auctionFields');
            const fixedPriceFields = document.getElementById('fixedPriceFields');
            const isBiddableInput = document.getElementById('isBiddableInput');
            const submitBtn = document.getElementById('submitBtn');
            const pricingSubtitle = document.getElementById('pricingSubtitle');
            const auctionDurationSelect = document.getElementById('auctionDurationSelect');
            const bidEndTimeInput = document.getElementById('bidEndTimeInput');

            // Function to update auction end time
            function updateAuctionEndTime() {
                const duration = parseInt(auctionDurationSelect.value);
                const now = new Date();
                const endTime = new Date(now.getTime() + (duration * 24 * 60 * 60 * 1000));

                // Format for datetime-local input (YYYY-MM-DDTHH:MM)
                const year = endTime.getFullYear();
                const month = String(endTime.getMonth() + 1).padStart(2, '0');
                const day = String(endTime.getDate()).padStart(2, '0');
                const hours = String(endTime.getHours()).padStart(2, '0');
                const minutes = String(endTime.getMinutes()).padStart(2, '0');

                const formattedEndTime = `${year}-${month}-${day}T${hours}:${minutes}`;
                bidEndTimeInput.value = formattedEndTime;
            }

            // Auction card click handler
            auctionCard.addEventListener('click', function () {
                console.log('Auction card clicked');

                // Remove selected class from both cards
                auctionCard.classList.remove('selected');
                fixedPriceCard.classList.remove('selected');

                // Add selected class to auction card
                auctionCard.classList.add('selected');

                // Set form values
                isBiddableInput.value = 'true';

                // Show/hide appropriate fields
                auctionFields.classList.add('show');
                fixedPriceFields.classList.remove('show');

                // Update UI text
                submitBtn.textContent = 'Start Auction';
                pricingSubtitle.textContent = 'Set your starting bid and optional Buy Now price';

                // Set auction end time
                updateAuctionEndTime();

                console.log('Auction mode activated');
            });

            // Fixed price card click handler
            fixedPriceCard.addEventListener('click', function () {
                console.log('Fixed price card clicked');

                // Remove selected class from both cards
                auctionCard.classList.remove('selected');
                fixedPriceCard.classList.remove('selected');

                // Add selected class to fixed price card
                fixedPriceCard.classList.add('selected');

                // Set form values
                isBiddableInput.value = 'false';

                // Show/hide appropriate fields
                fixedPriceFields.classList.add('show');
                auctionFields.classList.remove('show');

                // Update UI text
                submitBtn.textContent = 'List Item';
                pricingSubtitle.textContent = 'Set your fixed price';

                // Clear auction-specific fields
                document.querySelector('input[name="Product.StartingPrice"]').value = '';
                bidEndTimeInput.value = '';

                console.log('Fixed price mode activated');
            });

            // Update auction end time when duration changes
            auctionDurationSelect.addEventListener('change', function () {
                updateAuctionEndTime();
            });

            // Image preview logic
            const imageUpload = document.getElementById('imageUpload');
            const imagePreviewContainer = document.getElementById('imagePreviewContainer');

            imageUpload.addEventListener('change', function () {
                imagePreviewContainer.innerHTML = ''; // Clear previous previews
                const files = this.files;

                if (files.length > 12) {
                    alert('You can upload up to 12 images.');
                    this.value = '';
                    return;
                }

                for (let i = 0; i < files.length; i++) {
                    const file = files[i];
                    const reader = new FileReader();

                    reader.onload = function (e) {
                        const colDiv = document.createElement('div');
                        colDiv.className = 'col-md-2 col-sm-3 col-4 mb-2';

                        const img = document.createElement('img');
                        img.src = e.target.result;
                        img.className = 'img-thumbnail w-100';
                        img.style.height = '120px';
                        img.style.objectFit = 'cover';

                        colDiv.appendChild(img);
                        imagePreviewContainer.appendChild(colDiv);
                    };

                    reader.readAsDataURL(file);
                }
            });

            console.log('Script loaded successfully');
        });
