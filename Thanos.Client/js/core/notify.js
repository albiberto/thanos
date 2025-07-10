class NotifyService {
    constructor(toastElement, toastBody, defaultOptions) {
        this.toastElement = toastElement;
        this.toastBody = toastBody;
        this.toast = new bootstrap.Toast(this.toastElement);

        // Opzioni di default
        this.defaultOptions = defaultOptions !== undefined
            ? defaultOptions
            : {
                delay: 4000,
                autohide: true
            };
    }

    // Metodo principale per mostrare notifiche
    show(message, type = 'info', options = {}) {
        // Merge delle opzioni
        const config = {...this.defaultOptions, ...options};

        // Aggiorna il messaggio
        this.toastBody.textContent = message;

        // Aggiorna lo stile in base al tipo
        this.updateToastStyle(type);

        // Configura il toast
        this.toast._config.delay = config.delay;
        this.toast._config.autohide = config.autohide;

        // Mostra il toast
        this.toast.show();
    }

    // Metodi di convenienza per diversi tipi di notifica
    success(message, options = {}) {
        this.show(message, 'success', options);
    }

    error(message, options = {}) {
        this.show(message, 'error', {...options, delay: 0, autohide: false});
    }

    warning(message, options = {}) {
        this.show(message, 'warning', options);
    }

    info(message, options = {}) {
        this.show(message, 'info', options);
    }

    // Aggiorna lo stile del toast in base al tipo
    updateToastStyle(type) {
        // Rimuovi classi esistenti
        this.toastElement.classList.remove('bg-success', 'bg-danger', 'bg-warning', 'bg-info');

        // Aggiungi la classe appropriata
        switch (type) {
            case 'success':
                this.toastElement.classList.add('bg-success');
                break;
            case 'error':
                this.toastElement.classList.add('bg-danger');
                break;
            case 'warning':
                this.toastElement.classList.add('bg-warning');
                break;
            case 'info':
            default:
                this.toastElement.classList.add('bg-info');
                break;
        }
    }

    // Nascondi il toast manualmente
    hide() {
        this.toast.hide();
    }
}