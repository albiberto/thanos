/**
 * Process Tab Manager - Updated with Single Grid Layout and Grid Component
 * One grid per page with two-column layout (grid + controls)
 */
class ProcessTabManager {
    constructor(containerId, notifyService) {
        this.containerId = containerId;
        this.notify = notifyService;
        this.grids = [];
        this.currentGridIndex = 0;
        this.startId = 101;
        this.initialized = false;

        // Storage key per le griglie
        this.STORAGE_KEY = 'battlesnake_grids';

        // JSON formatter instance
        this.jsonFormatter = new BattlesnakeJsonFormatter();

        // Grid Matrix Component instance
        this.gridComponent = new GridMatrixComponent({
            cellSize: 40,
            fontSize: 25,
            margin: 2,
            borderRadius: 3
        });

        // Expected values mapping
        this.expectedLabels = {
            1: "⬆️ UP",
            2: "⬇️ DOWN",
            3: "⬆️⬇️ UP|DOWN",
            4: "⬅️ LEFT",
            5: "⬆️⬅️ UP|LEFT",
            6: "⬇️⬅️ DOWN|LEFT",
            7: "⬆️⬇️⬅️ UP|DOWN|LEFT",
            8: "➡️ RIGHT",
            9: "⬆️➡️ UP|RIGHT",
            10: "⬇️➡️ DOWN|RIGHT",
            11: "⬆️⬇️➡️ UP|DOWN|RIGHT",
            12: "⬅️➡️ LEFT|RIGHT",
            13: "⬆️⬅️➡️ UP|LEFT|RIGHT",
            14: "⬇️⬅️➡️ DOWN|LEFT|RIGHT",
            15: "⬆️⬇️⬅️➡️ ALL"
        };
    }

    /**
     * Initialize
     */
    initialize() {
        console.log('🔧 Inizializzazione ProcessTabManager...');

        // Initialize JSON formatter
        if (window.BattlesnakeJsonFormatter) {
            this.jsonFormatter = new window.BattlesnakeJsonFormatter();
            console.log('✅ JSON Formatter initialized');
        } else {
            console.error('❌ BattlesnakeJsonFormatter not found');
        }

        // Initialize Grid Component
        if (window.GridMatrixComponent) {
            this.gridComponent = new window.GridMatrixComponent({
                cellSize: 40,
                fontSize: 25,
                margin: 2,
                borderRadius: 3
            });
            console.log('✅ Grid Matrix Component initialized');
        } else {
            console.error('❌ GridMatrixComponent not found');
        }

        this.initialized = true;
        this.loadGridsFromStorage();
    }

    /**
     * Load grids from localStorage
     */
    loadGridsFromStorage() {
        console.log('📂 Caricamento griglie dal localStorage...');

        try {
            const data = localStorage.getItem(this.STORAGE_KEY);

            if (!data) {
                this.showNoGridsMessage();
                return;
            }

            const parsedData = JSON.parse(data);

            if (!parsedData.grids || !Array.isArray(parsedData.grids)) {
                this.showNoGridsMessage();
                return;
            }

            this.grids = parsedData.grids.map((grid, index) => ({
                ...grid,
                testId: this.startId + index,
                expectedValue: grid.expectedValue || null,
                status: grid.expectedValue ? 'ready' : 'pending'
            }));

            console.log(`✅ ${this.grids.length} griglie caricate`);
            this.notify.success(`${this.grids.length} griglie caricate`);

            this.currentGridIndex = 0;
            this.showGridsInterface();
            this.renderCurrentGrid();
            this.updateStats();

        } catch (error) {
            console.error('Errore nel caricamento delle griglie:', error);
            this.notify.error('Errore nel caricamento delle griglie');
            this.showNoGridsMessage();
        }
    }

    /**
     * Show message when no grids are available
     */
    showNoGridsMessage() {
        const singleGridContainer = document.getElementById('singleGridContainer');
        const noGridsMessage = document.getElementById('noGridsMessage');
        const gridNavigation = document.getElementById('gridNavigation');

        if (singleGridContainer) singleGridContainer.style.display = 'none';
        if (noGridsMessage) noGridsMessage.style.display = 'block';
        if (gridNavigation) gridNavigation.style.display = 'none';

        this.updateStatsEmpty();
    }

