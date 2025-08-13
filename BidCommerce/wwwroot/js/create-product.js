// Global variables
let uploadedImages = []
let draggedElement = null
let draggedIndex = -1
const bootstrap = window.bootstrap // Declare the bootstrap variable

// Ensure all functions are available globally
window.updatePreview = updatePreview
window.updatePreviewImage = updatePreviewImage
window.handleImageUpload = handleImageUpload
window.removeImage = removeImage

function updatePreview() {
    const titleInput = document.querySelector('input[name="Product.Title"]')
    const descriptionInput = document.querySelector('textarea[name="Product.Description"]')
    const buyNowPriceInput = document.querySelector('input[name="Product.BuyNowPrice"]')
    const startingPriceInput = document.querySelector('input[name="Product.StartingPrice"]')
    const categorySelect = document.querySelector('select[name="Product.CategoryId"]')
    const isBiddableInput = document.getElementById("isBiddableInput")
    const auctionDurationSelect = document.getElementById("auctionDurationSelect")

    // Check if elements exist before accessing their values
    const title = titleInput ? titleInput.value || "Enter product title" : "Enter product title"
    const description = descriptionInput
        ? descriptionInput.value || "Add a description for your product"
        : "Add a description for your product"
    const buyNowPrice = buyNowPriceInput ? buyNowPriceInput.value || "0.00" : "0.00"
    const startingPrice = startingPriceInput ? startingPriceInput.value || "0.99" : "0.99"
    const category =
        categorySelect && categorySelect.selectedIndex >= 0
            ? categorySelect.options[categorySelect.selectedIndex].text || "Select category"
            : "Select category"
    const isBiddable = isBiddableInput ? isBiddableInput.value === "True" : false
    const auctionDuration = auctionDurationSelect ? auctionDurationSelect.value || "7" : "7"

    // Update preview elements
    const previewTitle = document.getElementById("previewTitle")
    const previewDescription = document.getElementById("previewDescription")
    const previewCategory = document.getElementById("previewCategory")
    const previewPrice = document.getElementById("previewPrice")
    const previewButtonText = document.getElementById("previewButtonText")
    const previewButtonIcon = document.querySelector("#previewButton i")
    const previewTimeLeft = document.getElementById("previewTimeLeft")

    if (previewTitle) previewTitle.textContent = title
    if (previewDescription) previewDescription.textContent = description
    if (previewCategory) previewCategory.textContent = category

    let displayPrice = "0.00"
    if (isBiddable) {
        displayPrice = startingPrice
        if (previewPrice) previewPrice.textContent = `Starting bid: $${Number.parseFloat(displayPrice).toFixed(2)}`
    } else {
        displayPrice = buyNowPrice
        if (previewPrice) previewPrice.textContent = `Price: $${Number.parseFloat(displayPrice).toFixed(2)}`
    }

    const buttonText = isBiddable ? "Place Bid" : "Buy Now"
    const buttonIcon = isBiddable ? "bi-hammer" : "bi-cart-plus"
    if (previewButtonText) previewButtonText.textContent = buttonText
    if (previewButtonIcon) previewButtonIcon.className = buttonIcon

    const timeLeft = isBiddable ? `${auctionDuration}d left` : "Available"
    if (previewTimeLeft) previewTimeLeft.innerHTML = `<i class="bi bi-clock"></i> ${timeLeft}`
}

function updatePreviewImage() {
    const previewContainer = document.getElementById("previewImage")
    if (previewContainer) {
        if (uploadedImages.length > 0) {
            previewContainer.innerHTML = `<img src="${uploadedImages[0].dataUrl}" alt="Product Image">`
        } else {
            previewContainer.innerHTML = '<i class="bi bi-image" style="font-size: 40px;"></i>'
        }
    }
}

function handleImageUpload(input) {
    if (!input.files) return

    const files = Array.from(input.files)
    files.forEach((file, index) => {
        if (uploadedImages.length < 12) {
            const reader = new FileReader()
            reader.onload = (e) => {
                const imageData = {
                    file: file,
                    dataUrl: e.target.result,
                    name: file.name,
                    size: file.size,
                    id: Date.now() + index,
                }
                uploadedImages.push(imageData)
                renderImages()
                updateImageCount()
                updatePreviewImage()
            }
            reader.readAsDataURL(file)
        }
    })
    input.value = ""
}

