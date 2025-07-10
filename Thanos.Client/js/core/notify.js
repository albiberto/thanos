class NotifyService {
    constructor(toastId) {
        this.toast = document.getElementById(toastId);
        this.body = this.toast.querySelector('.toast-body');
        this.bootstrapToast = new bootstrap.Toast(this.toast);
    }

    show(message, type = 'info') {
        this.body.textContent = message;
        this.toast.className = `toast bg-${type} text-white`;
        this.bootstrapToast.show();
    }

    success(msg) { this.show(msg, 'success'); }
    error(msg) { this.show(msg, 'danger'); }
    info(msg) { this.show(msg, 'info'); }
}