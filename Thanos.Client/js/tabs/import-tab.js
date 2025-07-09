// Battlesnake Board Converter - Import Tab

class ImportTab {
    constructor() {
        this.initialized = false;
    }

    // Initialize the import tab
    init() {
        if (this.initialized) return;

        this.setupEventListeners();
        this.initialized = true;

        console.log('ImportTab initialized');
    }

    // Setup event listeners for import tab
    setupEventListeners() {
        // Import button
        const importBtn = document.getElementById('importBtn');
        if (importBtn) {
            importBtn.addEventListener('click', () => this.importBoards());
        }

        // Icon buttons for copying
        this.setupIconButtons();
    }

    // Setup icon copy buttons
    setupIconButtons() {
        const iconButtons = document.querySelectorAll('.icon-btn');
        iconButtons.forEach(button => {
            // Extract icon from button text or data attribute
            const buttonText = button.textContent;
            const iconMatch = buttonText.match(/^([👽💲⬛🍎💀⬆️⬇️⬅️➡️])/);

            if (iconMatch) {
                const icon = iconMatch[1];
                button.addEventListener('click', () => {
                    if (window.BattlesnakeCommon) {
                        window.BattlesnakeCommon.copyIcon(icon);
                    }
                });
            }
        });
    }

    // Main import function
    importBoards() {
        try {
            const boardInput = document.getElementById('boardInput');
            if (!boardInput) {
                throw new Error('Board input element not found');
            }

            const inputText = boardInput.value.trim();

            if (!inputText) {
                throw new Error('Inserisci una o più griglie da importare');
            }

            // Use common parsing function
            const gameStates = window.BattlesnakeCommon.parseMultipleBoards(inputText);

            if (gameStates.length === 0) {
                throw new Error('Nessuna griglia valida trovata nell\'input');
            }

            // Update global state
            window.BattlesnakeCommon.setImportedBoards(gameStates);

            // Show success message
            window.BattlesnakeCommon.showStatus(`✅ Importate ${gameStates.length} board!`);

            // Notify other tabs about the import
            this.notifyBoardsImported(gameStates);

            // Switch to process tab
            if (window.BattlesnakeTabManager) {
                window.BattlesnakeTabManager.switchTab('process');
            }

        } catch (error) {
            window.BattlesnakeCommon.showStatus(`❌ Errore: ${error.message}`, true);
            console.error('Import error:', error);
        }
    }

    // Notify other components about imported boards
    notifyBoardsImported(boards) {
        // Trigger custom event
        const event = new CustomEvent('boardsImported', {
            detail: { boards: boards, count: boards.length }
        });
        document.dispatchEvent(event);
    }

    // Called when tab becomes active
    onActivate() {
        // Focus on board input if empty
        const boardInput = document.getElementById('boardInput');
        if (boardInput && !boardInput.value.trim()) {
            setTimeout(() => boardInput.focus(), 100);
        }
    }

    // Clear the input
    clearInput() {
        const boardInput = document.getElementById('boardInput');
        if (boardInput) {
            boardInput.value = '';
        }
    }

    // Set example input
    setExampleInput() {
        const boardInput = document.getElementById('boardInput');
        if (boardInput) {
            boardInput.value = this.getExampleBoard();
        }
    }

    // Get example board text
    getExampleBoard() {
        return `00  01  02  03  04  05  06  07  08  09  10
00 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
01 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
02 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
03 ⬛  ⬛  ⬛  ⬛  ⬛  👽  ⬛  ⬛  ⬛  ⬛  ⬛  
04 ⬛  ⬛  ⬛  ⬛  ⬛  💲  ⬛  ⬛  ⬛  ⬛  ⬛  
05 ⬛  ⬛  ⬛  ⬛  ⬛  💲  ⬛  ⬛  ⬛  ⬛  ⬛  
06 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
07 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
08 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
09 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
10 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  `;
    }

    // Validate board input
    validateInput(inputText) {
        if (!inputText || !inputText.trim()) {
            return { valid: false, error: 'Input vuoto' };
        }

        // Check for basic grid pattern
        const hasNumbers = /^\s*\d+/.test(inputText);
        if (!hasNumbers) {
            return { valid: false, error: 'Formato griglia non valido' };
        }

        return { valid: true };
    }

    // Get input statistics
    getInputStats() {
        const boardInput = document.getElementById('boardInput');
        if (!boardInput) return null;

        const text = boardInput.value;
        const lines = text.split('\n').length;
        const chars = text.length;
        const grids = (text.match(/^\s*\d+/gm) || []).length;

        return { lines, chars, grids };
    }
}

// Create and register import tab
const importTab = new ImportTab();

// Global function for backward compatibility
function importBoards() {
    importTab.importBoards();
}

// Auto-register with tab manager when available
document.addEventListener('DOMContentLoaded', () => {
    if (window.BattlesnakeTabManager) {
        window.BattlesnakeTabManager.tabManager.registerTab('import', importTab);
    }
    importTab.init();
});

// Export for module use
window.BattlesnakeImportTab = {
    importTab,
    importBoards
};