    /**
     * Show grids interface
     */
    showGridsInterface() {
        const singleGridContainer = document.getElementById('singleGridContainer');
        const noGridsMessage = document.getElementById('noGridsMessage');
        const gridNavigation = document.getElementById('gridNavigation');

        if (singleGridContainer) singleGridContainer.style.display = 'block';
        if (noGridsMessage) noGridsMessage.style.display = 'none';
        if (gridNavigation && this.grids.length > 1) {
            gridNavigation.style.display = 'flex';
            this.updateNavigationControls();
        }
    }

    /**
     * Update navigation controls
     */
    updateNavigationControls() {
        const currentGridIndexEl = document.getElementById('currentGridIndex');
        const totalGridsNavEl = document.getElementById('totalGridsNav');
        const gotoGridInput = document.getElementById('gotoGridInput');
        const prevBtn = document.getElementById('prevGridBtn');
        const nextBtn = document.getElementById('nextGridBtn');

        if (currentGridIndexEl) currentGridIndexEl.textContent = this.currentGridIndex + 1;
        if (totalGridsNavEl) totalGridsNavEl.textContent = this.grids.length;

        if (gotoGridInput) {
            gotoGridInput.value = this.currentGridIndex + 1;
            gotoGridInput.max = this.grids.length;
        }

        if (prevBtn) prevBtn.disabled = this.currentGridIndex === 0;
        if (nextBtn) nextBtn.disabled = this.currentGridIndex === this.grids.length - 1;
    }

    /**
     * Navigate to specific grid
     */
    navigateGrid(direction) {
        const newIndex = this.currentGridIndex + direction;
        if (newIndex >= 0 && newIndex < this.grids.length) {
            this.currentGridIndex = newIndex;
            this.renderCurrentGrid();
            this.updateNavigationControls();
        }
    }

    /**
     * Go to specific grid by index
     */
    gotoGrid(index) {
        const gridIndex = parseInt(index);
        if (gridIndex >= 0 && gridIndex < this.grids.length) {
            this.currentGridIndex = gridIndex;
            this.renderCurrentGrid();
            this.updateNavigationControls();
        }
    }

    /**
     * Render current grid with all its information
     */
    renderCurrentGrid() {
        if (this.grids.length === 0 || this.currentGridIndex >= this.grids.length) {
            this.showNoGridsMessage();
            return;
        }

        const grid = this.grids[this.currentGridIndex];
        const testId = this.startId + this.currentGridIndex;

        // Update grid information
        this.updateGridInformation(grid, testId);

        // Render matrix using Grid Component with adaptive sizing
        this.renderGridMatrix(grid);

        // Update expected value controls
        this.updateExpectedControls(grid);

        // Update expected value grid
        this.renderExpectedValueGrid(testId, grid.expectedValue);

        console.log(`Grid ${this.currentGridIndex + 1} rendered`);
    }

    /**
     * Update grid information panel
     */
    updateGridInformation(grid, testId) {
        // Header information
        this.updateElement('currentGridTitle', `Test-${testId} - Grid Matrix`);
        this.updateElement('currentGridDimensions', `${grid.width}×${grid.height}`);

        // Status badge
        const statusElement = document.getElementById('currentGridStatus');
        if (statusElement) {
            const statusInfo = this.getStatusInfo(grid.status);
            statusElement.className = `badge ${statusInfo.class}`;
            statusElement.textContent = statusInfo.text;
        }

        // Detail information
        this.updateElement('currentTestId', testId);
        this.updateElement('currentDimensions', `${grid.width}×${grid.height}`);
        this.updateElement('mySnakeLength', grid.analysis?.mySnakeLength || 0);
        this.updateElement('enemyCount', grid.analysis?.totalEnemies || 0);
        this.updateElement('foodCount', grid.analysis?.food?.length || 0);
        this.updateElement('hazardCount', grid.analysis?.hazards?.length || 0);
    }

