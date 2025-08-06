document.addEventListener("DOMContentLoaded", () => {
    const imageUpload = document.getElementById("imageUpload");
    const imagePreviewContainer = document.getElementById("imagePreviewContainer");
    const imagePreviewRow = document.getElementById("imagePreviewRow");
    const selectedImagesInfo = document.getElementById("selectedImagesInfo");
    const imageCount = document.getElementById("imageCount");
    const uploadArea = document.querySelector(".upload-area");
    const dynamicFileInputs = document.getElementById("dynamicFileInputs");
    const productForm = document.getElementById("productForm");
    const saveDraftBtn = document.getElementById("saveDraftBtn");
    const draftSaveModal = document.getElementById("draftSaveModal");
    const loadingOverlay = document.getElementById("loadingOverlay");

    const selectedFiles = [];
    const maxFiles = 12;

    // Get a reference to the description field
    const descriptionInput = document.querySelector('textarea[name="Product.Description"]');

    // Form state tracking
    let formIsDirty = false;
    let isSubmitting = false;
    let pendingNavigation = null;

    // Track form changes to determine if form is "dirty"
    function trackFormChanges() {
        const formElements = productForm.querySelectorAll("input, textarea, select");

        formElements.forEach((element) => {
            element.addEventListener("input", () => {
                formIsDirty = true;
            });

            element.addEventListener("change", () => {
                formIsDirty = true;
            });
        });

        // Track listing type card selections
        document.getElementById("auctionCard")?.addEventListener("click", () => {
            formIsDirty = true;
        });

        document.getElementById("fixedPriceCard")?.addEventListener("click", () => {
            formIsDirty = true;
        });
    }

    // Initialize form change tracking
    trackFormChanges();

    // Handle beforeunload event to catch navigation attempts
    window.addEventListener("beforeunload", (e) => {
        if (formIsDirty && !isSubmitting) {
            e.preventDefault();
            e.returnValue = ""; // Required for Chrome
            return ""; // Required for some browsers
        }
    });

    // Handle page navigation attempts (for SPA-style navigation)
    window.addEventListener("pagehide", (e) => {
        if (formIsDirty && !isSubmitting) {
            console.log("Page is hiding with unsaved changes.");
        }
    });

    // Override link clicks and form submissions to show modal
    document.addEventListener("click", (e) => {
        const link = e.target.closest("a");
        if (link && formIsDirty && !isSubmitting) {
            const href = link.getAttribute("href");
            if (href && !href.startsWith("#") && !href.startsWith("javascript:")) {
                e.preventDefault();
                pendingNavigation = href;
                const modalInstance = bootstrap.Modal.getInstance(draftSaveModal) || new bootstrap.Modal(draftSaveModal);
                modalInstance.show();
            }
        }
    });

    // Handle draft save modal actions
    document.getElementById("saveDraftAndLeaveBtn")?.addEventListener("click", async () => {
        await saveDraft();
        if (pendingNavigation) {
            window.location.href = pendingNavigation;
        } else {
            window.history.back();
        }
    });

    document.getElementById("discardChangesBtn")?.addEventListener("click", () => {
        formIsDirty = false;
        const modalInstance = bootstrap.Modal.getInstance(draftSaveModal);
        if (modalInstance) modalInstance.hide();
        if (pendingNavigation) {
            window.location.href = pendingNavigation;
        } else {
            window.history.back();
        }
    });

    // Handle manual draft save button
    saveDraftBtn?.addEventListener("click", async (e) => {
        e.preventDefault(); // Prevent default form submission for this button
        await saveDraft();
    });

    // Save draft function
    async function saveDraft() {
        // Validate description for drafts
        if (!descriptionInput || descriptionInput.value.trim() === "") {
            showErrorMessage("Description cannot be blank when saving as a draft.");
            return; // Stop execution immediately
        }

        try {
            showLoadingOverlay();

            const formData = new FormData();

            // Collect form data
            const formElements = productForm.querySelectorAll("input, textarea, select");
            formElements.forEach((element) => {
                if (element.name && element.value) {
                    if (element.type === "checkbox" || element.type === "radio") {
                        if (element.checked) {
                            formData.append(element.name, element.value);
                        }
                    } else {
                        formData.append(element.name, element.value);
                    }
                }
            });

            // Add selected files
            selectedFiles.forEach((file, index) => {
                formData.append("ImageFiles", file);
            });

            // Add draft flag
            formData.append("IsDraft", "true");

            // Add anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (token) {
                formData.append("__RequestVerificationToken", token);
            }

            const response = await fetch("/Products/SaveDraft", {
                method: "POST",
                body: formData,
            });

            if (response.ok) {
                const result = await response.json();
                formIsDirty = false;
                showSuccessMessage("Draft saved successfully!");

                // Update the form with the draft ID if it's a new draft
                if (result.draftId) {
                    let draftIdInput = document.querySelector('input[name="DraftId"]');
                    if (!draftIdInput) {
                        draftIdInput = document.createElement("input");
                        draftIdInput.type = "hidden";
                        draftIdInput.name = "DraftId";
                        productForm.appendChild(draftIdInput);
                    }
                    draftIdInput.value = result.draftId;
                }
            } else {
                const errorText = await response.text();
                showErrorMessage("Failed to save draft: " + errorText);
            }
        } catch (error) {
            console.error("Error saving draft:", error);
            showErrorMessage("Failed to save draft. Please try again.");
        } finally {
            hideLoadingOverlay();
            const modalInstance = bootstrap.Modal.getInstance(draftSaveModal);
            if (modalInstance) modalInstance.hide();
        }
    }

    function showLoadingOverlay() {
        loadingOverlay.style.display = "flex";
    }

    function hideLoadingOverlay() {
        loadingOverlay.style.display = "none";
    }

    function showSuccessMessage(message) {
        const alert = document.createElement("div");
        alert.className = "alert alert-success alert-dismissible fade show position-fixed";
        alert.style.cssText = "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
        alert.innerHTML = `
      <i class="bi bi-check-circle me-2"></i>
      ${message}
      <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
        document.body.appendChild(alert);

        setTimeout(() => {
            if (alert.parentNode) {
                alert.remove();
            }
        }, 5000);
    }

    function showErrorMessage(message) {
        const alert = document.createElement("div");
        alert.className = "alert alert-danger alert-dismissible fade show position-fixed";
        alert.style.cssText = "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
        alert.innerHTML = `
      <i class="bi bi-exclamation-triangle me-2"></i>
      ${message}
      <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
        document.body.appendChild(alert);

        setTimeout(() => {
            if (alert.parentNode) {
                alert.remove();
            }
        }, 8000);
    }

    // Handle form submission for "Create Listing"
    productForm.addEventListener("submit", (e) => {
        isSubmitting = true;
        formIsDirty = false;
        console.log("Form submitting as Create Listing with", selectedFiles.length, "files");

        // For "Create Listing", all required fields should be enforced by HTML5 validation
        // No need to manipulate 'required' attributes here.

        if (selectedFiles.length > 0) {
            createDynamicFileInputs();
        }
    });

    // Handle file selection - UPDATED to accumulate files from multiple selections
    imageUpload.addEventListener("change", (e) => {
        if (e.target.files && e.target.files.length > 0) {
            handleFiles(e.target.files);
            formIsDirty = true; // Mark form as dirty when files are added
        }
        e.target.value = "";
    });

    // Handle drag and drop
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
        formIsDirty = true; // Mark form as dirty when files are dropped
    });

    function handleFiles(files) {
        const fileArray = Array.from(files);
        const imageFiles = fileArray.filter((file) => file.type.startsWith("image/"));

        if (imageFiles.length === 0) {
            alert("Please select valid image files.");
            return;
        }

        if (selectedFiles.length + imageFiles.length > maxFiles) {
            alert(
                `You can only upload up to ${maxFiles} images. Currently selected: ${selectedFiles.length}. You can add ${maxFiles - selectedFiles.length} more.`,
            );
            return;
        }

        imageFiles.forEach((file) => {
            const isDuplicate = selectedFiles.some(
                (existingFile) =>
                    existingFile.name === file.name &&
                    existingFile.size === file.size &&
                    existingFile.lastModified === file.lastModified,
            );

            if (!isDuplicate) {
                selectedFiles.push(file);
            } else {
                console.log(`File ${file.name} is already selected, skipping...`);
            }
        });

        createDynamicFileInputs();
        updateImagePreview();
    }

    function createDynamicFileInputs() {
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
            <small class="text-muted">${file.name}</small>
            <br>
            <small class="text-muted">${(file.size / 1024 / 1024).toFixed(2)} MB</small>
          </div>
        </div>
      `;

                imagePreviewRow.appendChild(col);

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
      <div class="add-more-item d-flex align-items-center justify-content-center" onclick="document.getElementById('imageUpload').click()" role="button" tabindex="0" aria-label="Add more images">
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
                formIsDirty = true;
            }
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

    // Check if all required elements exist before adding event listeners
    if (auctionCard && fixedPriceCard && isBiddableInput && auctionFields && pricingSubtitle && buyNowLabel && buyNowPriceInput) {
        // Auction card click handler
        auctionCard.addEventListener("click", () => {
            console.log("Auction card clicked"); // Debug log

            // Update visual selection
            auctionCard.classList.add("selected");
            fixedPriceCard.classList.remove("selected");

            // Set form values
            isBiddableInput.value = "true";

            // Show auction fields
            auctionFields.style.display = "block";

            // Update pricing section text
            pricingSubtitle.textContent = "Set your starting bid and buy now price";
            buyNowLabel.innerHTML = "Buy Now Price (Optional)";

            // Make BuyNowPrice optional for auctions
            buyNowPriceInput.removeAttribute("required");
        });

        // Fixed price card click handler
        fixedPriceCard.addEventListener("click", () => {
            console.log("Fixed Price card clicked"); // Debug log

            // Update visual selection
            fixedPriceCard.classList.add("selected");
            auctionCard.classList.remove("selected");

            // Set form values
            isBiddableInput.value = "false";

            // Hide auction fields
            auctionFields.style.display = "none";

            // Update pricing section text
            pricingSubtitle.textContent = "Set your fixed selling price";
            buyNowLabel.innerHTML = 'Price <span class="required">*</span>';

            // Make BuyNowPrice required for fixed price
            buyNowPriceInput.setAttribute("required", "required");
        });

        // Set default state (Fixed Price selected by default)
        fixedPriceCard.click();
    } else {
        console.error("Some required elements for listing type functionality are missing");
    }

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

    // Form submission handler (single listener)
    let isDraft = false;

    const submitBtn = document.getElementById("submitBtn");

    if (saveDraftBtn) {
        saveDraftBtn.addEventListener("click", () => {
            isDraft = true;
        });
    }

    if (submitBtn) {
        submitBtn.addEventListener("click", () => {
            isDraft = false;
        });
    }

    if (productForm) {
        productForm.addEventListener("submit", (e) => {
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
    }
});
