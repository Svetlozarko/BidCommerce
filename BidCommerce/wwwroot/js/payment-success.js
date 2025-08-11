// Initialize page animations and interactions
document.addEventListener("DOMContentLoaded", () => {
    // Add entrance animations to elements
    initializeAnimations()

    // Add interactive effects
    addInteractiveEffects()

    // Start floating particles
    animateParticles()
})

function initializeAnimations() {
    // Add stagger animation to detail rows
    const detailRows = document.querySelectorAll(".detail-row")
    detailRows.forEach((row, index) => {
        row.style.opacity = "0"
        row.style.transform = "translateX(-20px)"
        row.style.animation = `slideInLeft 0.6s ease-out ${1.5 + index * 0.1}s forwards`
    })

    // Add pulse animation to success icon after initial animation
    setTimeout(() => {
        const successIcon = document.querySelector(".success-icon")
        successIcon.style.animation += ", pulse 2s ease-in-out infinite"
    }, 1500)
}

function addInteractiveEffects() {
    // Add hover effects to cards
    const cards = document.querySelectorAll(".order-confirmation-card, .whats-next-card")
    cards.forEach((card) => {
        card.addEventListener("mouseenter", function () {
            this.style.transform = "translateY(-5px)"
            this.style.boxShadow = "0 25px 50px rgba(0, 0, 0, 0.15)"
        })

        card.addEventListener("mouseleave", function () {
            this.style.transform = "translateY(0)"
            this.style.boxShadow = "0 20px 40px rgba(0, 0, 0, 0.1)"
        })
    })

    // Add click effect to buttons
    const buttons = document.querySelectorAll(".btn, .copy-btn, .print-btn")
    buttons.forEach((button) => {
        button.addEventListener("click", function (e) {
            // Create ripple effect
            const ripple = document.createElement("span")
            const rect = this.getBoundingClientRect()
            const size = Math.max(rect.width, rect.height)
            const x = e.clientX - rect.left - size / 2
            const y = e.clientY - rect.top - size / 2

            ripple.style.cssText = `
                position: absolute;
                width: ${size}px;
                height: ${size}px;
                left: ${x}px;
                top: ${y}px;
                background: rgba(255, 255, 255, 0.3);
                border-radius: 50%;
                transform: scale(0);
                animation: ripple 0.6s ease-out;
                pointer-events: none;
            `

            this.style.position = "relative"
            this.style.overflow = "hidden"
            this.appendChild(ripple)

            setTimeout(() => {
                ripple.remove()
            }, 600)
        })
    })
}

function animateParticles() {
    const particles = document.querySelectorAll(".particle")

    particles.forEach((particle, index) => {
        // Randomize particle properties
        const size = Math.random() * 4 + 2
        const left = Math.random() * 100
        const animationDuration = Math.random() * 4 + 6
        const delay = Math.random() * 5

        particle.style.cssText = `
            position: absolute;
            width: ${size}px;
            height: ${size}px;
            left: ${left}%;
            background: rgba(255, 255, 255, ${Math.random() * 0.5 + 0.3});
            border-radius: 50%;
            animation: float ${animationDuration}s infinite linear ${delay}s;
        `
    })
}

// Copy order number to clipboard
function copyOrderNumber() {
    const orderNumber = document.getElementById("orderNumber").textContent

    if (navigator.clipboard) {
        navigator.clipboard.writeText(orderNumber).then(() => {
            showToast()
        })
    } else {
        // Fallback for older browsers
        const textArea = document.createElement("textarea")
        textArea.value = orderNumber
        document.body.appendChild(textArea)
        textArea.select()
        document.execCommand("copy")
        document.body.removeChild(textArea)
        showToast()
    }
}

// Show success toast
function showToast() {
    const toast = document.getElementById("successToast")
    toast.classList.add("show")

    setTimeout(() => {
        toast.classList.remove("show")
    }, 3000)
}

// Continue shopping function
function continueShopping() {
    // Add loading animation
    const button = event.target
    const originalText = button.innerHTML
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Redirecting...'
    button.disabled = true

    // Simulate redirect delay
    setTimeout(() => {
        // In a real application, this would redirect to the shopping page
        window.location.href = "/shop"
    }, 1500)
}

