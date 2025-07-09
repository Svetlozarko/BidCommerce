let currentStep = 1;
const totalSteps = 4;

function changeStep(direction) {
    const newStep = currentStep + direction;

    if (newStep < 1 || newStep > totalSteps) return;

    // Validate current step before proceeding
    if (direction > 0 && !validateCurrentStep()) {
        return;
    }

    // Hide current step
    document.getElementById(`step${currentStep}`).classList.remove('active');

    // Update step indicators
    updateStepIndicator(currentStep, newStep);

    // Show new step
    currentStep = newStep;
    document.getElementById(`step${currentStep}`).classList.add('active');

    // Update navigation buttons
    updateNavigationButtons();
}

function updateStepIndicator(oldStep, newStep) {
    // Mark previous steps as completed
    for (let i = 1; i < newStep; i++) {
        document.getElementById(`step${i}-circle`).className = 'step-circle completed';
        const line = document.getElementById(`step${i}-line`);
        if (line) line.className = 'step-line completed';
    }

    // Mark current step as active
    document.getElementById(`step${newStep}-circle`).className = 'step-circle active';

    // Mark future steps as inactive
    for (let i = newStep + 1; i <= totalSteps; i++) {
        document.getElementById(`step${i}-circle`).className = 'step-circle inactive';
    }
}

function updateNavigationButtons() {
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');
    const submitBtn = document.getElementById('submitBtn');

    // Show/hide previous button
    prevBtn.style.display = currentStep > 1 ? 'block' : 'none';

    // Show/hide next/submit buttons
    if (currentStep === totalSteps) {
        nextBtn.style.display = 'none';
        submitBtn.style.display = 'block';
    } else {
        nextBtn.style.display = 'block';
        submitBtn.style.display = 'none';
    }
}

function validateCurrentStep() {
    const currentStepElement = document.getElementById(`step${currentStep}`);
    const inputs = currentStepElement.querySelectorAll('input[required], input[data-val-required]');
    let isValid = true;

    inputs.forEach(input => {
        if (!input.value.trim()) {
            input.style.borderColor = '#dc2626';
            isValid = false;
        } else {
            input.style.borderColor = '#d1d5db';
        }
    });

    return isValid;
}

function previewImage(input) {
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function (e) {
            document.getElementById('profilePhoto').src = e.target.result;
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// Initialize navigation buttons
updateNavigationButtons();