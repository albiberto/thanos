class ImportTabManager {
    constructor(inputId, notifyService) {
        this.inputId = inputId;
        this.notify = notifyService;
        this._input = null;
        this._eventListener = null;
        this.initialized = false;

        // Caratteri validi per le griglie BattleSnake
        this.VALID_CHARACTERS = ['👽', '💲', '😈', '⛔', '🍎', '💀', '⬛', '⬆️', '⬇️', '⬅️', '➡️', ' ', '\t', '\n', '\r'];
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
        const lines = content ? content.split('\n').length : 0;
        const chars = content.length;
        const grids = content.trim() ? content.split(/\n\s*\n/).filter(grid => grid.trim()).length : 0;

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
        if (this._input && this._eventListener) {
            this._input.removeEventListener('input', this._eventListener);
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
            // Validazione caratteri
            const invalidChars = this.findInvalidCharacters(this.input.value);
            if (invalidChars.length > 0) {
                this.notify.error(`Caratteri non validi: ${invalidChars.join(', ')}`);
                return;
            }

            // Parsing delle griglie
            const grids = this.input.value
            .split(/\n\s*\n/)
            .filter(grid => grid.trim())
            .map(grid => grid.trim());

            if (grids.length === 0) {
                this.notify.warning('Nessuna griglia valida trovata');
                return;
            }

            // Validazione base delle griglie
            let validGrids = 0;
            for (let i = 0; i < grids.length; i++) {
                if (this.validateGrid(grids[i])) {
                    validGrids++;
                } else {
                    this.notify.error(`Griglia ${i + 1} non valida: righe di lunghezza diversa`);
                    return;
                }
            }

            this.notify.success(`${validGrids} griglie importate con successo`);
            this.onBoardsImported?.(grids);

        } catch (error) {
            console.error('Errore durante l\'importazione:', error);
            this.notify.error('Errore durante l\'importazione delle griglie');
        }
    }

    // Trova caratteri non validi
    findInvalidCharacters(content) {
        const invalidChars = new Set();
        for (const char of content) {
            if (!this.VALID_CHARACTERS.includes(char)) {
                invalidChars.add(char);
            }
        }
        return Array.from(invalidChars);
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
        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }

        this.input.value = '';
        this.updateStats();
        this.notify.success('Campo pulito');
        this.input.focus();
    }

    loadExample() {
        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }

        const exampleContent =
            '👽 💲 💲 ⬛ ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ 🍎\n' +
            '⬛ ⬛ 😈 ⛔ ⬛\n' +
            '💀 ⬛ ⬛ ⛔ ⛔\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n\n' +
            '⬛ ⬛ 😈 ⛔ ⛔\n' +
            '⬛ ⬛ ⬛ ⬛ ⛔\n' +
            '👽 💲 💲 🍎 ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n' +
            '💀 ⬛ ⬛ ⬛ ⬛';

        this.input.value = exampleContent;
        this.updateStats();
        this.notify.success('Esempio caricato');
    }

    async copyIcon(icon) {
        try {
            await navigator.clipboard.writeText(icon);
            this.notify.success(`Icona ${icon} copiata`);
        } catch (error) {
            // Fallback
            const tempInput = document.createElement('input');
            tempInput.value = icon;
            document.body.appendChild(tempInput);
            tempInput.select();

            try {
                document.execCommand('copy');
                this.notify.success(`Icona ${icon} copiata`);
            } catch (e) {
                this.notify.warning(`Usa: ${icon}`);
            } finally {
                document.body.removeChild(tempInput);
            }
        }
    }

    // Metodo per inizializzare manualmente
    initialize() {
        this.resetCache();
        setTimeout(() => {
            if (this.input) {
                this.setupEventListener();
                this.initialized = true;
                console.log('✅ ImportTabManager inizializzato');
            } else {
                console.error('❌ Elemento input non trovato');
            }
        }, 50);
    }

    // Callback per boards importate
    onBoardsImported(grids) {
        console.log('📊 Boards importate:', grids);
    }

    isInitialized() {
        return this.initialized && this.input !== null;
    }
}