function calculatePreview() {
    const serviceSelect = document.getElementById('service-select');
    const quantityInput = document.getElementById('quantity-input');
    const priceDisplay = document.getElementById('preview-price');

    const selectedOption = serviceSelect.options[serviceSelect.selectedIndex];
    const basePrice = parseFloat(selectedOption.getAttribute('data-price')) || 0;

    let quantity = parseFloat(quantityInput.value) || 0;
    if (quantity < 0) quantity = 0;

    const total = basePrice * quantity;

    priceDisplay.textContent = new Intl.NumberFormat('ru-RU').format(total) + ' ₽';

    if (total > 0) {
        priceDisplay.classList.remove('text-muted');
        priceDisplay.classList.add('text-primary', 'fw-bold');
    } else {
        priceDisplay.classList.remove('text-primary', 'fw-bold');
        priceDisplay.classList.add('text-muted');
    }
}
document.getElementById('service-select').addEventListener('change', calculatePreview);

window.addEventListener('load', calculatePreview);