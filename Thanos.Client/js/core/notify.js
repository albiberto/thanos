class NotifyService {
    constructor(toastId) {
        this.toastElement = document.getElementById(toastId);

        if (!this.toastElement) {
            console.error(`Toast element with id '${toastId}' not found`);
            return;
        }

        this.toastBody = this.toastElement.querySelector('.toast-body');
        this.toastHeader = this.toastElement.querySelector('.toast-header strong');

        if (!this.toastBody) {
            console.error('Toast body not found');
            return;
        }

        try {
            this.bootstrapToast = new bootstrap.Toast(this.toastElement, {
                autohide: true,
                delay: 3000
            });
        } catch (error) {
            console.error('Error initializing Bootstrap Toast:', error);
        }
    }

    show(message, type = 'info', title = 'Notifica') {
        if (!this.toastBody || !this.bootstrapToast) {
            console.warn('Toast not properly initialized, falling back to console:', message);
            console.log(`[${type.toUpperCase()}] ${message}`);
            return;
        }

        try {
            // Aggiorna il contenuto
            this.toastBody.textContent = message;

            // Aggiorna il titolo se esiste
            if (this.toastHeader) {
                this.toastHeader.textContent = title;
            }

            // Rimuovi classi precedenti e aggiungi la nuova
            this.toastElement.className = this.toastElement.className
            .replace(/bg-\w+/g, '')
            .replace(/text-\w+/g, '');

            // Aggiungi le nuove classi in base al tipo
            switch (type) {
                case 'success':
                    this.toastElement.classList.add('bg-success', 'text-white');
                    break;
                case 'danger':
                case 'error':
                    this.toastElement.classList.add('bg-danger', 'text-white');
                    break;
                case 'warning':
                    this.toastElement.classList.add('bg-warning', 'text-dark');
                    break;
                case 'info':
                default:
                    this.toastElement.classList.add('bg-info', 'text-white');
                    break;
            }

            // Mostra il toast
            this.bootstrapToast.show();

        } catch (error) {
            console.error('Error showing toast:', error);
            // Fallback
            alert(`${title}: ${message}`);
        }
    }

    success(message, title = '✅ Successo') {
        this.show(message, 'success', title);
    }

    error(message, title = '❌ Errore') {
        this.show(message, 'error', title);
    }

    warning(message, title = '⚠️ Attenzione') {
        this.show(message, 'warning', title);
    }

    info(message, title = 'ℹ️ Informazione') {
        this.show(message, 'info', title);
    }

    // Metodo per nascondere il toast se necessario
    hide() {
        if (this.bootstrapToast) {
            this.bootstrapToast.hide();
        }
    }
}