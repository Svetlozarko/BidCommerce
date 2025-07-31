document.addEventListener("DOMContentLoaded", () => {
    const imageUpload = document.getElementById("imageUpload");
    const imagePreviewContainer = document.getElementById("imagePreviewContainer");
    const imagePreviewRow = document.getElementById("imagePreviewRow");
    const selectedImagesInfo = document.getElementById("selectedImagesInfo");
    const imageCount = document.getElementById("imageCount");
    const uploadArea = document.querySelector(".upload-area");
    const dynamicFileInputs = document.getElementById("dynamicFileInputs");

    const selectedFiles = [];
    const maxFiles = 12;

    // Handle file selection - accumulate files from multiple selections
    imageUpload.addEventListener("change", (e) => {
        if (e.target.files && e.target.files.length > 0) {
            handleFiles(e.target.files);
        }
        // Reset input so same files can be selected again if needed
        e.target.value = "";
    });

    // Drag and drop handlers
    uploadArea.addEventListener("dragover", (e) => {
        e.preventDefault();
        uploadArea.classList.add("drag-over");
    });

    uploadArea.addEventListener("dragleave", (e) => {
        e.preventDefault();
        uploadArea.classList.remove("drag-over");
    });

    uploadArea.addEventListener("drop", (e) => {
        e.preventDefault();
        uploadArea.classList.remove("drag-over");
        handleFiles(e.dataTransfer.files);
    });

    function handleFiles(files) {
        const fileArray = Array.from(files);

        // Filter only images
        const imageFiles = fileArray.filter((file) => file.type.startsWith("image/"));

        if (imageFiles.length === 0) {
            alert("Please select valid image files.");
            return;
        }

        // Check if adding these files would exceed the limit
        if (selectedFiles.length + imageFiles.length > maxFiles) {
            alert(
                `You can only upload up to ${maxFiles} images. Currently selected: ${selectedFiles.length}. You can add ${maxFiles - selectedFiles.length} more.`
            );
            return;
        }

        // Add new files avoiding duplicates & count how many added
        let addedCount = 0;

        imageFiles.forEach((file) => {
            const isDuplicate = selectedFiles.some(
                (existingFile) =>
                    existingFile.name === file.name &&
                    existingFile.size === file.size &&
                    existingFile.lastModified === file.lastModified
            );

            if (!isDuplicate) {
                selectedFiles.push(file);
                addedCount++;
            } else {
                console.log(`File ${file.name} is already selected, skipping...`);
            }
        });

        if (addedCount > 0) {
            console.log(`Added ${addedCount} new files. Total: ${selectedFiles.length}`);
        }

        createDynamicFileInputs();
        updateImagePreview();
    }

    function createDynamicFileInputs() {
        // Clear existing dynamic inputs
        dynamicFileInputs.innerHTML = "";

        selectedFiles.forEach((file, index) => {
            const fileInput = document.createElement("input");
            fileInput.type = "file";
            fileInput.name = "ImageFiles";
            fileInput.style.display = "none";
            fileInput.setAttribute("data-index", index);

            const dt = new DataTransfer();
            dt.items.add(file);
            fileInput.files = dt.files;

            dynamicFileInputs.appendChild(fileInput);
        });
    }

    function updateImagePreview() {
        imagePreviewRow.innerHTML = "";

        if (selectedFiles.length === 0) {
            selectedImagesInfo.style.display = "none";
            uploadArea.classList.remove("has-images");
            return;
        }

        selectedImagesInfo.style.display = "block";
        imageCount.textContent = selectedFiles.length;
        uploadArea.classList.add("has-images");

        selectedFiles.forEach((file, index) => {
            const reader = new FileReader();
            reader.onload = (e) => {
                const col = document.createElement("div");
                col.className = "col-md-3 col-sm-4 col-6 mb-3";

                col.innerHTML = `
                    <div class="image-preview-item position-relative">
                      <img src="${e.target.result}" class="img-fluid rounded" style="width: 100%; height: 150px; object-fit: cover;" alt="Preview ${index + 1}">
                      <button type="button" class="btn btn-danger btn-sm position-absolute top-0 end-0 m-1 remove-image" data-index="${index}">
                        <i class="bi bi-x"></i>
                      </button>
                      <div class="image-info mt-1">
                        <small class="text-muted">${file.name}</small><br>
                        <small class="text-muted">${(file.size / 1024 / 1024).toFixed(2)} MB</small>
                      </div>
                    </div>
                `;

                imagePreviewRow.appendChild(col);

                // Add "Add More" button after last image
                if (index === selectedFiles.length - 1) {
                    createAddMoreButton();
                }
            };
            reader.readAsDataURL(file);
        });
    }

    function createAddMoreButton() {
        const existingButton = document.getElementById("addMoreButton");
        if (existingButton) {
            existingButton.remove();
        }

        if (selectedFiles.length > 0 && selectedFiles.length < maxFiles) {
            const addMoreButton = document.createElement("div");
            addMoreButton.id = "addMoreButton";
            addMoreButton.className = "col-md-3 col-sm-4 col-6 mb-3";
            addMoreButton.innerHTML = `
                <div class="add-more-item d-flex align-items-center justify-content-center" 
                     onclick="document.getElementById('imageUpload').click()" 
                     role="button" tabindex="0" aria-label="Add more images" >
                  <div class="text-center">
                    <i class="bi bi-plus-circle" style="font-size: 2rem; color: #6c757d;"></i>
                    <div class="mt-2 text-muted">Add More</div>
                    <small class="text-muted">${maxFiles - selectedFiles.length} remaining</small>
                  </div>
                </div>
            `;

            // Keyboard accessibility: Enter and Space triggers click
            addMoreButton.querySelector(".add-more-item").addEventListener("keydown", (e) => {
                if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    document.getElementById("imageUpload").click();
                }
            });

            imagePreviewRow.appendChild(addMoreButton);
        }
    }

    // Handle image removal with stable indexing by reassigning indices after removal
    imagePreviewRow.addEventListener("click", (e) => {
        if (e.target.classList.contains("remove-image") || e.target.closest(".remove-image")) {
            const button = e.target.classList.contains("remove-image") ? e.target : e.target.closest(".remove-image");
            const index = Number.parseInt(button.getAttribute("data-index"));

            if (!isNaN(index)) {
                selectedFiles.splice(index, 1);
                createDynamicFileInputs();
                updateImagePreview();
            }
        }
    });

    // Form submission handler (single listener)
    let isDraft = false;

    document.getElementById("saveDraftBtn")?.addEventListener("click", () => {
        isDraft = true;
    });

    document.getElementById("submitBtn")?.addEventListener("click", () => {
        isDraft = false;
    });

    document.getElementById("productForm").addEventListener("submit", (e) => {
        console.log("Submitting as", isDraft ? "Draft" : "Listing");

        if (isDraft) {
            // Disable client-side required validation for drafts
            const requiredFields = document.querySelectorAll("#productForm [required]");
            requiredFields.forEach((field) => {
                field.dataset.originalRequired = "true";
                field.removeAttribute("required");
            });
        } else {
            // Restore required attributes if needed (optional)
            const requiredFields = document.querySelectorAll("#productForm [data-original-required='true']");
            requiredFields.forEach((field) => {
                field.setAttribute("required", "required");
                field.removeAttribute("data-original-required");
            });
        }

        // Ensure files are properly included
        if (selectedFiles.length > 0) {
            createDynamicFileInputs();
        }
    });

    // Listing type functionality
    const auctionCard = document.getElementById("auctionCard");
    const fixedPriceCard = document.getElementById("fixedPriceCard");
    const isBiddableInput = document.getElementById("isBiddableInput");
    const auctionFields = document.getElementById("auctionFields");
    const pricingSubtitle = document.getElementById("pricingSubtitle");
    const buyNowLabel = document.getElementById("buyNowLabel");
    const buyNowPriceInput = document.getElementById("buyNowPriceInput");

    auctionCard.addEventListener("click", () => {
        auctionCard.classList.add("selected");
        fixedPriceCard.classList.remove("selected");
        isBiddableInput.value = "true";
        auctionFields.style.display = "block";
        pricingSubtitle.textContent = "Set your starting bid and buy now price";
        buyNowLabel.innerHTML = "Buy Now Price (Optional)";
        buyNowPriceInput.removeAttribute("required");
    });

    fixedPriceCard.addEventListener("click", () => {
        fixedPriceCard.classList.add("selected");
        auctionCard.classList.remove("selected");
        isBiddableInput.value = "false";
        auctionFields.style.display = "none";
        pricingSubtitle.textContent = "Set your fixed selling price";
        buyNowLabel.innerHTML = 'Price <span class="required">*</span>';
        buyNowPriceInput.setAttribute("required", "required");
    });

    // Auction duration handling
    const auctionDurationSelect = document.getElementById("auctionDurationSelect");
    const bidEndTimeInput = document.getElementById("bidEndTimeInput");

    if (auctionDurationSelect && bidEndTimeInput) {
        auctionDurationSelect.addEventListener("change", function () {
            const days = Number.parseInt(this.value);
            const endDate = new Date();
            endDate.setDate(endDate.getDate() + days);
            const formattedDate = endDate.toISOString().slice(0, 16);
            bidEndTimeInput.value = formattedDate;
        });

        // Set default auction end time
        if (auctionDurationSelect.value) {
            const days = Number.parseInt(auctionDurationSelect.value);
            const endDate = new Date();
            endDate.setDate(endDate.getDate() + days);
            const formattedDate = endDate.toISOString().slice(0, 16);
            bidEndTimeInput.value = formattedDate;
        }
    }
});
