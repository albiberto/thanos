class ImportTabManager {
    constructor(inputId, notifyService) {
        this.inputId = inputId;
        this.notify = notifyService;
        this._input = null; // Cache per l'elemento
        this._eventListener = null; // Cache per l'event listener
    }

    // Getter che trova l'elemento quando necessario
    get input() {
        if (!this._input) {
            this._input = document.getElementById(this.inputId);
            if (!this._input) {
                console.warn(`Element ${this.inputId} not found`);
            } else {
                // Aggiungi l'event listener quando l'elemento viene trovato
                this.setupEventListener();
            }
        }
        return this._input;
    }

    // Setup dell'event listener per aggiornare automaticamente le statistiche
    setupEventListener() {
        if (this._input && !this._eventListener) {
            this._eventListener = () => this.updateStats();
            this._input.addEventListener('input', this._eventListener);
            // Aggiorna le statistiche iniziali
            this.updateStats();
        }
    }

    // Aggiorna le statistiche in tempo reale
    updateStats() {
        if (!this._input) return;

        const content = this._input.value;

        // Calcola righe
        const lines = content ? content.split('\n').length : 0;

        // Calcola caratteri
        const chars = content.length;

        // Calcola griglie (separate da due o più newline consecutive)
        const grids = content.trim() ?
            content.split(/\n\s*\n/).filter(grid => grid.trim()).length : 0;

        // Aggiorna i div delle statistiche
        this.updateStatElement('lineCount', lines);
        this.updateStatElement('charCount', chars);
        this.updateStatElement('gridCount', grids);
    }

    // Helper per aggiornare un elemento delle statistiche
    updateStatElement(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = value;
        }
    }

    // Reset del cache quando il tab cambia
    resetCache() {
        // Rimuovi l'event listener se esiste
        if (this._input && this._eventListener) {
            this._input.removeEventListener('input', this._eventListener);
        }

        this._input = null;
        this._eventListener = null;
    }

    importBoards() {
        if (!this.input || !this.input.value.trim()) {
            this.notify.error('Inserisci del contenuto');
            return;
        }

        const grids = this.input.value.split(/\n\s*\n/).filter(g => g.trim());
        this.notify.success(`${grids.length} griglie importate`);
    }

    clearInput() {
        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }
        this.input.value = ''; // Usa .value invece di .textContent per i textarea
        this.updateStats(); // Aggiorna le statistiche dopo aver pulito
        this.notify.success('Campo pulito');
    }

    loadExample() {
        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }

        this.input.value =
            '👽 💲 💲 ⬛ ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n' +
            '⬛ ⬛ 😈 ⛔ ⬛\n' +
            '💀 ⬛ ⬛ ⛔ ⛔\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n\n' +
            '⬛ ⬛ 😈 ⛔ ⛔\n' +
            '⬛ ⬛ ⬛ ⬛ ⛔\n' +
            '👽 💲 💲 ⬛ ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n' +
            '💀 ⬛ ⬛ ⬛ ⬛';

        this.updateStats(); // Aggiorna le statistiche dopo aver caricato l'esempio
        this.notify.success('Esempio caricato');
    }

    copyIcon(icon) {
        navigator.clipboard.writeText(icon);
        this.notify.success(`Icona ${icon} copiata`);
    }

    // Metodo per inizializzare manualmente (da chiamare quando il tab viene caricato)
    initialize() {
        // Reset del cache per assicurarsi che l'elemento venga ricaricato
        this.resetCache();
        // Accedi al getter per triggerare il setup
        const input = this.input;
        if (input) {
            console.log('ImportTabManager inizializzato correttamente');
        }
    }
}