    /**
     * Render grid matrix using Grid Component with responsive sizing
     */
    renderGridMatrix(grid) {
        const matrixContainer = document.getElementById('gridMatrixContainer');
        if (!matrixContainer || !this.gridComponent) {
            console.error('Matrix container or grid component not found');
            return;
        }

        // Get container dimensions
        const containerRect = matrixContainer.getBoundingClientRect();
        const containerWidth = containerRect.width || 400;
        const containerHeight = containerRect.height || 300;

        // Calculate optimal sizing for the grid
        const optimalSize = this.gridComponent.calculateOptimalSize(
            containerWidth,
            containerHeight,
            grid.width,
            grid.height
        );

        // Add expected value and head position for direction overlay
        optimalSize.expectedValue = grid.expectedValue;
        optimalSize.myHead = grid.analysis?.myHead;

        console.log('🎯 Rendering grid with expected value:', grid.expectedValue, 'and head:', grid.analysis?.myHead);

        // Render the matrix with optimal sizing and direction overlays
        const matrixHTML = this.gridComponent.render(grid, optimalSize);
        matrixContainer.innerHTML = matrixHTML;
    }

    /**
     * Update expected value controls
     */
    updateExpectedControls(grid) {
        const expectedDisplay = document.getElementById('currentExpectedDisplay');
        if (expectedDisplay) {
            if (grid.expectedValue) {
                expectedDisplay.innerHTML = `<span class="badge bg-success">${grid.expectedValue} - ${this.expectedLabels[grid.expectedValue]}</span>`;
            } else {
                expectedDisplay.innerHTML = `<span class="badge bg-warning">Not Set</span>`;
            }
        }
    }

    /**
     * Render expected value grid buttons
     */
    renderExpectedValueGrid(testId, currentValue) {
        const container = document.getElementById('expectedValueGrid');
        if (!container) return;

        const buttonGroups = [
            [1, 2, 3, 4, 5],
            [6, 7, 8, 9, 10],
            [11, 12, 13, 14, 15]
        ];

        let html = '';
        buttonGroups.forEach(group => {
            group.forEach(value => {
                const isSelected = currentValue === value;
                const btnClass = isSelected ? 'btn-primary' : 'btn-outline-primary';
                const icon = this.getDirectionIcon(value);

                html += `
                    <div class="col-4 col-md-3 col-lg-4">
                        <button class="btn ${btnClass} w-100 btn-sm expected-btn" 
                                data-value="${value}"
                                style="font-size: 10px; padding: 4px 2px; min-height: 35px;"
                                onclick="window.snake.processTabManager.setCurrentExpectedValue(${value})"
                                title="${this.expectedLabels[value]}">
                            <div style="font-size: 11px; font-weight: bold;">${value}</div>
                            <div style="font-size: 10px;">${icon}</div>
                        </button>
                    </div>
                `;
            });
        });

        container.innerHTML = html;
    }

    /**
     * Get direction icon for value
     */
    getDirectionIcon(value) {
        const icons = {
            1: "⬆️", 2: "⬇️", 3: "⬆️⬇️", 4: "⬅️", 5: "⬆️⬅️",
            6: "⬇️⬅️", 7: "⬆️⬇️⬅️", 8: "➡️", 9: "⬆️➡️", 10: "⬇️➡️",
            11: "⬆️⬇️➡️", 12: "⬅️➡️", 13: "⬆️⬅️➡️", 14: "⬇️⬅️➡️", 15: "⬆️⬇️⬅️➡️"
        };
        return icons[value] || "?";
    }

    /**
     * Get status information
     */
    getStatusInfo(status) {
        const statusMap = {
            pending: { class: 'bg-warning text-dark', text: '⏳ Pending' },
            ready: { class: 'bg-success', text: '✅ Ready' },
            failed: { class: 'bg-danger', text: '❌ Failed' }
        };
        return statusMap[status] || { class: 'bg-secondary', text: '❓ Unknown' };
    }

    /**
     * Set expected value for current grid
     */
    setCurrentExpectedValue(value) {
        if (this.currentGridIndex >= 0 && this.currentGridIndex < this.grids.length) {
            this.grids[this.currentGridIndex].expectedValue = value;
            this.grids[this.currentGridIndex].status = 'ready';

            // Update display immediately
            this.updateExpectedControls(this.grids[this.currentGridIndex]);
            this.renderExpectedValueGrid(this.startId + this.currentGridIndex, value);

            // IMPORTANT: Re-render the grid matrix to show direction overlays
            this.renderGridMatrix(this.grids[this.currentGridIndex]);

            // Also update the status badge
            this.updateGridInformation(this.grids[this.currentGridIndex], this.startId + this.currentGridIndex);

            // Update stats and save
            this.updateStats();
            this.saveGridsToStorage();

            this.notify.success(`Expected value ${value} - ${this.expectedLabels[value]} set for current grid`);
        }
    }

