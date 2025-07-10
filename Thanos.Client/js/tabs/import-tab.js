/**
 * ImportTabManager - Gestisce la funzionalità del tab Import
 * Implementazione completa con tutti i metodi funzionanti
 */
class ImportTabManager {

    constructor(notificationService) {
        this.notificationService = notificationService;
        this.importedBoards = [];
        this.exampleBoard = `
        👽 💲 💲 ⬛ ⬛
        💲 💲 💲 ⬛ ⬛
        💲 💲 ⬛ ⬛ ⬛
        ⬛ ⬛ 😈 ⛔ ⬛
        
        💀 ⬛ ⬛ ⬛ ⬛
        ⬛ 👽 💲 ⬛ ⬛
        ⬛ ⬛ ⬛ ⬛ ⬛
        ⬛ ⬛ 😈 ⛔ ⛔`;

        this.bindEvents();
        this.updateStats();
        
        console.log('✅ ImportTabManager inizializzato con successo');
    }

    /**
     * Bind degli eventi per aggiornare le statistiche in tempo reale
     */
    bindEvents() {
        document.addEventListener('input', (e) => {
            if (e.target.id === 'boardInput') {
                this.updateStats();
            }
        });
    }

    /**
     * Importa le board dal textarea
     */
    importBoards() {
        const input = document.getElementById('boardInput')?.value?.trim();

        if (!input) {
            this.showNotification('⚠️ Inserisci almeno una griglia prima di importare', 'warning');
            return;
        }

        try {
            // Parse delle board dal testo
            const boards = this.parseBoards(input);

            if (boards.length === 0) {
                this.showNotification('❌ Nessuna griglia valida trovata nel testo', 'error');
                return;
            }

            // Validazione delle board
            const validation = this.validateBoards(boards);
            if (!validation.valid) {
                this.showNotification(`❌ Errore di validazione: ${validation.error}`, 'error');
                return;
            }

            // Salva le board importate
            this.storeBoards(boards);

            this.showNotification(`✅ ${boards.length} griglia/e importate con successo!`, 'success');

            // Opzionale: pulisce l'input dopo l'import
            // this.clearInput();

        } catch (error) {
            console.error('Errore durante l\'import:', error);
            this.showNotification(`❌ Errore durante l'importazione: ${error.message}`, 'error');
        }
    }

    /**
     * Pulisce il campo di input
     */
    clearInput() {
        const textarea = document.getElementById('boardInput');
        if (textarea) {
            textarea.value = '';
            this.updateStats();
            textarea.focus();
            this.showNotification('🗑️ Campo pulito', 'info');
        }
    }

    /**
     * Carica un esempio di griglia
     */
    loadExample() {
        const textarea = document.getElementById('boardInput');
        if (textarea) {
            textarea.value = this.exampleBoard;
            this.updateStats();
            this.showNotification('📋 Esempio caricato', 'info');
        }
    }

    /**
     * Aggiorna le statistiche in tempo reale
     */
    updateStats() {
        const input = document.getElementById('boardInput')?.value || '';

        // Conta righe
        const lines = input.split('\n').length;
        const lineCountEl = document.getElementById('lineCount');
        if (lineCountEl) lineCountEl.textContent = lines;

        // Conta caratteri
        const chars = input.length;
        const charCountEl = document.getElementById('charCount');
        if (charCountEl) charCountEl.textContent = chars;

        // Conta griglie
        const grids = this.countGrids(input);
        const gridCountEl = document.getElementById('gridCount');
        if (gridCountEl) gridCountEl.textContent = grids;
    }

    /**
     * Conta il numero di griglie nel testo
     */
    countGrids(input) {
        if (!input.trim()) return 0;

        try {
            const boards = this.parseBoards(input);
            return boards.length;
        } catch (error) {
            return 0;
        }
    }

    /**
     * Copia un'icona negli appunti
     */
    async copyIcon(icon) {
        try {
            await navigator.clipboard.writeText(icon);
            this.showNotification(`📋 Icona ${icon} copiata negli appunti`, 'success');
        } catch (error) {
            // Fallback per browser che non supportano clipboard API
            try {
                const textArea = document.createElement('textarea');
                textArea.value = icon;
                document.body.appendChild(textArea);
                textArea.select();
                document.execCommand('copy');
                document.body.removeChild(textArea);
                this.showNotification(`📋 Icona ${icon} copiata negli appunti`, 'success');
            } catch (fallbackError) {
                console.error('Errore copia negli appunti:', fallbackError);
                this.showNotification('❌ Errore durante la copia', 'error');
            }
        }
    }

    /**
     * Parse delle board dal testo di input
     */
    parseBoards(input) {
        const boards = [];
        const lines = input.split('\n').map(line => line.trim()).filter(line => line.length > 0);

        let currentBoard = [];

        for (const line of lines) {
            // Se la riga contiene emoji/caratteri di gioco, è parte di una board
            if (this.isGameLine(line)) {
                currentBoard.push(line);
            } else {
                // Se abbiamo accumulato righe e troviamo una riga vuota/non-game,
                // concludiamo la board corrente
                if (currentBoard.length > 0) {
                    boards.push(this.processBoardLines(currentBoard));
                    currentBoard = [];
                }
            }
        }

        // Non dimenticare l'ultima board se il file non finisce con una riga vuota
        if (currentBoard.length > 0) {
            boards.push(this.processBoardLines(currentBoard));
        }

        return boards.filter(board => board !== null);
    }

