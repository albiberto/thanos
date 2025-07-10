/**
 * BattleSnake Board Converter - Unified Notification System
 * Sistema di notifiche centralizato con Bootstrap Toast
 */

class NotificationService {
    constructor() {
        this.initialized = false;
        this.container = null;
        this.toasts = new Map();
        this.defaultDuration = 4000;
        this.maxToasts = 5;
    }

    /**
     * Inizializza il servizio notifiche
     */
    init() {
        if (this.initialized) return;

        this.createContainer();
        this.initialized = true;
        console.log('✅ NotificationService initialized');
    }

    /**
     * Crea container per i toast
     */
    createContainer() {
        // Cerca container esistente
        this.container = document.getElementById('toastContainer');

        if (!this.container) {
            // Crea nuovo container
            this.container = document.createElement('div');
            this.container.id = 'toastContainer';
            this.container.className = 'toast-container position-fixed top-0 end-0 p-3';
            this.container.style.zIndex = '9999';
            document.body.appendChild(this.container);
        }
    }

    /**
     * Mostra notifica di successo
     */
    success(message, duration = this.defaultDuration) {
        return this.show(message, 'success', duration);
    }

    /**
     * Mostra notifica di errore
     */
    error(message, duration = this.defaultDuration * 1.5) {
        return this.show(message, 'danger', duration);
    }

    /**
     * Mostra notifica di warning
     */
    warning(message, duration = this.defaultDuration) {
        return this.show(message, 'warning', duration);
    }

    /**
     * Mostra notifica informativa
     */
    info(message, duration = this.defaultDuration) {
        return this.show(message, 'info', duration);
    }

    /**
     * Mostra notifica generica
     */
    show(message, type = 'info', duration = this.defaultDuration) {
        if (!this.initialized) {
            console.warn('NotificationService not initialized');
            return null;
        }

        // Limita numero di toast
        this.limitToasts();

        const toastId = this.generateId();
        const toast = this.createToast(toastId, message, type, duration);

        this.container.appendChild(toast);
        this.toasts.set(toastId, toast);

        // Inizializza Bootstrap toast
        const bsToast = new bootstrap.Toast(toast, {
            autohide: duration > 0,
            delay: duration
        });

        // Event listeners
        toast.addEventListener('hidden.bs.toast', () => {
            this.removeToast(toastId);
        });

        // Mostra il toast
        bsToast.show();

        return toastId;
    }

    /**
     * Crea elemento toast
     */
    createToast(id, message, type, duration) {
        const toast = document.createElement('div');
        toast.id = id;
        toast.className = 'toast fade-in';
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');

        const iconMap = {
            success: '✅',
            danger: '❌',
            warning: '⚠️',
            info: 'ℹ️'
        };

        const titleMap = {
            success: 'Successo',
            danger: 'Errore',
            warning: 'Attenzione',
            info: 'Informazione'
        };

        toast.innerHTML = `
            <div class="toast-header bg-${type} text-white">
                <span class="me-2">${iconMap[type] || 'ℹ️'}</span>
                <strong class="me-auto">${titleMap[type] || 'Notifica'}</strong>
                <small class="text-white-50">${this.getTimeStamp()}</small>
                <button type="button" class="btn-close btn-close-white" 
                        data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${this.formatMessage(message)}
            </div>
        `;

        return toast;
    }

    /**
     * Formatta messaggio per supportare HTML semplice
     */
    formatMessage(message) {
        if (typeof message !== 'string') {
            message = String(message);
        }

        // Preserva line breaks
        return message
        .replace(/\n/g, '<br>')
        .replace(/\t/g, '&nbsp;&nbsp;&nbsp;&nbsp;');
    }