    /**
     * Calculate expected value based on grid analysis (safe moves)
     */
    calculateExpected() {
        const grid = this.grids[this.currentGridIndex];
        if (!grid) return;

        if (!grid.analysis?.myHead) {
            this.notify.warning('Cannot calculate expected value: missing snake head position');
            return;
        }

        const head = grid.analysis.myHead;
        const gridWidth = grid.width;
        const gridHeight = grid.height;

        // Check which directions are safe/valid
        const safeMoves = [];

        // Check UP (y-1)
        if (head.y > 0 && this.isSafeMove(grid, head.x, head.y - 1)) {
            safeMoves.push(1);
        }

        // Check DOWN (y+1)
        if (head.y < gridHeight - 1 && this.isSafeMove(grid, head.x, head.y + 1)) {
            safeMoves.push(2);
        }

        // Check LEFT (x-1)
        if (head.x > 0 && this.isSafeMove(grid, head.x - 1, head.y)) {
            safeMoves.push(4);
        }

        // Check RIGHT (x+1)
        if (head.x < gridWidth - 1 && this.isSafeMove(grid, head.x + 1, head.y)) {
            safeMoves.push(8);
        }

        if (safeMoves.length === 0) {
            this.notify.warning('No safe moves available!');
            return;
        }

        // Calculate suggested value based on safe moves
        let suggestedValue = 0;
        safeMoves.forEach(move => {
            suggestedValue |= move; // Bitwise OR to combine directions
        });

        // If all directions are safe, suggest the primary direction (UP as default)
        if (suggestedValue === 15) { // All directions safe
            suggestedValue = 1; // Default to UP
        }

        const moveNames = safeMoves.map(move => this.expectedLabels[move]).join(', ');

        // Show suggestion dialog
        this.setCurrentExpectedValue(suggestedValue);
    }

    /**
     * Check if a move to the given position is safe
     * @param {Object} grid - Grid data
     * @param {number} x - Target X position
     * @param {number} y - Target Y position
     * @returns {boolean} - True if move is safe
     */
    isSafeMove(grid, x, y) {
        // Check bounds
        if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) {
            return false;
        }

        // Get cell content
        const cell = grid.cells[y][x];

        // Unsafe cells: my body, enemy head, enemy body, hazards
        const unsafeCells = ['B', 'E', 'b', '#'];

