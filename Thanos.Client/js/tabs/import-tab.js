/**
 * Battlesnake Board Converter - Import Tab JavaScript
 * Handles board input, validation, import functionality and icon copying
 * Updated: HTML moved to import-tab.html, uses global NotifyService
 */

class ImportTab {
    constructor() {
        this.initialized = false;
        this.elements = {};
    }

    /**
     * Initialize the import tab
     */
    init() {
        if (this.initialized) return;

        // Check if HTML is already loaded
        const importTab = document.getElementById('import-tab');
        if (!importTab) {
            console.error('Import tab container not found');
            return;
        }

        // HTML should be loaded from import-tab.html, just cache elements
        this.cacheElements();
        this.setupEventListeners();
        this.updateStats();
        this.initialized = true;
        console.log('ImportTab initialized');
    }

    /**
     * Cache DOM elements for performance
     */
    cacheElements() {
        this.elements = {
            boardInput: document.getElementById('boardInput'),
            importBtn: document.getElementById('importBtn'),
            clearBtn: document.getElementById('clearBtn'),
            exampleBtn: document.getElementById('exampleBtn'),
            lineCount: document.getElementById('lineCount'),
            charCount: document.getElementById('charCount'),
            gridCount: document.getElementById('gridCount'),
            iconButtons: document.querySelectorAll('.icon-btn')
        };
    }

    /**
     * Setup all event listeners
     */
    setupEventListeners() {
        // Import button
        if (this.elements.importBtn) {
            this.elements.importBtn.addEventListener('click', () => this.importBoards());
        }

        // Clear button
        if (this.elements.clearBtn) {
            this.elements.clearBtn.addEventListener('click', () => this.clearInput());
        }

        // Example button
        if (this.elements.exampleBtn) {
            this.elements.exampleBtn.addEventListener('click', () => this.loadExample());
        }

        // Board input for live stats
        if (this.elements.boardInput) {
            this.elements.boardInput.addEventListener('input', () => this.updateStats());
            this.elements.boardInput.addEventListener('paste', () => {
                // Update stats after paste event
                setTimeout(() => this.updateStats(), 10);
            });
        }

        // Icon copy buttons
        this.elements.iconButtons.forEach(btn => {
            btn.addEventListener('click', (e) => this.copyIcon(e));
        });

        // Register tab with tab manager
        if (window.BattlesnakeTabManager) {
            window.BattlesnakeTabManager.registerTab('import', {
                onActivate: () => this.onTabActivate(),
                onDeactivate: () => this.onTabDeactivate()
            });
        }
    }

    /**
     * Tab activation handler
     */
    onTabActivate() {
        // Focus board input when tab is activated
        if (this.elements.boardInput) {
            this.elements.boardInput.focus();
        }
    }

    /**
     * Tab deactivation handler
     */
    onTabDeactivate() {
        // Any cleanup when leaving tab
    }

    /**
     * Import boards from input
     */
    importBoards() {
        const input = this.elements.boardInput?.value?.trim();

        if (!input) {
            window.NotifyService?.error('❌ Inserisci almeno una griglia nel campo di input');
            return;
        }

        try {
            window.NotifyService?.info('🔄 Elaborazione in corso...');

            // Parse the boards
            const boards = this.parseBoards(input);

            if (boards.length === 0) {
                window.NotifyService?.error('❌ Nessuna griglia valida trovata nell\'input');
                return;
            }

            // Store boards in global state
            this.storeBoards(boards);

            // Show success message
            const message = boards.length === 1
                ? '✅ 1 griglia importata con successo'
                : `✅ ${boards.length} griglie importate con successo`;

            window.NotifyService?.success(message);

            // Switch to process tab if available
            if (window.BattlesnakeTabManager) {
                setTimeout(() => {
                    window.BattlesnakeTabManager.switchTab('process');
                }, 1000);
            }

        } catch (error) {
            console.error('Import error:', error);
            window.NotifyService?.error(`❌ Errore durante l'importazione: ${error.message}`);
        }
    }

