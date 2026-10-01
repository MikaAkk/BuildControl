function openDeleteModal(id, name) {
    document.getElementById('deleteId').value = id;
    document.getElementById('deleteName').innerText = name;
    document.getElementById('deleteModal').style.display = 'flex';
}

function closeDeleteModal() {
    document.getElementById('deleteModal').style.display = 'none';
}

// Закрытие по клику вне окна
document.addEventListener('DOMContentLoaded', function () {
    var modal = document.getElementById('deleteModal');
    modal.addEventListener('click', function (e) {
        if (e.target === modal) {
            closeDeleteModal();
        }
    });
});