        return !unsafeCells.includes(cell);
    }

    /**
     * Preview current test JSON
     */
    previewCurrentTest() {
        const grid = this.grids[this.currentGridIndex];
        if (!grid) return;

        if (!this.jsonFormatter) {
            this.notify.error('JSON Formatter non inizializzato');
            return;
        }

        const testId = this.startId + this.currentGridIndex;
        const testJson = this.jsonFormatter.gridToTest(grid, testId);

        alert(`Test-${testId} JSON Preview:\n\n${JSON.stringify(testJson, null, 2)}`);
    }

    /**
     * Duplicate current grid
     */
    duplicateCurrentGrid() {
        const grid = this.grids[this.currentGridIndex];
        if (!grid) return;

        const duplicatedGrid = {
            ...JSON.parse(JSON.stringify(grid)), // Deep clone
            id: `grid_${Date.now()}_duplicated`,
            index: this.grids.length + 1,
            status: 'imported',
            timestamp: new Date().toISOString(),
            expectedValue: null // Reset expected value for duplicate
        };

        this.grids.push(duplicatedGrid);
        this.currentGridIndex = this.grids.length - 1; // Switch to new grid
        this.renderCurrentGrid();
        this.updateNavigationControls();
        this.updateStats();
        this.saveGridsToStorage();

        this.notify.success('Grid duplicated successfully');
    }

    /**
     * Delete current grid
     */
    deleteCurrentGrid() {
        if (this.grids.length === 0) return;

        const testId = this.startId + this.currentGridIndex;
        if (confirm(`Sei sicuro di voler eliminare Test-${testId}?`)) {
            this.grids.splice(this.currentGridIndex, 1);

            // Adjust current index if necessary
            if (this.currentGridIndex >= this.grids.length) {
                this.currentGridIndex = Math.max(0, this.grids.length - 1);
            }

            if (this.grids.length === 0) {
                this.showNoGridsMessage();
            } else {
                this.renderCurrentGrid();
                this.updateNavigationControls();
            }

            this.updateStats();
            this.saveGridsToStorage();
            this.notify.success('Grid eliminata');
        }
    }

    /**
     * Delete all grids
     */
    deleteGrids() {
        if (confirm('Sei sicuro di voler eliminare TUTTE le griglie?')) {
            this.grids = [];
            this.currentGridIndex = 0;
            this.showNoGridsMessage();
            this.updateStats();
            this.saveGridsToStorage();
            this.notify.success('Tutte le griglie eliminate');
        }
    }

    /**
     * Change start ID
     */
    changeStartId(delta) {
        const input = document.getElementById('startIdInput');
        if (input) {
            const newValue = Math.max(1, Math.min(9999, parseInt(input.value) + delta));
            input.value = newValue;
            this.updateStartId();
        }
    }

    /**
     * Update start ID
     */
    updateStartId() {
        const input = document.getElementById('startIdInput');
        if (input) {
            this.startId = parseInt(input.value) || 101;
            this.renderCurrentGrid(); // Re-render with new ID
        }
    }

    /**
     * Generate all tests JSON
     */
    generateAllTests() {
        console.log('🚀 Generate All Tests chiamato');

        if (!this.jsonFormatter) {
            this.notify.error('JSON Formatter non inizializzato');
            return;
        }

        const readyGrids = this.grids.filter(g => g.expectedValue);
        console.log(`📊 Griglie pronte: ${readyGrids.length}/${this.grids.length}`);

        if (readyGrids.length === 0) {
            this.notify.warning('Nessuna griglia pronta. Imposta i valori Expected prima di generare il JSON.');
            return;
        }

        const tests = readyGrids.map((grid, index) => {
            const testId = this.startId + this.grids.indexOf(grid);
            console.log(`🔧 Generando test per griglia ${grid.index} con ID ${testId}`);
            return this.jsonFormatter.gridToTest(grid, testId);
        });

        console.log(`✅ ${tests.length} test generati`);
        this.showJsonModal(tests);
    }

    /**
     * Show JSON modal
     */
    showJsonModal(tests) {
        const output = document.getElementById('jsonOutput');
        const count = document.getElementById('jsonTestCount');
        const modalElement = document.getElementById('jsonOutputModal');

        if (output && count && modalElement) {
            output.textContent = JSON.stringify(tests, null, 2);
            count.textContent = tests.length;

            // Store for copy/download
            this.generatedJson = tests;

            // Show modal
            try {
                if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                    const modal = new bootstrap.Modal(modalElement);
                    modal.show();
                } else {
                    modalElement.style.display = 'block';
                    modalElement.classList.add('show');
                    document.body.classList.add('modal-open');

                    const backdrop = document.createElement('div');
                    backdrop.className = 'modal-backdrop fade show';
                    backdrop.id = 'json-modal-backdrop';
                    document.body.appendChild(backdrop);
                }
            } catch (error) {
                console.error('Modal error:', error);
                alert(`Generated ${tests.length} test cases:\n\n${JSON.stringify(tests, null, 2).substring(0, 1000)}...`);
            }
        } else {
            console.error('Modal elements not found');
            this.notify.error('Errore nella visualizzazione del JSON');
        }
    }

    /**
     * Close JSON modal
     */
    closeJsonModal() {
        const modalElement = document.getElementById('jsonOutputModal');
        const backdrop = document.getElementById('json-modal-backdrop');

        if (modalElement) {
            modalElement.style.display = 'none';
            modalElement.classList.remove('show');
            document.body.classList.remove('modal-open');

            if (backdrop) {
                backdrop.remove();
            }
        }
    }

    /**
     * Copy JSON to clipboard
     */
    async copyJsonToClipboard() {
        if (this.generatedJson) {
            try {
                const jsonString = JSON.stringify(this.generatedJson, null, 2);
                await navigator.clipboard.writeText(jsonString);
                this.notify.success('JSON copiato negli appunti');
            } catch (error) {
                console.error('Clipboard error:', error);
                const textArea = document.createElement('textarea');
                textArea.value = JSON.stringify(this.generatedJson, null, 2);
                document.body.appendChild(textArea);
                textArea.select();
                try {
                    document.execCommand('copy');
                    this.notify.success('JSON copiato negli appunti (fallback)');
                } catch (e) {
                    this.notify.error('Impossibile copiare negli appunti');
                }
                document.body.removeChild(textArea);
            }
        } else {
            this.notify.warning('Nessun JSON da copiare');
        }
    }

    /**
     * Download JSON file
     */
    downloadJson() {
        if (this.generatedJson) {
            try {
                const jsonString = JSON.stringify(this.generatedJson, null, 2);
                const blob = new Blob([jsonString], { type: 'application/json' });
                const url = URL.createObjectURL(blob);
                const link = document.createElement('a');

                link.href = url;
                link.download = `battlesnake-tests-${new Date().toISOString().split('T')[0]}.json`;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);

                URL.revokeObjectURL(url);
                this.notify.success('JSON scaricato con successo');
            } catch (error) {
                console.error('Download error:', error);
                this.notify.error('Errore durante il download');
            }
        } else {
            this.notify.warning('Nessun JSON da scaricare');
        }
    }

    /**
     * Refresh grids
     */
    refreshGrids() {
        this.loadGridsFromStorage();
    }

    /**
     * Update statistics
     */
    updateStats() {
        const stats = {
            total: this.grids.length,
            pending: this.grids.filter(g => !g.expectedValue).length,
            ready: this.grids.filter(g => g.expectedValue && g.status === 'ready').length,
            failed: this.grids.filter(g => g.status === 'failed').length
        };

        this.updateElement('totalGrids', stats.total);
        this.updateElement('pendingGrids', stats.pending);
        this.updateElement('readyGrids', stats.ready);
        this.updateElement('failedGrids', stats.failed);
    }

    /**
     * Update stats when empty
     */
    updateStatsEmpty() {
        this.updateElement('totalGrids', 0);
        this.updateElement('pendingGrids', 0);
        this.updateElement('readyGrids', 0);
        this.updateElement('failedGrids', 0);
    }

    /**
     * Update element text content
     */
    updateElement(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = value;
        }
    }

    /**
     * Save grids to localStorage
     */
    saveGridsToStorage() {
        try {
            const data = {
                grids: this.grids,
                timestamp: new Date().toISOString(),
                version: '1.0',
                totalGrids: this.grids.length
            };

            localStorage.setItem(this.STORAGE_KEY, JSON.stringify(data));
            console.log('✅ Griglie aggiornate nel localStorage');

            // Update badge in tab
            if (window.snake?.updateGridsBadge) {
                window.snake.updateGridsBadge();
            }

        } catch (error) {
            console.error('Errore nel salvataggio:', error);
            this.notify.error('Errore nel salvataggio delle griglie');
        }
    }

    // Aggiungi questo metodo alla classe ProcessTabManager

    /**
     * Calculate expected value for all grids
     */
    calculateAllExpected() {
        console.log('🧮 Calculate All Expected chiamato...');

        if (this.grids.length === 0) {
            this.notify.warning('Nessuna griglia da processare');
            return;
        }

        let calculatedCount = 0;
        let errorCount = 0;
        const totalGrids = this.grids.length;

        // Cicla tutte le griglie
        for (let i = 0; i < this.grids.length; i++) {
            // Imposta la griglia corrente
            this.currentGridIndex = i;

            try {
                // Chiama il metodo calculate esistente
                this.calculateExpected();
                calculatedCount++;
            } catch (error) {
                console.error(`Errore calcolando griglia ${i + 1}:`, error);
                errorCount++;
            }
        }

        // Torna alla prima griglia e aggiorna la visualizzazione
        this.currentGridIndex = 0;
        this.renderCurrentGrid();
        this.updateNavigationControls();

        // Notifica risultati
        this.notify.success(`Calculate All completato: ${calculatedCount}/${totalGrids} griglie processate (${errorCount} errori)`);

        console.log(`✅ Calculate All completato: ${calculatedCount} successi, ${errorCount} errori`);
    }
    
    /**
     * Check if initialized
     */
    isInitialized() {
        return this.initialized;
    }
}

// Export for global access
if (typeof window !== 'undefined') {
    window.ProcessTabManager = ProcessTabManager;
}