    /**
     * Parse boards from input text
     */
    parseBoards(input) {
        const boards = [];
        const sections = input.split(/\n\s*\n/).filter(section => section.trim());

        for (const section of sections) {
            try {
                const board = this.parseBoard(section);
                if (board) {
                    boards.push(board);
                }
            } catch (error) {
                console.warn('Failed to parse board section:', error);
            }
        }

        return boards;
    }

    /**
     * Parse individual board
     */
    parseBoard(boardText) {
        if (typeof window.parseBoard === 'function') {
            return window.parseBoard(boardText);
        }

        // Fallback basic parsing
        const lines = boardText.split('\n').filter(line => line.trim());
        return {
            width: 11,
            height: 11,
            snakes: [],
            food: [],
            hazards: []
        };
    }

    /**
     * Store boards in global state
     */
    storeBoards(boards) {
        if (window.BattlesnakeMain?.app) {
            window.BattlesnakeMain.app.boards = boards;
        } else {
            // Fallback storage
            window.importedBoards = boards;
        }
    }

    /**
     * Clear input field
     */
    clearInput() {
        if (this.elements.boardInput) {
            this.elements.boardInput.value = '';
            this.updateStats();
            this.elements.boardInput.focus();
            window.NotifyService?.success('✅ Campo pulito');
        }
    }

    /**
     * Load example board
     */
    loadExample() {
        const example = this.getExampleBoard();
        if (this.elements.boardInput) {
            this.elements.boardInput.value = example;
            this.updateStats();
            window.NotifyService?.success('✅ Esempio caricato');
        }
    }

    /**
     * Get example board content
     */
    getExampleBoard() {
        return `00  01  02  03  04  05  06  07  08  09  10
00 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
01 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
02 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
03 ⬛  ⬛  ⬛  ⬛  ⬛  👽  ⬛  ⬛  ⬛  ⬛  ⬛  
04 ⬛  ⬛  ⬛  ⬛  ⬛  💲  ⬛  ⬛  ⬛  ⬛  ⬛  
05 ⬛  ⬛  ⬛  ⬛  ⬛  💲  ⬛  ⬛  ⬛  ⬛  ⬛  
06 ⬛  ⬛  ⬛  ⬛  ⬛  🍎  ⬛  ⬛  ⬛  ⬛  ⬛  
07 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
08 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
09 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
10 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛`;
    }

    /**
     * Update input statistics
     */
    updateStats() {
        const input = this.elements.boardInput?.value || '';
        const lines = input.split('\n').length;
        const chars = input.length;
        const grids = this.countGrids(input);

        if (this.elements.lineCount) {
            this.elements.lineCount.textContent = lines;
        }
        if (this.elements.charCount) {
            this.elements.charCount.textContent = chars;
        }
        if (this.elements.gridCount) {
            this.elements.gridCount.textContent = grids;
        }
    }

    /**
     * Count grids in input
     */
    countGrids(input) {
        if (!input.trim()) return 0;

        const sections = input.split(/\n\s*\n/).filter(section => {
            const lines = section.split('\n').filter(line => line.trim());
            return lines.length > 2; // At least 3 lines to be a potential grid
        });

        return sections.length;
    }

    /**
     * Copy icon to clipboard
     */
    async copyIcon(event) {
        const button = event.target;
        const icon = button.getAttribute('data-icon');

        if (!icon) return;

        try {
            await navigator.clipboard.writeText(icon);

            // Temporarily change button appearance
            const originalText = button.textContent;
            button.textContent = '✅';
            button.style.background = '#22c55e';

            setTimeout(() => {
                button.textContent = originalText;
                button.style.background = '';
            }, 1000);

            window.NotifyService?.success(`✅ Icona ${icon} copiata negli appunti`);

        } catch (error) {
            console.error('Failed to copy icon:', error);
            window.NotifyService?.error('❌ Errore durante la copia');
        }
    }
}

// Create and register the import tab
const importTab = new ImportTab();

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        importTab.init();
    });
} else {
    importTab.init();
}

// Export for global access
window.BattlesnakeImportTab = importTab;