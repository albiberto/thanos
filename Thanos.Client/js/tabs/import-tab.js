/**
 * Battlesnake Board Converter - Import Tab JavaScript
 * Handles board input, validation, import functionality and icon copying
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

        this.loadHTML().then(() => {
            this.cacheElements();
            this.setupEventListeners();
            this.updateStats();
            this.initialized = true;
            console.log('ImportTab initialized');
        }).catch(error => {
            console.error('Failed to initialize ImportTab:', error);
        });
    }

    /**
     * Load the HTML content for the import tab
     */
    async loadHTML() {
        const importTab = document.getElementById('import-tab');
        if (!importTab) {
            throw new Error('Import tab container not found');
        }

        // HTML content for the import tab
        const htmlContent = `
            <div class="import-content">
                <!-- Board Input Section (Left side - grows) -->
                <div class="board-input-section">
                    <h2>📝 Board Input</h2>
                    
                    <textarea 
                        id="boardInput" 
                        class="board-input" 
                        placeholder="Incolla qui la griglia o liste di griglie...

Esempio singola griglia:
00  01  02  03  04  05  06  07  08  09  10
00 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
01 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
02 ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  ⬛  
03 ⬛  ⬛  ⬛  ⬛  ⬛  👽  ⬛  ⬛  ⬛  ⬛  ⬛  
04 ⬛  ⬛  ⬛  ⬛  ⬛  💲  ⬛  ⬛  ⬛  ⬛  ⬛  
05 ⬛  ⬛  ⬛  ⬛  ⬛  💲  ⬛  ⬛  ⬛  ⬛  ⬛  

Esempio multiple griglie (separate da linee vuote):
--- GRID 1 ---
00  01  02  03
00 ⬛  ⬛  ⬛  ⬛  
01 ⬛  👽  ⬛  ⬛  
02 ⬛  💲  ⬛  ⬛  

--- GRID 2 ---
00  01  02  03
00 ⬛  ⬛  ⬛  ⬛  
01 ⬛  ⬛  👽  ⬛  
02 ⬛  ⬛  💲  ⬛  "></textarea>

                    <div class="input-actions">
                        <button class="button" id="importBtn">
                            📥 Import Boards
                        </button>
                        <button class="button secondary" id="clearBtn">
                            🗑️ Clear
                        </button>
                        <button class="button secondary" id="exampleBtn">
                            📋 Example
                        </button>
                    </div>

                    <div class="input-stats" id="inputStats">
                        <span>Lines: <span id="lineCount">0</span></span>
                        <span>Characters: <span id="charCount">0</span></span>
                        <span>Grids detected: <span id="gridCount">0</span></span>
                    </div>

                    <div class="status-message" id="statusMessage"></div>
                </div>

                <!-- Icons Section (Right side - fixed width) -->
                <div class="icons-section">
                    <h2>🎯 Quick Icons</h2>
                    
                    <div class="icons-grid">
                        <button class="icon-btn" data-icon="👽">
                            <span class="icon-emoji">👽</span>
                            <span class="icon-label">Snake Head</span>
                        </button>
                        <button class="icon-btn" data-icon="💲">
                            <span class="icon-emoji">💲</span>
                            <span class="icon-label">Snake Body</span>
                        </button>
                        <button class="icon-btn" data-icon="⬛">
                            <span class="icon-emoji">⬛</span>
                            <span class="icon-label">Empty</span>
                        </button>
                        <button class="icon-btn" data-icon="🍎">
                            <span class="icon-emoji">🍎</span>
                            <span class="icon-label">Food</span>
                        </button>
                        <button class="icon-btn" data-icon="💀">
                            <span class="icon-emoji">💀</span>
                            <span class="icon-label">Hazard</span>
                        </button>
                        <button class="icon-btn" data-icon="⬆️">
                            <span class="icon-emoji">⬆️</span>
                            <span class="icon-label">Up</span>
                        </button>
                        <button class="icon-btn" data-icon="⬇️">
                            <span class="icon-emoji">⬇️</span>
                            <span class="icon-label">Down</span>
                        </button>
                        <button class="icon-btn" data-icon="⬅️">
                            <span class="icon-emoji">⬅️</span>
                            <span class="icon-label">Left</span>
                        </button>
                        <button class="icon-btn" data-icon="➡️">
                            <span class="icon-emoji">➡️</span>
                            <span class="icon-label">Right</span>
                        </button>
                    </div>

                    <div class="legend">
                        <h3>📚 Legend</h3>
                        <div class="legend-items">
                            <div class="legend-item">
                                <span class="legend-emoji">👽</span>
                                <span>Snake Head</span>
                            </div>
                            <div class="legend-item">
                                <span class="legend-emoji">💲</span>
                                <span>Snake Body</span>
                            </div>
                            <div class="legend-item">
                                <span class="legend-emoji">⬛</span>
                                <span>Empty Space</span>
                            </div>
                            <div class="legend-item">
                                <span class="legend-emoji">🍎</span>
                                <span>Food</span>
                            </div>
                            <div class="legend-item">
                                <span class="legend-emoji">💀</span>
                                <span>Hazard</span>
                            </div>
                            <div class="legend-item">
                                <span class="legend-emoji">⬆️⬇️⬅️➡️</span>
                                <span>Directions</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Insert the HTML content
        importTab.innerHTML = htmlContent;
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
            statusMessage: document.getElementById('statusMessage'),
            copyFeedback: document.getElementById('copyFeedback'),
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

        // Icon buttons for copying
        this.elements.iconButtons.forEach(button => {
            button.addEventListener('click', () => {
                const icon = button.dataset.icon;
                if (icon) {
                    this.copyIcon(icon);
                }
            });
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => this.handleKeyboardShortcuts(e));
    }

    /**
     * Handle keyboard shortcuts
     */
    handleKeyboardShortcuts(event) {
        // Only handle shortcuts when import tab is active
        const importTab = document.getElementById('import-tab');
        if (!importTab || !importTab.classList.contains('active')) return;

        // Ctrl/Cmd + Enter to import
        if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
            event.preventDefault();
            this.importBoards();
        }

        // Ctrl/Cmd + K to clear
        if ((event.ctrlKey || event.metaKey) && event.key === 'k') {
            event.preventDefault();
            this.clearInput();
        }

        // Ctrl/Cmd + E to load example
        if ((event.ctrlKey || event.metaKey) && event.key === 'e') {
            event.preventDefault();
            this.loadExample();
        }
    }

    /**
     * Main import function
     */
    importBoards() {
        try {
            const inputText = this.elements.boardInput.value.trim();

            if (!inputText) {
                throw new Error('Please enter one or more grids to import');
            }

            // Validate input format
            const validation = this.validateInput(inputText);
            if (!validation.valid) {
                throw new Error(validation.error);
            }

            // Count grids for feedback
            const gridCount = this.countGrids(inputText);

            if (gridCount === 0) {
                throw new Error('No valid grids found in input');
            }

            // TODO: Integration with existing BattlesnakeCommon
            // In the real implementation, this would:
            // 1. Call window.BattlesnakeCommon.parseMultipleBoards(inputText)
            // 2. Store results with window.BattlesnakeCommon.setImportedBoards(gameStates)
            // 3. Trigger custom events for other tabs
            // 4. Switch to process tab

            // For now, simulate successful import
            this.showStatus(`✅ Successfully imported ${gridCount} board(s)!`, 'success');

            // Trigger custom event for integration
            this.notifyBoardsImported(gridCount);

            console.log(`Import successful: ${gridCount} boards processed`);

        } catch (error) {
            this.showStatus(`❌ Error: ${error.message}`, 'error');
            console.error('Import error:', error);
        }
    }

    /**
     * Validate board input format
     */
    validateInput(inputText) {
        if (!inputText || !inputText.trim()) {
            return { valid: false, error: 'Input is empty' };
        }

        // Check for basic grid pattern (lines starting with numbers)
        const hasGridPattern = /^\s*\d+/.test(inputText);
        if (!hasGridPattern) {
            return { valid: false, error: 'Invalid grid format - expected coordinate headers' };
        }

        // Check for emoji patterns (basic validation)
        const hasEmojis = /[👽💲⬛🍎💀⬆️⬇️⬅️➡️]/.test(inputText);
        if (!hasEmojis) {
            return { valid: false, error: 'No valid board symbols found' };
        }

        return { valid: true };
    }

    /**
     * Count potential grids in input
     */
    countGrids(inputText) {
        // Count coordinate header lines (lines starting with numbers followed by space and numbers)
        const coordinateHeaders = inputText.match(/^\s*\d+\s+\d+/gm) || [];

        // Estimate grid count (each grid should have multiple coordinate lines)
        // This is a rough estimate - real parsing would be more precise
        return Math.max(1, Math.floor(coordinateHeaders.length / 3));
    }

    /**
     * Copy icon to clipboard
     */
    copyIcon(icon) {
        // Modern clipboard API
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(icon)
            .then(() => this.showCopyFeedback(icon))
            .catch(err => {
                console.warn('Clipboard API failed, using fallback:', err);
                this.fallbackCopy(icon);
            });
        } else {
            // Fallback for older browsers
            this.fallbackCopy(icon);
        }
    }

    /**
     * Fallback copy method for older browsers
     */
    fallbackCopy(text) {
        const textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-999999px';
        textArea.style.top = '-999999px';
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();

        try {
            const successful = document.execCommand('copy');
            if (successful) {
                this.showCopyFeedback(text);
            } else {
                console.error('Fallback copy failed');
            }
        } catch (err) {
            console.error('Fallback copy failed:', err);
        }

        document.body.removeChild(textArea);
    }

    /**
     * Show copy feedback animation
     */
    showCopyFeedback(icon) {
        const feedback = this.elements.copyFeedback;
        feedback.textContent = `Copied: ${icon}`;
        feedback.classList.add('show');

        setTimeout(() => {
            feedback.classList.remove('show');
        }, 2000);
    }

    /**
     * Show status message
     */
    showStatus(message, type = 'success') {
        const statusElement = this.elements.statusMessage;
        statusElement.textContent = message;
        statusElement.className = `status-message status-${type}`;
        statusElement.style.display = 'block';

        // Auto-hide after 5 seconds
        setTimeout(() => {
            statusElement.style.display = 'none';
        }, 5000);
    }

    /**
     * Update input statistics
     */
    updateStats() {
        const text = this.elements.boardInput.value;

        const lines = text.split('\n').length;
        const chars = text.length;
        const grids = this.countGrids(text);

        this.elements.lineCount.textContent = lines;
        this.elements.charCount.textContent = chars;
        this.elements.gridCount.textContent = grids;
    }

    /**
     * Clear board input
     */
    clearInput() {
        this.elements.boardInput.value = '';
        this.updateStats();
        this.showStatus('Input cleared', 'success');
        this.elements.boardInput.focus();
    }

    /**
     * Load example board
     */
    loadExample() {
        const exampleBoard = this.getExampleBoard();
        this.elements.boardInput.value = exampleBoard;
        this.updateStats();
        this.showStatus('Example loaded', 'success');
    }

    /**
     * Get example board text
     */
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

    /**
     * Notify other components about imported boards
     */
    notifyBoardsImported(boardCount) {
        // Dispatch custom event for integration with other tabs
        const event = new CustomEvent('boardsImported', {
            detail: {
                count: boardCount,
                timestamp: Date.now()
            }
        });
        document.dispatchEvent(event);
    }

    /**
     * Called when tab becomes active
     */
    onActivate() {
        // Focus on board input if empty
        if (this.elements.boardInput && !this.elements.boardInput.value.trim()) {
            setTimeout(() => this.elements.boardInput.focus(), 100);
        }
    }

    /**
     * Called when tab becomes inactive
     */
    onDeactivate() {
        // Hide any visible status messages
        if (this.elements.statusMessage) {
            this.elements.statusMessage.style.display = 'none';
        }
    }

    /**
     * Get current input statistics
     */
    getInputStats() {
        const text = this.elements.boardInput.value;
        return {
            lines: text.split('\n').length,
            characters: text.length,
            grids: this.countGrids(text),
            hasContent: text.trim().length > 0
        };
    }
}

// Create global instance
const importTab = new ImportTab();

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => importTab.init());
} else {
    importTab.init();
}

// Global functions for backward compatibility and direct access
window.importBoards = () => importTab.importBoards();
window.clearBoardInput = () => importTab.clearInput();
window.loadExample = () => importTab.loadExample();

// Export for module use and integration
window.BattlesnakeImportTab = {
    instance: importTab,
    ImportTab: ImportTab
};

// Auto-register with tab manager when available
document.addEventListener('DOMContentLoaded', () => {
    if (window.BattlesnakeTabManager) {
        window.BattlesnakeTabManager.registerTab('import', importTab);
    }
});