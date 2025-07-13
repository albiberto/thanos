class ImportTabManager {
    constructor(inputId, notifyService) {
        this.inputId = inputId;
        this.notify = notifyService;
        this._input = null;
        this._eventListener = null;
        this.initialized = false;

        this.STORAGE_KEY = 'battlesnake_grids';
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
    // Modifica il metodo importBoards
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
            const sanitizer = new ImprovedSanitizer();
            const sanitizedContent = sanitizer.sanitizeBattlesnakeGrid(this.input.value);
            
            console.log('Contenuto sanificato:', sanitizedContent);
            
            // Parsing delle griglie
            const gridTexts = sanitizedContent
            .split(/\n\s*\n/)
            .filter(grid => grid.trim())
            .map(grid => grid.trim());

            if (gridTexts.length === 0) {
                this.notify.warning('Nessuna griglia valida trovata');
                return;
            }

            // Validazione e parsing delle griglie
            const parsedGrids = [];
            for (let i = 0; i < gridTexts.length; i++) {
                const parsedGrid = this.parseGrid(gridTexts[i], i + 1);
                if (parsedGrid) {
                    parsedGrids.push(parsedGrid);
                } else {
                    return; // L'errore è già stato notificato in parseGrid
                }
            }

            // Salva nel localStorage
            this.saveGridsToStorage(parsedGrids);

            this.notify.success(`${parsedGrids.length} griglie importate e salvate con successo`);

            // Naviga al tab process
            setTimeout(() => {
                this.navigateToProcessTab();
            }, 1000);

        } catch (error) {
            console.error('Errore durante l\'importazione:', error);
            this.notify.error('Errore durante l\'importazione delle griglie');
        }
    }

    // Parse una singola griglia in formato strutturato
    parseGrid(gridText, gridIndex) {
        if (!gridText || !gridText.trim()) {
            this.notify.error(`Griglia ${gridIndex} è vuota`);
            return null;
        }

        const lines = gridText.split('\n').filter(line => line.trim());
        if (lines.length === 0) {
            this.notify.error(`Griglia ${gridIndex} non contiene righe valide`);
            return null;
        }

        // Converte ogni riga in array di celle
        const rows = lines.map(line => {
            // Split su spazi multipli o tab
            return line.split(/\s+/).filter(cell => cell);
        });

        // Verifica che tutte le righe abbiano la stessa lunghezza
        const width = rows[0].length;
        const height = rows.length;

        if (!rows.every(row => row.length === width)) {
            this.notify.error(`Griglia ${gridIndex}: le righe hanno lunghezze diverse (prima riga: ${width} celle)`);
            return null;
        }

        // Verifica dimensioni minime
        if (width < 3 || height < 3) {
            this.notify.error(`Griglia ${gridIndex}: dimensioni troppo piccole (minimo 3x3), trovata ${width}x${height}`);
            return null;
        }

        // Analizza il contenuto della griglia
        const analysis = this.analyzeGrid(rows);

        return {
            id: `grid_${Date.now()}_${gridIndex}`,
            index: gridIndex,
            width: width,
            height: height,
            rawText: gridText,
            cells: rows,
            analysis: analysis,
            status: 'imported',
            timestamp: new Date().toISOString(),
            importedAt: new Date().toISOString()
        };
    }

    // Analizza il contenuto di una griglia per estrarre informazioni
    analyzeGrid(rows) {
        const analysis = {
            myHead: null,
            myTail: null,
            myBody: [],
            enemyHeads: [],
            enemyBodies: [],
            food: [],
            hazards: [],
            empty: [],
            directions: [],
            totalCells: rows.length * rows[0].length
        };

        for (let y = 0; y < rows.length; y++) {
            for (let x = 0; x < rows[y].length; x++) {
                const cell = rows[y][x];
                const position = { x, y };
                
                switch (cell) {
                    case 'H':
                        analysis.myHead = position;
                        break;
                    case 'B':
                        analysis.myBody.push(position);
                        break;
                    case 'T':
                        analysis.myTail = position;
                        break;
                    case 'E':
                        analysis.enemyHeads.push(position);
                        break;
                    case 'b':
                        analysis.enemyBodies.push(position);
                        break;
                    case 'F':
                        analysis.food.push(position);
                        break;
                    case 'X':
                        analysis.hazards.push(position);
                        break;
                    case '.':
                        analysis.empty.push(position);
                        break;
                        case '^':
                        case 'v':
                        case '<':
                        case '>':
                        analysis.directions.push({ position, direction: cell });
                        break;
                }
            }
        }

        // Calcola statistiche aggiuntive
        analysis.mySnakeLength = (analysis.myHead ? 1 : 0) + analysis.myBody.length;
        analysis.totalEnemies = analysis.enemyHeads.length;
        analysis.totalEnemyBodies = analysis.enemyBodies.length;
        analysis.emptyPercentage = Math.round((analysis.empty.length / analysis.totalCells) * 100);

        return analysis;
    }

    // Salva le griglie nel localStorage
    saveGridsToStorage(newGrids) {
        try {
            // Carica griglie esistenti se presenti
            let existingData = { grids: [], timestamp: null, version: '1.0' };

            try {
                const existing = localStorage.getItem(this.STORAGE_KEY);
                if (existing) {
                    existingData = JSON.parse(existing);
                }
            } catch (e) {
                console.warn('Dati esistenti nel localStorage non validi, li sovrascrivo');
            }

            // Aggiungi le nuove griglie
            const allGrids = [...(existingData.grids || []), ...newGrids];

            const data = {
                grids: allGrids,
                timestamp: new Date().toISOString(),
                version: '1.0',
                totalImported: allGrids.length,
                lastImport: new Date().toISOString()
            };

            localStorage.setItem(this.STORAGE_KEY, JSON.stringify(data));
            console.log(`✅ ${newGrids.length} nuove griglie salvate nel localStorage (totale: ${allGrids.length})`);

        } catch (error) {
            console.error('Errore nel salvataggio:', error);

            // Verifica se è un problema di spazio
            if (error.name === 'QuotaExceededError') {
                this.notify.error('Spazio localStorage esaurito. Elimina alcune griglie dal tab Process.');
            } else {
                this.notify.error('Errore nel salvataggio delle griglie');
            }
        }
    }

    // Naviga al tab process
    navigateToProcessTab() {
        console.log('🔄 Navigazione al tab process...');

        // Usa il tab manager globale
        if (window.snake?.tabManager) {
            window.snake.tabManager.switchTab('process');
        } else {
            console.error('Tab manager non disponibile');
            this.notify.error('Impossibile navigare al tab process');
        }
    }