    /**
     * Verifica se una riga contiene caratteri di gioco
     */
    isGameLine(line) {
        const gameChars = ['👽', '💲', '😈', '⛔', '💀', '⬛', '⬆️', '⬇️', '⬅️', '➡️'];
        return gameChars.some(char => line.includes(char));
    }

    /**
     * Processa le righe di una singola board
     */
    processBoardLines(lines) {
        if (lines.length === 0) return null;

        const board = {
            width: 0,
            height: lines.length,
            grid: [],
            myHead: null,
            myBody: [],
            enemies: [],
            hazards: [],
            food: []
        };

        for (let y = 0; y < lines.length; y++) {
            const line = lines[y];
            const cells = this.parseGameLine(line);

            if (cells.length > board.width) {
                board.width = cells.length;
            }

            board.grid.push(cells);

            // Identifica posizioni speciali
            for (let x = 0; x < cells.length; x++) {
                const cell = cells[x];
                const pos = { x, y };

                switch (cell) {
                    case '👽':
                        board.myHead = pos;
                        break;
                    case '💲':
                        board.myBody.push(pos);
                        break;
                    case '😈':
                        board.enemies.push({ head: pos, body: [] });
                        break;
                    case '⛔':
                        // Trova il nemico più vicino per aggiungere questo corpo
                        const nearestEnemy = this.findNearestEnemy(board.enemies, pos);
                        if (nearestEnemy) {
                            nearestEnemy.body.push(pos);
                        }
                        break;
                    case '💀':
                        board.hazards.push(pos);
                        break;
                }
            }
        }

        return board;
    }

    /**
     * Parse di una singola riga di gioco
     */
    parseGameLine(line) {
        // Rimuovi spazi extra e splitta su spazi
        const parts = line.trim().split(/\s+/);
        return parts.filter(part => part.length > 0);
    }

    /**
     * Trova il nemico più vicino per assegnare una parte del corpo
     */
    findNearestEnemy(enemies, pos) {
        if (enemies.length === 0) return null;

        let nearest = enemies[0];
        let minDistance = this.calculateDistance(enemies[0].head, pos);

        for (let i = 1; i < enemies.length; i++) {
            const distance = this.calculateDistance(enemies[i].head, pos);
            if (distance < minDistance) {
                minDistance = distance;
                nearest = enemies[i];
            }
        }

        return nearest;
    }

    /**
     * Calcola distanza Manhattan tra due posizioni
     */
    calculateDistance(pos1, pos2) {
        return Math.abs(pos1.x - pos2.x) + Math.abs(pos1.y - pos2.y);
    }

    /**
     * Validazione delle board importate
     */
    validateBoards(boards) {
        if (!Array.isArray(boards)) {
            return { valid: false, error: 'Le board devono essere un array' };
        }

        if (boards.length === 0) {
            return { valid: false, error: 'Nessuna board trovata' };
        }

        for (let i = 0; i < boards.length; i++) {
            const board = boards[i];

            // Validazione dimensioni minime
            if (board.width < 3 || board.height < 3) {
                return {
                    valid: false,
                    error: `Board ${i + 1}: dimensioni troppo piccole (minimo 3x3)`
                };
            }

            // Validazione presenza testa del giocatore
            if (!board.myHead) {
                return {
                    valid: false,
                    error: `Board ${i + 1}: manca la testa del giocatore (👽)`
                };
            }

            // Validazione griglia consistente
            for (const row of board.grid) {
                if (row.length === 0) {
                    return {
                        valid: false,
                        error: `Board ${i + 1}: trovata riga vuota`
                    };
                }
            }
        }

        return { valid: true };
    }

    /**
     * Salva le board importate
     */
    storeBoards(boards) {
        this.importedBoards = [...boards];

        // Salva anche nel localStorage per persistenza
        try {
            localStorage.setItem('battlesnake_imported_boards', JSON.stringify(boards));
        } catch (error) {
            console.warn('Impossibile salvare nel localStorage:', error);
        }

        // Notifica altre parti dell'app se necessario
        if (window.StateManager) {
            window.StateManager.setBoards(boards);
        }

        console.log(`📊 ${boards.length} board salvate:`, boards);
    }

    /**
     * Ottieni le board importate
     */
    getImportedBoards() {
        return [...this.importedBoards];
    }

    /**
     * Carica board dal localStorage
     */
    loadStoredBoards() {
        try {
            const stored = localStorage.getItem('battlesnake_imported_boards');
            if (stored) {
                this.importedBoards = JSON.parse(stored);
                return this.importedBoards;
            }
        } catch (error) {
            console.warn('Errore caricamento localStorage:', error);
        }
        return [];
    }

    /**
     * Helper per mostrare notifiche
     */
    showNotification(message, type = 'info') {
        if (this.notificationService) {
            this.notificationService.show(message, type);
        } else if (window.snake?.notifyService) {
            window.snake.notifyService.show(message, type);
        } else {
            console.log(`[${type.toUpperCase()}] ${message}`);
        }
    }

    /**
     * Metodo per debug - mostra info sulle board importate
     */
    debugBoards() {
        console.group('🐛 Debug Imported Boards');
        console.log('Numero totale di board:', this.importedBoards.length);

        this.importedBoards.forEach((board, index) => {
            console.group(`Board ${index + 1}`);
            console.log('Dimensioni:', `${board.width}x${board.height}`);
            console.log('Testa giocatore:', board.myHead);
            console.log('Corpo giocatore:', board.myBody);
            console.log('Nemici:', board.enemies.length);
            console.log('Pericoli:', board.hazards.length);
            console.log('Griglia:', board.grid);
            console.groupEnd();
        });

        console.groupEnd();
    }
}