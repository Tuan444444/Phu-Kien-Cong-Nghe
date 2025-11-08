// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Chờ cho toàn bộ trang web tải xong
document.addEventListener("DOMContentLoaded", function () {

    // Tìm thông báo Toast
    var toastEl = document.getElementById('liveToast');

    if (toastEl) {
        // Kiểm tra xem 'bootstrap' đã được tải chưa (an toàn)
        if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {

            // Tạo một đối tượng Toast
            var toast = new bootstrap.Toast(toastEl, {
                delay: 3000 // Tự ẩn sau 3 giây
            });

            // Hiển thị nó
            toast.show();
        }
    }

});