/**
 * NotificationService.js - Gestisce le notifiche toast dell'applicazione
 */

export class NotificationService {
    constructor(toastElementId) {
        this.toastElement = document.getElementById(toastElementId);

        if (!this.toastElement) {
            console.error(`Toast element '${toastElementId}' not found`);
            this.fallbackMode = true;
            return;
        }

        this.toastBody = this.toastElement.querySelector('.toast-body');
        this.toastHeader = this.toastElement.querySelector('.toast-header strong');

        try {
            this.bootstrapToast = new bootstrap.Toast(this.toastElement, {
                autohide: true,
                delay: 3000
            });
            this.fallbackMode = false;
        } catch (error) {
            console.error('Error initializing Bootstrap Toast:', error);
            this.fallbackMode = true;
        }
    }

    /**
     * Mostra una notifica
     */
    show(message, type = 'info', title = 'Notifica') {
        if (this.fallbackMode) {
            console.log(`[${type.toUpperCase()}] ${message}`);
            return;
        }

        try {
            // Aggiorna contenuto
            this.toastBody.textContent = message;
            if (this.toastHeader) {
                this.toastHeader.textContent = title;
            }

            // Rimuovi classi precedenti
            this.toastElement.className = this.toastElement.className
            .replace(/bg-\w+/g, '')
            .replace(/text-\w+/g, '');

            // Aggiungi nuove classi
            const classMap = {
                success: ['bg-success', 'text-white'],
                error: ['bg-danger', 'text-white'],
                warning: ['bg-warning', 'text-dark'],
                info: ['bg-info', 'text-white']
            };

            const classes = classMap[type] || classMap.info;
            this.toastElement.classList.add(...classes);

            // Mostra toast
            this.bootstrapToast.show();

        } catch (error) {
            console.error('Error showing toast:', error);
            alert(`${title}: ${message}`);
        }
    }

    success(message) {
        this.show(message, 'success', '✅ Successo');
    }

    error(message) {
        this.show(message, 'error', '❌ Errore');
    }

    warning(message) {
        this.show(message, 'warning', '⚠️ Attenzione');
    }

    info(message) {
        this.show(message, 'info', 'ℹ️ Info');
    }
}