function renderImages() {
    const grid = document.getElementById("imagesGrid")
    const instructions = document.getElementById("dragInstructions")

    if (!grid) return

    grid.innerHTML = ""

    uploadedImages.forEach((image, index) => {
        const imageDiv = document.createElement("div")
        imageDiv.className = "image-thumbnail"
        imageDiv.draggable = true
        imageDiv.dataset.imageId = image.id
        imageDiv.dataset.index = index

        imageDiv.innerHTML = `
            <img src="${image.dataUrl}" alt="${image.name}">
            <div class="image-order-badge">${index + 1}</div>
            <button type="button" class="delete-image-btn" onclick="removeImage(${image.id})">
                ×
            </button>
        `

        imageDiv.addEventListener("dragstart", handleDragStart)
        imageDiv.addEventListener("dragover", handleDragOver)
        imageDiv.addEventListener("drop", handleDrop)
        imageDiv.addEventListener("dragend", handleDragEnd)
        imageDiv.addEventListener("dragenter", handleDragEnter)
        imageDiv.addEventListener("dragleave", handleDragLeave)

        grid.appendChild(imageDiv)
    })

    if (instructions) {
        if (uploadedImages.length > 1) {
            instructions.style.display = "block"
        } else {
            instructions.style.display = "none"
        }
    }

    updateFileInputs()
}

function handleDragStart(e) {
    draggedElement = this
    draggedIndex = Number.parseInt(this.dataset.index)
    this.classList.add("dragging")
    e.dataTransfer.effectAllowed = "move"
    e.dataTransfer.setData("text/html", "")

    const dragDropZone = document.getElementById("dragDropZone")
    if (dragDropZone) {
        dragDropZone.classList.add("drag-active")
    }
}

function handleDragOver(e) {
    e.preventDefault()
    e.dataTransfer.dropEffect = "move"
}

function handleDragEnter(e) {
    e.preventDefault()
    if (this !== draggedElement && this.classList.contains("image-thumbnail")) {
        this.classList.add("drag-over")
    }
}

function handleDragLeave(e) {
    if (this.classList.contains("image-thumbnail")) {
        this.classList.remove("drag-over")
    }
}

function handleDrop(e) {
    e.preventDefault()
    e.stopPropagation()

    if (this !== draggedElement && this.classList.contains("image-thumbnail")) {
        const targetIndex = Number.parseInt(this.dataset.index)
        if (draggedIndex !== targetIndex) {
            const draggedImage = uploadedImages[draggedIndex]
            uploadedImages.splice(draggedIndex, 1)
            uploadedImages.splice(targetIndex, 0, draggedImage)
            renderImages()
            updatePreviewImage()
        }
    }
    this.classList.remove("drag-over")
}

function handleDragEnd(e) {
    this.classList.remove("dragging")
    document.querySelectorAll(".image-thumbnail").forEach((el) => {
        el.classList.remove("drag-over")
    })

    const dragDropZone = document.getElementById("dragDropZone")
    if (dragDropZone) {
        dragDropZone.classList.remove("drag-active")
    }

    draggedElement = null
    draggedIndex = -1
}

function removeImage(imageId) {
    uploadedImages = uploadedImages.filter((img) => img.id !== imageId)
    renderImages()
    updateImageCount()
    updatePreviewImage()
}

function updateImageCount() {
    const countElement = document.getElementById("imageCount")
    const infoElement = document.getElementById("selectedImagesInfo")

    if (countElement && infoElement) {
        if (uploadedImages.length > 0) {
            countElement.textContent = uploadedImages.length
            infoElement.style.display = "block"
        } else {
            infoElement.style.display = "none"
        }
    }
}

function updateFileInputs() {
    const container = document.getElementById("dynamicFileInputs")
    if (!container) return

    container.innerHTML = ""
    uploadedImages.forEach((image, index) => {
        const input = document.createElement("input")
        input.type = "file"
        input.name = "ImageFiles"
        input.style.display = "none"
        input.files = createFileList([image.file])
        container.appendChild(input)
    })
}

function createFileList(files) {
    const dt = new DataTransfer()
    files.forEach((file) => dt.items.add(file))
    return dt.files
}

