document.addEventListener("DOMContentLoaded", () => {
    // Payment method selection
    const paymentOptions = document.querySelectorAll(".method-option")
    const cardDetails = document.getElementById("cardDetails")

    paymentOptions.forEach((option) => {
        option.addEventListener("click", function () {
            // Remove active class from all options
            paymentOptions.forEach((opt) => opt.classList.remove("active"))

            // Add active class to clicked option
            this.classList.add("active")

            // Show/hide card details based on selection
            const paymentType = this.querySelector("input").value
            if (paymentType === "card") {
                cardDetails.style.display = "block"
            } else {
                cardDetails.style.display = "none"
            }
        })
    })

    // Card number formatting
    const cardNumberInput = document.querySelector('input[placeholder="1234 5678 9012 3456"]')
    if (cardNumberInput) {
        cardNumberInput.addEventListener("input", (e) => {
            const value = e.target.value.replace(/\s/g, "").replace(/[^0-9]/gi, "")
            const formattedValue = value.match(/.{1,4}/g)?.join(" ") || value
            e.target.value = formattedValue
        })
    }

    // Expiry date formatting
    const expiryInput = document.querySelector('input[placeholder="MM/YY"]')
    if (expiryInput) {
        expiryInput.addEventListener("input", (e) => {
            let value = e.target.value.replace(/\D/g, "")
            if (value.length >= 2) {
                value = value.substring(0, 2) + "/" + value.substring(2, 4)
            }
            e.target.value = value
        })
    }

    // CVV input restriction
    const cvvInput = document.querySelector('input[placeholder="123"]')
    if (cvvInput) {
        cvvInput.addEventListener("input", (e) => {
            e.target.value = e.target.value.replace(/[^0-9]/g, "")
        })
    }

    // Form submission
    const checkoutForm = document.getElementById("checkoutForm")
    const loadingOverlay = document.getElementById("loadingOverlay")

    checkoutForm.addEventListener("submit", (e) => {
        e.preventDefault()

        // Show loading overlay
        loadingOverlay.style.display = "flex"

        // Simulate payment processing
        setTimeout(() => {
            // Hide loading overlay
            loadingOverlay.style.display = "none"

            // Redirect to success page
            window.location.href = "payment-success.html"
        }, 3000)
    })

    // Dynamic background color changes
    function changeBackgroundColors() {
        const shapes = document.querySelectorAll(".bg-shape")
        const colors = [
            "linear-gradient(45deg, rgba(79, 70, 229, 0.3), rgba(147, 51, 234, 0.3))",
            "linear-gradient(45deg, rgba(255, 107, 53, 0.3), rgba(236, 72, 153, 0.3))",
            "linear-gradient(45deg, rgba(34, 197, 94, 0.3), rgba(59, 130, 246, 0.3))",
            "linear-gradient(45deg, rgba(168, 85, 247, 0.3), rgba(236, 72, 153, 0.3))",
            "linear-gradient(45deg, rgba(59, 130, 246, 0.3), rgba(16, 185, 129, 0.3))",
        ]

        shapes.forEach((shape, index) => {
            const randomColor = colors[Math.floor(Math.random() * colors.length)]
            shape.style.background = randomColor
        })
    }

    // Change background colors every 10 seconds
    setInterval(changeBackgroundColors, 10000)

    // Form validation
    const inputs = document.querySelectorAll("input[required]")
    inputs.forEach((input) => {
        input.addEventListener("blur", function () {
            if (this.value.trim() === "") {
                this.style.borderColor = "#EF4444"
            } else {
                this.style.borderColor = "#10B981"
            }
        })
    })

    // Add smooth scrolling for mobile
    if (window.innerWidth <= 768) {
        document.body.style.scrollBehavior = "smooth"
    }
})

// Add some interactive effects
document.addEventListener("mousemove", (e) => {
    const shapes = document.querySelectorAll(".bg-shape")
    const mouseX = e.clientX / window.innerWidth
    const mouseY = e.clientY / window.innerHeight

    shapes.forEach((shape, index) => {
        const speed = (index + 1) * 0.5
        const x = (mouseX - 0.5) * speed
        const y = (mouseY - 0.5) * speed

        shape.style.transform += ` translate(${x}px, ${y}px)`
    })
})
