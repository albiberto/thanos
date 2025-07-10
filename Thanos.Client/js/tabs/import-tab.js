class ImportTabManager {
    constructor(inputId, notifyService) {
        this.inputId = inputId;
        this.notify = notifyService;
        this._input = null; // Cache per l'elemento
    }

    // Getter che trova l'elemento quando necessario
    get input() {
        if (!this._input) {
            this._input = document.getElementById(this.inputId);
            if (!this._input) {
                console.warn(`Element ${this.inputId} not found`);
            }
        }
        return this._input;
    }

    // Reset del cache quando il tab cambia
    resetCache() {
        this._input = null;
    }

    importBoards() {
        if (!this.input || !this.input.value.trim()) {
            this.notify.error('Inserisci del contenuto');
            return;
        }

        const grids = this.input.value.split('\n\n').filter(g => g.trim());
        this.notify.success(`${grids.length} griglie importate`);
    }

    clearInput() {
        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }
        this.input.textContent = '';
        this.notify.success('Campo pulito');
    }

    loadExample() {
        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }

        this.input.textContent = 
            '👽 💲 💲 ⬛ ⬛' + '\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛' + '\n' +
            '⬛ ⬛ 😈 ⛔ ⬛' + '\n' +
            '💀 ⬛ ⬛ ⛔ ⛔' + '\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛' + '\n' +
            '⬛ ⬛ 😈 ⛔ ⛔';
        
        this.notify.success('Esempio caricato');
    }

    copyIcon(icon) {
        navigator.clipboard.writeText(icon);
        this.notify.success(`Icona ${icon} copiata`);
    }
}