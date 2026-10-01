document.querySelectorAll('.emp-checkbox').forEach(function (cb) {
    cb.addEventListener('change', function () {
        var row = this.closest('tr');
        var badge = row.querySelector('span.badge');
        if (this.checked) {
            row.classList.add('table-success');
            if (badge) {
                badge.className = 'badge bg-success';
                badge.textContent = 'Назначен';
            }
        } else {
            row.classList.remove('table-success');
            if (badge) {
                badge.className = 'badge bg-secondary';
                badge.textContent = 'Свободен';
            }
        }
    });
});