    /**
     * Genera timestamp per il toast
     */
    getTimeStamp() {
        const now = new Date();
        return now.toLocaleTimeString('it-IT', {
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    /**
     * Genera ID univoco
     */
    generateId() {
        return `toast-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Rimuove toast specifico
     */
    removeToast(toastId) {
        const toast = this.toasts.get(toastId);
        if (toast) {
            toast.remove();
            this.toasts.delete(toastId);
        }
    }

    /**
     * Limita numero di toast visibili
     */
    limitToasts() {
        if (this.toasts.size >= this.maxToasts) {
            // Rimuovi il toast più vecchio
            const oldestId = this.toasts.keys().next().value;
            const oldestToast = this.toasts.get(oldestId);

            if (oldestToast) {
                const bsToast = bootstrap.Toast.getInstance(oldestToast);
                if (bsToast) {
                    bsToast.hide();
                } else {
                    this.removeToast(oldestId);
                }
            }
        }
    }

    /**
     * Nasconde tutti i toast
     */
    hideAll() {
        this.toasts.forEach((toast, id) => {
            const bsToast = bootstrap.Toast.getInstance(toast);
            if (bsToast) {
                bsToast.hide();
            }
        });
    }

    /**
     * Rimuove tutti i toast
     */
    clear() {
        this.toasts.forEach((toast, id) => {
            toast.remove();
        });
        this.toasts.clear();
    }

    /**
     * Mostra notifica persistente (senza auto-hide)
     */
    persistent(message, type = 'info') {
        return this.show(message, type, 0); // duration = 0 disabilita auto-hide
    }

    /**
     * Mostra loading toast
     */
    loading(message = 'Caricamento in corso...') {
        const loadingMessage = `
            <div class="d-flex align-items-center">
                <div class="spinner-border spinner-border-sm me-2" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                ${message}
            </div>
        `;

        return this.show(loadingMessage, 'info', 0);
    }

    /**
     * Aggiorna toast esistente
     */
    update(toastId, message, type = null) {
        const toast = this.toasts.get(toastId);
        if (!toast) return false;

        const bodyElement = toast.querySelector('.toast-body');
        if (bodyElement) {
            bodyElement.innerHTML = this.formatMessage(message);
        }

        // Aggiorna tipo se specificato
        if (type) {
            const header = toast.querySelector('.toast-header');
            if (header) {
                // Rimuovi classi precedenti
                header.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info');
                header.classList.add(`bg-${type}`);
            }
        }

        return true;
    }

    /**
     * Mostra notifica con pulsanti custom
     */
    showWithActions(message, actions = [], type = 'info') {
        const toastId = this.generateId();
        const toast = this.createToast(toastId, message, type, 0); // Persistente

        // Aggiungi pulsanti al body
        const bodyElement = toast.querySelector('.toast-body');
        const actionsHtml = actions.map(action =>
            `<button class="btn btn-sm btn-outline-${type} me-2" 
                     onclick="${action.handler}">${action.label}</button>`
        ).join('');

        bodyElement.innerHTML += `<div class="mt-2">${actionsHtml}</div>`;

        this.container.appendChild(toast);
        this.toasts.set(toastId, toast);

        const bsToast = new bootstrap.Toast(toast, { autohide: false });

        toast.addEventListener('hidden.bs.toast', () => {
            this.removeToast(toastId);
        });

        bsToast.show();
        return toastId;
    }

    /**
     * Ottieni statistiche notifiche
     */
    getStats() {
        return {
            activeToasts: this.toasts.size,
            maxToasts: this.maxToasts,
            initialized: this.initialized
        };
    }
}

// === LEGACY STATUS MESSAGE SUPPORT ===
class LegacyStatusService {
    /**
     * Mostra status message nel vecchio container
     */
    static showStatus(message, isError = false) {
        const statusDiv = document.getElementById('statusMessage');
        if (!statusDiv) return;

        const className = isError ? 'alert alert-danger' : 'alert alert-success';
        statusDiv.innerHTML = `<div class="${className} fade-in">${message}</div>`;

        setTimeout(() => {
            statusDiv.innerHTML = '';
        }, 3000);
    }
}

// === INITIALIZATION ===
const notificationService = new NotificationService();

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        notificationService.init();
    });
} else {
    notificationService.init();
}

// === GLOBAL EXPORTS ===
window.NotifyService = notificationService;
window.BattleSnakeNotify = notificationService;

// Legacy compatibility
window.showStatus = LegacyStatusService.showStatus;