// Clear input
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

    // Load example
    loadExample() {
        if (!this.input) {
            this.notify.error('Campo non trovato');
            return;
        }

        this.input.value = 
            '👽 💲 💲 ⬛ ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ 🍎\n' +
            '⬛ ⬛ 😈 ⛔ ⬛\n' +
            '💀 ⬛ ⬛ ⛔ ⛔\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n\n' +

            '⬛ ⬛ 😈 ⛔ ⛔\n' +
            '⬛ ⬛ ⬛ ⬛ ⛔\n' +
            '👽 💲 💲 🍎 ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛\n' +
            '💀 ⬛ ⬛ ⬛ ⬛\n\n' +

            '🍎 ⬛ ⬛ ⬛ ⬛ ⬛\n' +
            '⬛ 😈 ⛔ ⛔ ⬛ ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛ ⬛\n' +
            '👽 💲 💲 💲 ⬛ ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ 💀 ⬛\n' +
            '⬛ ⬛ ⬛ ⬛ ⬛ ⬛';
        this.updateStats();
        this.notify.success('Esempio caricato con 3 griglie');
    }

    // Copy icon to clipboard
    async copyIcon(icon) {
        try {
            await navigator.clipboard.writeText(icon);
            this.notify.success(`Icona ${icon} copiata negli appunti`);
        } catch (error) {
            // Fallback for older browsers
            const tempInput = document.createElement('input');
            tempInput.value = icon;
            document.body.appendChild(tempInput);
            tempInput.select();

            try {
                document.execCommand('copy');
                this.notify.success(`Icona ${icon} copiata negli appunti`);
            } catch (e) {
                this.notify.warning(`Impossibile copiare automaticamente. Usa manualmente: ${icon}`);
            } finally {
                document.body.removeChild(tempInput);
            }
        }
    }

    // Initialize
    initialize() {
        console.log('🔧 Inizializzazione ImportTabManager...');
        this.resetCache();

        setTimeout(() => {
            if (this.input) {
                this.setupEventListener();
                this.initialized = true;
                console.log('✅ ImportTabManager inizializzato');

                // Mostra statistiche sulle griglie esistenti
                this.showStorageInfo();
            } else {
                console.error('❌ Elemento input non trovato');
            }
        }, 50);
    }

    // Mostra info su griglie già salvate
    showStorageInfo() {
        try {
            const data = localStorage.getItem(this.STORAGE_KEY);
            if (data) {
                const parsedData = JSON.parse(data);
                const gridCount = parsedData.grids ? parsedData.grids.length : 0;
                if (gridCount > 0) {
                    this.notify.info(`Trovate ${gridCount} griglie già salvate nel localStorage`);
                }
            }
        } catch (error) {
            console.warn('Errore nel leggere i dati del localStorage:', error);
        }
    }
    
    isInitialized() {
        return this.initialized && this.input !== null;
    }
}