class ImportTabManager {
    constructor(inputId, notifyService) {
        this.inputId = inputId;
        this.notify = notifyService;
        this._input = null; // Cache per l'elemento
        this._eventListener = null; // Cache per l'event listener
        this.initialized = false;
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

    // Setup dell'event listener per aggiornare automaticamente le statistiche
    setupEventListener() {
        if (this.input && !this._eventListener) {
            this._eventListener = () => this.updateStats();
            this.input.addEventListener('input', this._eventListener);
            // Aggiorna le statistiche iniziali
            this.updateStats();
            console.log('✅ Event listener setup per import tab');
        }
    }

    // Aggiorna le statistiche in tempo reale
    updateStats() {
        if (!this.input) {
            console.warn('Input element not available for stats update');
            return;
        }

        const content = this.input.value;

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
        } else {
            console.warn(`Stat element ${elementId} not found`);
        }
    }

    // Reset del cache quando il tab cambia
    resetCache() {
        // Rimuovi l'event listener se esiste
        if (this._input && this._eventListener) {
            this._input.removeEventListener('input', this._eventListener);
            console.log('🗑️ Event listener rimosso');
        }

        this._input = null;
        this._eventListener = null;
        this.initialized = false;
    }

    // Metodo principale per importare le boards
    importBoards() {
        console.log('📥 Tentativo di importazione boards...');

        if (!this.input) {
            this.notify.error('Campo di input non trovato');
            return;
        }

        if (!this.input.value.trim()) {
            this.notify.error('Inserisci del contenuto da importare');
            return;
        }

        try {
            // Divide il contenuto in griglie separate da righe vuote
            const grids = this.input.value
            .split(/\n\s*\n/)
            .filter(grid => grid.trim())
            .map(grid => grid.trim());

            if (grids.length === 0) {
                this.notify.warning('Nessuna griglia valida trovata');
                return;
            }

            // Qui potresti aggiungere la logica di validazione delle griglie
            let validGrids = 0;
            grids.forEach((grid, index) => {
                if (this.validateGrid(grid)) {
                    validGrids++;
                } else {
                    console.warn(`Griglia ${index + 1} non valida:`, grid);
                }
            });

            this.notify.success(`${validGrids}/${grids.length} griglie importate con successo`);

            // Qui potresti triggerare un evento o salvare i dati
            this.onBoardsImported?.(grids);

        } catch (error) {
            console.error('Errore durante l\'importazione:', error);
            this.notify.error('Errore durante l\'importazione delle griglie');
        }
    }

    // Validazione semplice di una griglia
    validateGrid(grid) {
        if (!grid || !grid.trim()) return false;

        const lines = grid.split('\n').filter(line => line.trim());
        if (lines.length === 0) return false;

        // Controlla che tutte le righe abbiano la stessa lunghezza
        const firstLineLength = lines[0].split(/\s+/).length;
        return lines.every(line => line.split(/\s+/).length === firstLineLength);
    }

    clearInput() {
        console.log('🗑️ Tentativo di pulizia input...');

        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }

        this.input.value = '';
        this.updateStats();
        this.notify.success('Campo pulito');

        // Focus sul campo dopo la pulizia
        this.input.focus();
    }

    loadExample() {
        console.log('📋 Caricamento esempio...');

        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }

        // Esempio di griglie BattleSnake
        const exampleContent =
            '👽 💲 💲 ⬛ ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n' +
            '⬛ ⬛ 😈 ⛔ ⬛\n' +
            '💀 ⬛ ⬛ ⛔ ⛔\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n\n' +
            '⬛ ⬛ 😈 ⛔ ⛔\n' +
            '⬛ ⬛ ⬛ ⬛ ⛔\n' +
            '👽 💲 💲 ⬛ ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n' +
            '💀 ⬛ ⬛ ⬛ ⬛\n\n' +
            '😈 ⛔ ⛔ ⛔ ⬛\n' +
            '⬛ ⬛ ⬛ ⛔ ⬛\n' +
            '👽 💲 ⬛ ⬛ ⬛\n' +
            '💲 ⬛ ⬛ 💀 ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛';

        this.input.value = exampleContent;
        this.updateStats();
        this.notify.success('Esempio caricato - 3 griglie disponibili');
    }

    async copyIcon(icon) {
        try {
            await navigator.clipboard.writeText(icon);
            this.notify.success(`Icona ${icon} copiata negli appunti`);
        } catch (error) {
            console.error('Errore durante la copia:', error);
            // Fallback per browser che non supportano clipboard API
            this.fallbackCopyIcon(icon);
        }
    }

    // Fallback per la copia quando clipboard API non è disponibile
    fallbackCopyIcon(icon) {
        const tempInput = document.createElement('input');
        tempInput.value = icon;
        document.body.appendChild(tempInput);
        tempInput.select();
        tempInput.setSelectionRange(0, 99999); // Per mobile

        try {
            document.execCommand('copy');
            this.notify.success(`Icona ${icon} copiata`);
        } catch (error) {
            this.notify.warning(`Impossibile copiare. Usa: ${icon}`);
        } finally {
            document.body.removeChild(tempInput);
        }
    }

    // Metodo per inizializzare manualmente (da chiamare quando il tab viene caricato)
    initialize() {
        console.log('🔧 Inizializzazione ImportTabManager...');

        // Reset del cache per assicurarsi che l'elemento venga ricaricato
        this.resetCache();

        // Aspetta un momento per essere sicuri che il DOM sia pronto
        setTimeout(() => {
            if (this.input) {
                this.setupEventListener();
                this.initialized = true;
                console.log('✅ ImportTabManager inizializzato correttamente');
            } else {
                console.error('❌ Impossibile inizializzare ImportTabManager: elemento input non trovato');
                this.notify.error('Errore durante l\'inizializzazione del tab import');
            }
        }, 50);
    }

    // Callback che può essere impostato dall'esterno per reagire all'importazione
    onBoardsImported(grids) {
        console.log('📊 Boards importate:', grids);
        // Questo metodo può essere sovrascritto o collegato a eventi personalizzati
    }

    // Metodo per verificare se il manager è stato inizializzato
    isInitialized() {
        return this.initialized && this.input !== null;
    }
}