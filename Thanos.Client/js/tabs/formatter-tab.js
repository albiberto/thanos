class FormatterTabManager {
    constructor(inputId, notifyService) {
        this.inputId = inputId;
        this.notify = notifyService;
        this._input = null;
        this._eventListener = null;
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
            this.updateStats();
            console.log('✅ Event listener setup per formatter tab');
        }
    }

    // Aggiorna le statistiche in tempo reale
    updateStats() {
        if (!this.input) {
            console.warn('Input element not available for stats update');
            return;
        }

        const input = this.input.value || '';
        const lines = input.split('\n').length;
        const chars = input.length;
        let itemCount = 0;

        try {
            if (input.trim()) {
                const parsed = JSON.parse(input);
                itemCount = Array.isArray(parsed) ? parsed.length : 1;
            }
        } catch (error) {
            // Invalid JSON, keep count at 0
        }

        this.updateStatElement('jsonLineCount', lines);
        this.updateStatElement('jsonCharCount', chars);
        this.updateStatElement('jsonItemCount', itemCount);
        this.updateStatElement('jsonStatus', input.trim() ? 'Content present' : 'Empty');
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

    // Format JSON with inline body per Battlesnake
    formatJSON() {
        console.log('📝 Tentativo di formattazione JSON...');

        if (!this.input) {
            this.notify.error('Campo di input non trovato');
            return;
        }

        if (!this.input.value.trim()) {
            this.notify.error('Inserisci del JSON nel campo di input');
            return;
        }

        try {
            // Parse JSON
            const jsonList = JSON.parse(this.input.value);

            if (!Array.isArray(jsonList)) {
                throw new Error('Input deve essere un array di oggetti JSON');
            }

            // Format each item with inline body per Battlesnake
            const formattedList = jsonList.map(item => this.formatItemWithInlineBody(item));

            // Generate output
            const formattedJSON = JSON.stringify(formattedList, null, 2);

            // Show output
            this.showOutput(formattedJSON);

            this.notify.success(`${formattedList.length} elementi formattati con successo`);

        } catch (error) {
            console.error('Format error:', error);
            this.notify.error(`Errore nella formattazione: ${error.message}`);
        }
    }

    // Format item with inline body for Battlesnake tests
    formatItemWithInlineBody(item) {
        if (!item.MoveRequest?.board?.snakes) {
            return item;
        }

        const formatted = JSON.parse(JSON.stringify(item)); // Deep clone

        // Format snakes with inline body
        formatted.MoveRequest.board.snakes = formatted.MoveRequest.board.snakes.map(snake => ({
            ...snake,
            body: snake.body ? JSON.stringify(snake.body) : "[]"
        }));

        // Format 'you' snake if exists
        if (formatted.MoveRequest.you?.body) {
            formatted.MoveRequest.you.body = JSON.stringify(formatted.MoveRequest.you.body);
        }

        return formatted;
    }

    // Validate JSON
    validateJSON() {
        console.log('🔍 Validazione JSON...');

        if (!this.input) {
            this.notify.error('Campo di input non trovato');
            return;
        }

        if (!this.input.value.trim()) {
            this.notify.warning('Nessun contenuto da validare');
            return;
        }

        try {
            const parsed = JSON.parse(this.input.value);
            const isArray = Array.isArray(parsed);
            const itemCount = isArray ? parsed.length : 1;

            this.updateValidationStatus(
                `JSON valido - ${itemCount} elemento${itemCount !== 1 ? 'i' : ''}`,
                'success'
            );

            this.notify.success('JSON valido');

        } catch (error) {
            this.updateValidationStatus(`JSON non valido: ${error.message}`, 'error');
            this.notify.error('JSON non valido');
        }
    }

    // Prettify JSON
    prettifyJSON() {
        console.log('🎨 Prettify JSON...');

        if (!this.input) {
            this.notify.error('Campo di input non trovato');
            return;
        }

        if (!this.input.value.trim()) {
            this.notify.error('Nessun contenuto da formattare');
            return;
        }

        try {
            const parsed = JSON.parse(this.input.value);
            const prettified = JSON.stringify(parsed, null, 2);
            this.input.value = prettified;
            this.updateStats();
            this.notify.success('JSON formattato');

        } catch (error) {
            this.notify.error('JSON non valido per la formattazione');
        }
    }

    // Minify JSON
    minifyJSON() {
        console.log('📦 Minify JSON...');

        if (!this.input) {
            this.notify.error('Campo di input non trovato');
            return;
        }

        if (!this.input.value.trim()) {
            this.notify.error('Nessun contenuto da comprimere');
            return;
        }

        try {
            const parsed = JSON.parse(this.input.value);
            const minified = JSON.stringify(parsed);
            this.input.value = minified;
            this.updateStats();
            this.notify.success('JSON compresso');

        } catch (error) {
            this.notify.error('JSON non valido per la compressione');
        }
    }

    // Clear input
    clearInput() {
        console.log('🗑️ Clear input...');

        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }

        this.input.value = '';
        this.updateStats();
        this.clearValidationStatus();
        this.hideOutput();
        this.input.focus();
        this.notify.success('Campo pulito');
    }

    // Load example JSON
    loadExample() {
        console.log('📄 Caricamento esempio...');

        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }

        const exampleJSON = `[
  {
    "Id": 127,
    "Name": "Test Movement Up",
    "Expected": 1,
    "MoveRequest": {
      "game": {
        "id": "test-game-id",
        "ruleset": {
          "name": "standard",
          "version": "v1.0.0"
        },
        "timeout": 500
      },
      "turn": 1,
      "board": {
        "height": 11,
        "width": 11,
        "food": [
          {"x": 5, "y": 4}
        ],
        "hazards": [],
        "snakes": [
          {
            "id": "snake-1",
            "name": "Test Snake",
            "health": 100,
            "body": [
              {"x": 5, "y": 5},
              {"x": 5, "y": 6},
              {"x": 5, "y": 7}
            ],
            "head": {"x": 5, "y": 5},
            "length": 3,
            "latency": "0",
            "shout": ""
          }
        ]
      },
      "you": {
        "id": "snake-1",
        "name": "Test Snake",
        "health": 100,
        "body": [
          {"x": 5, "y": 5},
          {"x": 5, "y": 6},
          {"x": 5, "y": 7}
        ],
        "head": {"x": 5, "y": 5},
        "length": 3,
        "latency": "0",
        "shout": ""
      }
    }
  }
]`;

        this.input.value = exampleJSON;
        this.updateStats();
        this.notify.success('Esempio caricato');
    }

    // Copy to clipboard
    async copyToClipboard() {
        console.log('📋 Copy to clipboard...');

        const formatterCode = document.getElementById('formatterCode');
        const content = formatterCode?.textContent;

        if (!content) {
            this.notify.error('Nessun contenuto da copiare');
            return;
        }

        try {
            await navigator.clipboard.writeText(content);
            this.notify.success('JSON copiato negli appunti');

        } catch (error) {
            // Fallback
            const tempInput = document.createElement('input');
            tempInput.value = content;
            document.body.appendChild(tempInput);
            tempInput.select();

            try {
                document.execCommand('copy');
                this.notify.success('JSON copiato negli appunti');
            } catch (e) {
                this.notify.warning('Impossibile copiare');
            } finally {
                document.body.removeChild(tempInput);
            }
        }
    }

    // Export JSON
    exportJSON() {
        console.log('💾 Export JSON...');

        const formatterCode = document.getElementById('formatterCode');
        const content = formatterCode?.textContent;

        if (!content) {
            this.notify.error('Nessun contenuto da esportare');
            return;
        }

        try {
            const blob = new Blob([content], { type: 'application/json' });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');

            link.href = url;
            link.download = `battlesnake-tests-${Date.now()}.json`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            URL.revokeObjectURL(url);

            this.notify.success('File esportato');

        } catch (error) {
            console.error('Export error:', error);
            this.notify.error('Errore durante l\'esportazione');
        }
    }

    // Show output
    showOutput(content) {
        const formatterOutput = document.getElementById('formatterOutput');
        const formatterPlaceholder = document.getElementById('formatterPlaceholder');
        const formatterCode = document.getElementById('formatterCode');

        if (formatterOutput && formatterPlaceholder && formatterCode) {
            formatterCode.textContent = content;
            formatterOutput.style.display = 'block';
            formatterPlaceholder.style.display = 'none';

            // Scroll to output
            formatterOutput.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    }

    // Hide output
    hideOutput() {
        const formatterOutput = document.getElementById('formatterOutput');
        const formatterPlaceholder = document.getElementById('formatterPlaceholder');

        if (formatterOutput && formatterPlaceholder) {
            formatterOutput.style.display = 'none';
            formatterPlaceholder.style.display = 'block';
        }
    }

    // Update validation status
    updateValidationStatus(message, type) {
        const jsonValidation = document.getElementById('jsonValidation');
        if (jsonValidation) {
            jsonValidation.textContent = message;
            jsonValidation.className = `validation-status validation-${type}`;
            jsonValidation.style.display = 'block';
        }
    }

    // Clear validation status
    clearValidationStatus() {
        const jsonValidation = document.getElementById('jsonValidation');
        if (jsonValidation) {
            jsonValidation.style.display = 'none';
        }
    }

    // Initialize
    initialize() {
        console.log('🔧 Inizializzazione FormatterTabManager...');

        this.resetCache();

        setTimeout(() => {
            if (this.input) {
                this.setupEventListener();
                this.initialized = true;
                console.log('✅ FormatterTabManager inizializzato');
            } else {
                console.error('❌ Elemento input non trovato');
            }
        }, 50);
    }

    // Check if initialized
    isInitialized() {
        return this.initialized && this.input !== null;
    }
}