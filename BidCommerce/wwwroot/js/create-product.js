document.addEventListener("DOMContentLoaded", () => {
    const imageUpload = document.getElementById("imageUpload")
    const imagePreviewContainer = document.getElementById("imagePreviewContainer")
    const imagePreviewRow = document.getElementById("imagePreviewRow")
    const selectedImagesInfo = document.getElementById("selectedImagesInfo")
    const imageCount = document.getElementById("imageCount")
    const uploadArea = document.querySelector(".upload-area")
    const dynamicFileInputs = document.getElementById("dynamicFileInputs")

    const selectedFiles = []
    const maxFiles = 12

    // Handle file selection - UPDATED to accumulate files from multiple selections
    imageUpload.addEventListener("change", (e) => {
        if (e.target.files && e.target.files.length > 0) {
            handleFiles(e.target.files)
        }
        // Reset the input so the same files can be selected again if needed
        e.target.value = ""
    })

    // Handle drag and drop
    uploadArea.addEventListener("dragover", (e) => {
        e.preventDefault()
        uploadArea.classList.add("drag-over")
    })

    uploadArea.addEventListener("dragleave", (e) => {
        e.preventDefault()
        uploadArea.classList.remove("drag-over")
    })

    uploadArea.addEventListener("drop", (e) => {
        e.preventDefault()
        uploadArea.classList.remove("drag-over")
        handleFiles(e.dataTransfer.files)
    })

    function handleFiles(files) {
        const fileArray = Array.from(files)

        // Filter only image files
        const imageFiles = fileArray.filter((file) => file.type.startsWith("image/"))

        if (imageFiles.length === 0) {
            alert("Please select valid image files.")
            return
        }

        // Check if adding these files would exceed the limit
        if (selectedFiles.length + imageFiles.length > maxFiles) {
            alert(
                `You can only upload up to ${maxFiles} images. Currently selected: ${selectedFiles.length}. You can add ${maxFiles - selectedFiles.length} more.`,
            )
            return
        }

        // Add new files to selectedFiles array, avoiding duplicates
        imageFiles.forEach((file) => {
            // Check if file is already selected (by name, size, and last modified date for better uniqueness)
            const isDuplicate = selectedFiles.some(
                (existingFile) =>
                    existingFile.name === file.name &&
                    existingFile.size === file.size &&
                    existingFile.lastModified === file.lastModified,
            )

            if (!isDuplicate) {
                selectedFiles.push(file)
            } else {
                console.log(`File ${file.name} is already selected, skipping...`)
            }
        })

        // Show feedback about how many files were added
        const newFilesCount = imageFiles.filter(
            (file) =>
                !selectedFiles.some(
                    (existingFile) =>
                        existingFile.name === file.name &&
                        existingFile.size === file.size &&
                        existingFile.lastModified === file.lastModified,
                ),
        ).length

        if (newFilesCount > 0) {
            console.log(`Added ${newFilesCount} new files. Total: ${selectedFiles.length}`)
        }

        // Create dynamic file inputs for form submission
        createDynamicFileInputs()

        // Update preview
        updateImagePreview()
    }

    function createDynamicFileInputs() {
        // Clear existing dynamic inputs
        dynamicFileInputs.innerHTML = ""

        // Create individual file inputs for each selected file
        selectedFiles.forEach((file, index) => {
            const fileInput = document.createElement("input")
            fileInput.type = "file"
            fileInput.name = "ImageFiles"
            fileInput.style.display = "none"
            fileInput.setAttribute("data-index", index)

            // Create a DataTransfer object and add the file to it
            const dt = new DataTransfer()
            dt.items.add(file)
            fileInput.files = dt.files

            dynamicFileInputs.appendChild(fileInput)
        })
    }

    function updateImagePreview() {
        imagePreviewRow.innerHTML = ""

        if (selectedFiles.length === 0) {
            selectedImagesInfo.style.display = "none"
            uploadArea.classList.remove("has-images")
            return
        }

        selectedImagesInfo.style.display = "block"
        imageCount.textContent = selectedFiles.length
        uploadArea.classList.add("has-images")

        selectedFiles.forEach((file, index) => {
            const reader = new FileReader()
            reader.onload = (e) => {
                const col = document.createElement("div")
                col.className = "col-md-3 col-sm-4 col-6 mb-3"

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
      `

                imagePreviewRow.appendChild(col)

                // Add the "Add More" button after the last image
                if (index === selectedFiles.length - 1) {
                    createAddMoreButton()
                }
            }
            reader.readAsDataURL(file)
        })
    }

    // Add this after the updateImagePreview function
    function createAddMoreButton() {
        // Remove existing add more button if it exists
        const existingButton = document.getElementById("addMoreButton")
        if (existingButton) {
            existingButton.remove()
        }

        // Only show "Add More" button if we have images but haven't reached the limit
        if (selectedFiles.length > 0 && selectedFiles.length < maxFiles) {
            const addMoreButton = document.createElement("div")
            addMoreButton.id = "addMoreButton"
            addMoreButton.className = "col-md-3 col-sm-4 col-6 mb-3"
            addMoreButton.innerHTML = `
      <div class="add-more-item d-flex align-items-center justify-content-center" onclick="document.getElementById('imageUpload').click()">
        <div class="text-center">
          <i class="bi bi-plus-circle" style="font-size: 2rem; color: #6c757d;"></i>
          <div class="mt-2 text-muted">Add More</div>
          <small class="text-muted">${maxFiles - selectedFiles.length} remaining</small>
        </div>
      </div>
    `
            imagePreviewRow.appendChild(addMoreButton)
        }
    }

    // Handle image removal
    imagePreviewRow.addEventListener("click", (e) => {
        if (e.target.classList.contains("remove-image") || e.target.closest(".remove-image")) {
            const button = e.target.classList.contains("remove-image") ? e.target : e.target.closest(".remove-image")
            const index = Number.parseInt(button.getAttribute("data-index"))

            // Remove file from selectedFiles array
            selectedFiles.splice(index, 1)

            // Recreate dynamic file inputs
            createDynamicFileInputs()

            // Update preview
            updateImagePreview()
        }
    })

    // Form submission handler to ensure files are properly included
    document.getElementById("productForm").addEventListener("submit", (e) => {
        console.log("Form submitting with", selectedFiles.length, "files")

        // Double-check that we have the dynamic inputs
        if (selectedFiles.length > 0) {
            createDynamicFileInputs()
        }
    })

    // Listing type functionality
    const auctionCard = document.getElementById("auctionCard")
    const fixedPriceCard = document.getElementById("fixedPriceCard")
    const isBiddableInput = document.getElementById("isBiddableInput")
    const auctionFields = document.getElementById("auctionFields")
    const pricingSubtitle = document.getElementById("pricingSubtitle")
    const buyNowLabel = document.getElementById("buyNowLabel")
    const buyNowPriceInput = document.getElementById("buyNowPriceInput")

    // Auction card click
    auctionCard.addEventListener("click", () => {
        auctionCard.classList.add("selected")
        fixedPriceCard.classList.remove("selected")
        isBiddableInput.value = "true"
        auctionFields.style.display = "block"
        pricingSubtitle.textContent = "Set your starting bid and buy now price"
        buyNowLabel.innerHTML = "Buy Now Price (Optional)"
        buyNowPriceInput.removeAttribute("required")
    })

    // Fixed price card click
    fixedPriceCard.addEventListener("click", () => {
        fixedPriceCard.classList.add("selected")
        auctionCard.classList.remove("selected")
        isBiddableInput.value = "false"
        auctionFields.style.display = "none"
        pricingSubtitle.textContent = "Set your fixed selling price"
        buyNowLabel.innerHTML = 'Price <span class="required">*</span>'
        buyNowPriceInput.setAttribute("required", "required")
    })

    // Auction duration handling
    const auctionDurationSelect = document.getElementById("auctionDurationSelect")
    const bidEndTimeInput = document.getElementById("bidEndTimeInput")

    if (auctionDurationSelect && bidEndTimeInput) {
        auctionDurationSelect.addEventListener("change", function () {
            const days = Number.parseInt(this.value)
            const endDate = new Date()
            endDate.setDate(endDate.getDate() + days)

            // Format for datetime-local input
            const formattedDate = endDate.toISOString().slice(0, 16)
            bidEndTimeInput.value = formattedDate
        })

        // Set default auction end time
        if (auctionDurationSelect.value) {
            const days = Number.parseInt(auctionDurationSelect.value)
            const endDate = new Date()
            endDate.setDate(endDate.getDate() + days)
            const formattedDate = endDate.toISOString().slice(0, 16)
            bidEndTimeInput.value = formattedDate
        }
    }
})
