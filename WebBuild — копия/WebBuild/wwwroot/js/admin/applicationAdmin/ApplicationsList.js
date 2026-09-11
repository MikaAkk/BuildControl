
document.addEventListener('DOMContentLoaded', function () {
    const selectAllCheckbox = document.getElementById('selectAll');
    const checkboxes = document.querySelectorAll('.app-checkbox');
    const bulkBtn = document.getElementById('bulkBtn');
    const bulkForm = document.getElementById('bulkForm');


    if (checkboxes.length === 0) return;

    if (selectAllCheckbox) {
        selectAllCheckbox.addEventListener('change', function () {
            checkboxes.forEach(cb => cb.checked = this.checked);
            toggleBulkButton();
        });
    }

    checkboxes.forEach(cb => {
        cb.addEventListener('change', toggleBulkButton);
    });

    function toggleBulkButton() {
        const isAnyChecked = Array.from(checkboxes).some(cb => cb.checked);
        if (bulkBtn) {
            bulkBtn.disabled = !isAnyChecked;
        }
    }

    if (bulkForm) {
        bulkForm.addEventListener('submit', function (e) {
            const managerSelect = bulkForm.querySelector('select[name="managerId"]');

            if (!managerSelect || !managerSelect.value) {
                e.preventDefault();
                alert('Пожалуйста, выберите менеджера для назначения.');
                return false;
            }

            const selectedCount = Array.from(checkboxes).filter(cb => cb.checked).length;

            return confirm(`Вы действительно хотите назначить выбранного менеджера на ${selectedCount} заявок?`);
        });
    }
});