// Initialize when DOM is loaded
document.addEventListener("DOMContentLoaded", () => {
    console.log("Create product script loaded")

    // Initialize listing type functionality
    initializeListingType()

    // Initialize image upload functionality
    initializeImageUpload()

    // Initialize form tracking
    initializeFormTracking()

    // Initialize preview updates
    initializePreviewUpdates()

    // Set initial preview
    updatePreview()
})

function initializeListingType() {
    const auctionCard = document.getElementById("auctionCard")
    const fixedPriceCard = document.getElementById("fixedPriceCard")
    const isBiddableInput = document.getElementById("isBiddableInput")
    const auctionFields = document.getElementById("auctionFields")
    const buyNowLabel = document.getElementById("buyNowLabel")
    const pricingSubtitle = document.getElementById("pricingSubtitle")
    const auctionDurationSelect = document.getElementById("auctionDurationSelect")

    function selectListingType(isAuction) {
        if (!isBiddableInput || !auctionFields || !buyNowLabel || !pricingSubtitle) return

        if (isAuction) {
            isBiddableInput.value = "True"
            if (auctionCard) auctionCard.classList.add("active")
            if (fixedPriceCard) fixedPriceCard.classList.remove("active")
            auctionFields.classList.add("show")
            buyNowLabel.textContent = "Buy Now Price (Optional)"
            pricingSubtitle.textContent = "Set your starting bid and auction duration"
            updateAuctionEndTime()
        } else {
            isBiddableInput.value = "False"
            if (fixedPriceCard) fixedPriceCard.classList.add("active")
            if (auctionCard) auctionCard.classList.remove("active")
            auctionFields.classList.remove("show")
            buyNowLabel.textContent = "Price"
            pricingSubtitle.textContent = "Set your fixed price for this item"
        }
        updatePreview()
    }

    function updateAuctionEndTime() {
        const durationSelect = document.getElementById("auctionDurationSelect")
        const endTimeInput = document.getElementById("bidEndTimeInput")

        if (durationSelect && endTimeInput) {
            const duration = Number.parseInt(durationSelect.value)
            const now = new Date()
            const endTime = new Date(now.getTime() + duration * 24 * 60 * 60 * 1000)

            const year = endTime.getFullYear()
            const month = String(endTime.getMonth() + 1).padStart(2, "0")
            const day = String(endTime.getDate()).padStart(2, "0")
            const hours = String(endTime.getHours()).padStart(2, "0")
            const minutes = String(endTime.getMinutes()).padStart(2, "0")

            endTimeInput.value = `${year}-${month}-${day}T${hours}:${minutes}`
        }
    }

    // Add event listeners
    if (auctionCard) {
        auctionCard.addEventListener("click", () => selectListingType(true))
    }
    if (fixedPriceCard) {
        fixedPriceCard.addEventListener("click", () => selectListingType(false))
    }
    if (auctionDurationSelect) {
        auctionDurationSelect.addEventListener("change", updateAuctionEndTime)
    }

    // Set default state
    selectListingType(false)
}

function initializeImageUpload() {
    const imageUpload = document.getElementById("imageUpload")
    const uploadArea = document.querySelector(".upload-area")

    if (imageUpload) {
        imageUpload.addEventListener("change", function (e) {
            handleImageUpload(this)
        })
    }

    if (uploadArea) {
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
            if (e.dataTransfer.files) {
                const fakeInput = { files: e.dataTransfer.files }
                handleImageUpload(fakeInput)
            }
        })
    }
}

