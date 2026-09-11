document.addEventListener('DOMContentLoaded', () => {
    const rejectBtn = document.getElementById('rejectBtn');
    const rejectModalEl = document.getElementById('rejectModal');

    if (rejectBtn && rejectModalEl) {
        rejectBtn.addEventListener('click', () => {
            const modal = new bootstrap.Modal(rejectModalEl);
            modal.show();
        });
    }
});

function submitReject() {
    console.log('submitReject вызван');
    const form = document.getElementById('rejectForm');
    if (!form) {
        console.error('Форма rejectForm не найдена.');
        return;
    }

    const reasonInput = form.querySelector('textarea[name="reason"]');
    const reason = reasonInput ? reasonInput.value.trim() : '';

    if (!reason) {
        alert('Пожалуйста, укажите причину отклонения!');
        return;
    }
    const modalEl = document.getElementById('rejectModal');
    const modal = bootstrap.Modal.getInstance(modalEl);
    if (modal) {
        modal.hide();
    }
    form.submit();
}