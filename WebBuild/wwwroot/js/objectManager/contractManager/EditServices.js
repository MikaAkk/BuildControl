document.getElementById('newServiceSelect')?.addEventListener('change', function () {
    var opt = this.selectedOptions[0];
    if (opt) {
        var priceInput = document.getElementById('newPrice');
        priceInput.value = opt.dataset.price || '';
        priceInput.readOnly = false;
    }
});

function recalcTotals() {
    var grandTotal = 0;
    document.querySelectorAll('.qty-input').forEach(function (qtyInput, i) {
        var priceInput = document.querySelectorAll('.price-input')[i];
        var totalCell = document.querySelectorAll('.total-cell')[i];
        var row = qtyInput.closest('tr');
        var delCheckbox = row?.querySelector('.delete-checkbox');
        if (delCheckbox && delCheckbox.checked) {
            totalCell.textContent = '—';
            return;
        }

        var qty = parseFloat(qtyInput.value) || 0;
        var price = parseFloat(priceInput.value) || 0;
        var total = qty * price;
        totalCell.textContent = total.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        grandTotal += total;
    });
    var gt = document.getElementById('grandTotal');
    if (gt) gt.textContent = grandTotal.toLocaleString('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

document.querySelectorAll('.qty-input, .price-input').forEach(function (el) {
    el.addEventListener('input', recalcTotals);
});

document.querySelectorAll('.delete-checkbox').forEach(function (cb) {
    cb.addEventListener('change', function () {
        var row = this.closest('tr');
        if (this.checked) {
            row.classList.add('text-decoration-line-through', 'text-muted');
        } else {
            row.classList.remove('text-decoration-line-through', 'text-muted');
        }
        recalcTotals();
    });
});

recalcTotals();