function initializeFormTracking() {
    const productForm = document.getElementById("productForm")
    const saveDraftBtn = document.getElementById("saveDraftBtn")
    const draftSaveModal = document.getElementById("draftSaveModal")
    const loadingOverlay = document.getElementById("loadingOverlay")

    let formIsDirty = false
    const isSubmitting = false
    const pendingNavigation = null

    // Track form changes
    if (productForm) {
        const formElements = productForm.querySelectorAll("input, textarea, select")
        formElements.forEach((element) => {
            element.addEventListener("input", () => {
                formIsDirty = true
            })
            element.addEventListener("change", () => {
                formIsDirty = true
            })
        })
    }

    // Handle beforeunload
    window.addEventListener("beforeunload", (e) => {
        if (formIsDirty && !isSubmitting) {
            e.preventDefault()
            e.returnValue = ""
            return ""
        }
    })

    // Handle draft save
    if (saveDraftBtn) {
        saveDraftBtn.addEventListener("click", async (e) => {
            e.preventDefault()
            await saveDraft()
        })
    }

    // Modal button handlers
    const saveDraftAndLeaveBtn = document.getElementById("saveDraftAndLeaveBtn")
    const discardChangesBtn = document.getElementById("discardChangesBtn")

    if (saveDraftAndLeaveBtn) {
        saveDraftAndLeaveBtn.addEventListener("click", async () => {
            await saveDraft()
            if (pendingNavigation) {
                window.location.href = pendingNavigation
            }
        })
    }

    if (discardChangesBtn) {
        discardChangesBtn.addEventListener("click", () => {
            formIsDirty = false
            if (pendingNavigation) {
                window.location.href = pendingNavigation
            }
        })
    }

    async function saveDraft() {
        const descriptionInput = document.querySelector('textarea[name="Product.Description"]')

        if (!descriptionInput || descriptionInput.value.trim() === "") {
            showErrorMessage("Description cannot be blank when saving as a draft.")
            return
        }

        try {
            showLoadingOverlay()

            const formData = new FormData()

            // Collect form data
            if (productForm) {
                const formElements = productForm.querySelectorAll("input, textarea, select")
                formElements.forEach((element) => {
                    if (element.name && element.value) {
                        if (element.type === "checkbox" || element.type === "radio") {
                            if (element.checked) {
                                formData.append(element.name, element.value)
                            }
                        } else {
                            formData.append(element.name, element.value)
                        }
                    }
                })
            }

            // Add images
            uploadedImages.forEach((imageData) => {
                formData.append("ImageFiles", imageData.file)
            })

            formData.append("IsDraft", "true")

            // Add anti-forgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value
            if (token) {
                formData.append("__RequestVerificationToken", token)
            }

            const response = await fetch("/Products/SaveDraft", {
                method: "POST",
                body: formData,
            })

            if (response.ok) {
                const result = await response.json()
                formIsDirty = false
                showSuccessMessage("Draft saved successfully!")

                if (result.draftId) {
                    let draftIdInput = document.querySelector('input[name="DraftId"]')
                    if (!draftIdInput && productForm) {
                        draftIdInput = document.createElement("input")
                        draftIdInput.type = "hidden"
                        draftIdInput.name = "DraftId"
                        productForm.appendChild(draftIdInput)
                    }
                    if (draftIdInput) {
                        draftIdInput.value = result.draftId
                    }
                }
            } else {
                const errorText = await response.text()
                showErrorMessage("Failed to save draft: " + errorText)
            }
        } catch (error) {
            console.error("Error saving draft:", error)
            showErrorMessage("Failed to save draft. Please try again.")
        } finally {
            hideLoadingOverlay()
            if (draftSaveModal && bootstrap) {
                const modalInstance = bootstrap.Modal.getInstance(draftSaveModal)
                if (modalInstance) modalInstance.hide()
            }
        }
    }

    function showLoadingOverlay() {
        if (loadingOverlay) {
            loadingOverlay.style.display = "flex"
        }
    }

    function hideLoadingOverlay() {
        if (loadingOverlay) {
            loadingOverlay.style.display = "none"
        }
    }

    function showSuccessMessage(message) {
        const alert = document.createElement("div")
        alert.className = "alert alert-success alert-dismissible fade show position-fixed"
        alert.style.cssText = "top: 20px; right: 20px; z-index: 9999; min-width: 300px;"
        alert.innerHTML = `
            <i class="bi bi-check-circle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `
        document.body.appendChild(alert)

        setTimeout(() => {
            if (alert.parentNode) {
                alert.remove()
            }
        }, 5000)
    }

    function showErrorMessage(message) {
        const alert = document.createElement("div")
        alert.className = "alert alert-danger alert-dismissible fade show position-fixed"
        alert.style.cssText = "top: 20px; right: 20px; z-index: 9999; min-width: 300px;"
        alert.innerHTML = `
            <i class="bi bi-exclamation-triangle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `
        document.body.appendChild(alert)

        setTimeout(() => {
            if (alert.parentNode) {
                alert.remove()
            }
        }, 8000)
    }
}

function initializePreviewUpdates() {
    // Add event listeners for real-time preview updates
    document.addEventListener("input", (e) => {
        if (e.target.matches("input, textarea, select")) {
            updatePreview()
        }
    })

    document.addEventListener("change", (e) => {
        if (e.target.matches("select")) {
            updatePreview()
        }
    })
}