// Download receipt function
function downloadReceipt() {
    const button = event.target
    const originalText = button.innerHTML
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Generating...'
    button.disabled = true

    // Simulate download process
    setTimeout(() => {
        // Create a simple receipt content
        const receiptContent = generateReceiptContent()
        downloadFile("receipt-ORD-2024-001234.txt", receiptContent)

        // Reset button
        button.innerHTML = originalText
        button.disabled = false

        // Show success message
        showCustomToast("Receipt downloaded successfully!", "success")
    }, 2000)
}

// Generate receipt content
function generateReceiptContent() {
    return `
BIDCOMMERCE RECEIPT
==================

Order Number: ORD-2024-001234
Date: August 11, 2025 at 10:55 PM

ITEM DETAILS
------------
Vintage Camera - Canon AE-1
Price: $249.99

PAYMENT INFORMATION
------------------
Payment Method: Credit Card ****1234
Transaction ID: txn_abc123def

SELLER INFORMATION
-----------------
Seller ID: seller_67890

BUYER INFORMATION
----------------
Buyer ID: buyer_12345

TOTAL: $249.99

Thank you for your purchase!
Visit us at bidcommerce.com
    `
}

// Download file helper
function downloadFile(filename, content) {
    const element = document.createElement("a")
    element.setAttribute("href", "data:text/plain;charset=utf-8," + encodeURIComponent(content))
    element.setAttribute("download", filename)
    element.style.display = "none"
    document.body.appendChild(element)
    element.click()
    document.body.removeChild(element)
}

// Show custom toast with different types
function showCustomToast(message, type = "success") {
    const toast = document.createElement("div")
    const icon = type === "success" ? "fa-check-circle" : "fa-info-circle"
    const bgColor = type === "success" ? "#28a745" : "#007bff"

    toast.className = "toast show"
    toast.style.background = bgColor
    toast.innerHTML = `
        <i class="fas ${icon}"></i>
        <span>${message}</span>
    `

    document.body.appendChild(toast)

    setTimeout(() => {
        toast.classList.remove("show")
        setTimeout(() => {
            document.body.removeChild(toast)
        }, 300)
    }, 3000)
}

// Add CSS for additional animations
const additionalStyles = `
    @keyframes slideInLeft {
        from {
            opacity: 0;
            transform: translateX(-20px);
        }
        to {
            opacity: 1;
            transform: translateX(0);
        }
    }
    
    @keyframes ripple {
        to {
            transform: scale(2);
            opacity: 0;
        }
    }
`

// Inject additional styles
const styleSheet = document.createElement("style")
styleSheet.textContent = additionalStyles
document.head.appendChild(styleSheet)

// Add smooth scrolling for any internal links
document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
    anchor.addEventListener("click", function (e) {
        e.preventDefault()
        const target = document.querySelector(this.getAttribute("href"))
        if (target) {
            target.scrollIntoView({
                behavior: "smooth",
                block: "start",
            })
        }
    })
})

// Add keyboard navigation support
document.addEventListener("keydown", (e) => {
    // Press 'C' to copy order number
    if (e.key.toLowerCase() === "c" && !e.ctrlKey && !e.metaKey) {
        copyOrderNumber()
    }

    // Press 'P' to print
    if (e.key.toLowerCase() === "p" && !e.ctrlKey && !e.metaKey) {
        e.preventDefault()
        window.print()
    }

    // Press 'D' to download receipt
    if (e.key.toLowerCase() === "d" && !e.ctrlKey && !e.metaKey) {
        downloadReceipt()
    }
})

// Add loading states and error handling
window.addEventListener("beforeunload", () => {
    // Show loading indicator if page is being unloaded
    document.body.style.opacity = "0.7"
})

// Handle any errors gracefully
window.addEventListener("error", (e) => {
    console.error("An error occurred:", e.error)
    showCustomToast("Something went wrong. Please refresh the page.", "error")
})
