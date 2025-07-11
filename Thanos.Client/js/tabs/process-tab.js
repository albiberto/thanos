/**
 * Process Tab Manager - JSON Test Generation
 * Completely rewritten for matrix visualization and JSON generation
 */
class ProcessTabManager {
    constructor(containerId, notifyService) {
        this.containerId = containerId;
        this.notify = notifyService;
        this.grids = [];
        this.startId = 101;
        this.initialized = false;

        // Storage key per le griglie
        this.STORAGE_KEY = 'battlesnake_grids';

        // JSON formatter instance
        this.jsonFormatter = null;

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

            this.renderGrids();
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
        const container = document.getElementById('gridsContainer');
        if (!container) return;

        container.innerHTML = `
            <div class="col-12">
                <div class="alert alert-info text-center">
                    <h4 class="alert-heading">📭 Nessuna griglia trovata</h4>
                    <p>Non ci sono griglie da processare. Vai al tab Import per importare delle griglie.</p>
                    <hr>
                    <button class="btn btn-outline-primary" onclick="window.snake.tabManager.switchTab('import')">
                        📥 Vai al Tab Import
                    </button>
                </div>
            </div>
        `;

        this.updateStatsEmpty();
    }

    /**
     * Render grids as matrix visualization
     */
    renderGrids() {
        const container = document.getElementById('gridsContainer');
        if (!container) return;

        if (this.grids.length === 0) {
            this.showNoGridsMessage();
            return;
        }

        const gridsHTML = this.grids.map((grid, index) => this.renderGridCard(grid, index)).join('');
        container.innerHTML = gridsHTML;

        // Bind events after rendering
        this.bindGridEvents();
    }

    /**
     * Render single grid card with matrix visualization
     */
    renderGridCard(grid, index) {
        const testId = this.startId + index;
        const statusClass = this.getStatusClass(grid.status);
        const statusBadge = this.getStatusBadge(grid.status);
        const matrixHTML = this.renderMatrix(grid);
        const expectedDisplay = grid.expectedValue ?
            `<span class="badge bg-success">${grid.expectedValue} - ${this.expectedLabels[grid.expectedValue]}</span>` :
            `<span class="badge bg-warning">Not Set</span>`;

        return `
            <div class="col-lg-12 col-xl-6">
                <div class="card h-100 ${statusClass}" data-grid-id="${grid.id}" data-test-id="${testId}">
                    <div class="card-header d-flex justify-content-between align-items-center py-2">
                        <div>
                            <h6 class="card-title mb-0 fw-semibold">Test-${testId}</h6>
                            <small class="text-muted">${grid.width}×${grid.height}</small>
                        </div>
                        ${statusBadge}
                    </div>
                    </div>
                    
                    <div class="card-body d-flex flex-column p-2">
                        <!-- Grid Matrix -->
                        <div class="grid-matrix-container mb-2">
                            <div class="small text-muted mb-1">Grid Matrix:</div>
                            <div class="grid-matrix border rounded p-1" style="font-size: 8px; line-height: 1; font-family: monospace;">
                                ${matrixHTML}
                            </div>
                        </div>

                        <!-- Expected Value Display -->
                        <div class="mb-2 text-center">
                            <div class="small text-muted">Expected Direction:</div>
                            <div id="expectedDisplay-${testId}">${expectedDisplay}</div>
                        </div>

                        <!-- Expected Value Selector -->
                        <div>
                            <div class="row g-2"> 
                                ${this.renderExpectedButtons(testId, grid.expectedValue)}
                            </div>
                        </div>

                        <!-- Actions -->
                        <div class="mt-auto pt-2 border-top">
                            <div class="d-flex gap-1">
                                <button class="btn btn-outline-info btn-sm flex-fill" onclick="window.snake.processTabManager.previewTest(${testId})">
                                    👁️ Preview
                                </button>
                                <button class="btn btn-outline-danger btn-sm" onclick="window.snake.processTabManager.deleteGrid('${grid.id}')">
                                    🗑️
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Render matrix visualization
     */
    renderMatrix(grid) {
        if (!grid.cells || grid.cells.length === 0) {
            return '<div class="text-muted">No matrix data</div>';
        }

        const rows = grid.cells.map(row => {
            const cells = row.map(cell => {
                // Convert emoji to colored cells
                let cellClass = 'matrix-cell';
                let cellContent = '·';

                switch (cell) {
                    case '👽':
                        cellClass += ' my-head';
                        cellContent = 'H';
                        break;
                    case '💲':
                        cellClass += ' my-body';
                        cellContent = 'B';
                        break;
                    case '😈':
                        cellClass += ' enemy-head';
                        cellContent = 'E';
                        break;
                    case '⛔':
                        cellClass += ' enemy-body';
                        cellContent = 'e';
                        break;
                    case '🍎':
                        cellClass += ' food';
                        cellContent = 'F';
                        break;
                    case '💀':
                        cellClass += ' hazard';
                        cellContent = 'X';
                        break;
                    case '⬛':
                    default:
                        cellClass += ' empty';
                        cellContent = '·';
                        break;
                }

                return `<span class="${cellClass}">${cellContent}</span>`;
            }).join('');

            return `<div class="matrix-row">${cells}</div>`;
        }).join('');

        return rows;
    }

    /**
     * Render expected value buttons
     */
    renderExpectedButtons(testId, currentValue) {
        const buttonGroups = [
            [1, 2, 3, 4, 5],
            [6, 7, 8, 9, 10],
            [11, 12, 13, 14, 15]
        ];

        return buttonGroups.map(group => {
            const buttons = group.map(value => {
                const isSelected = currentValue === value;
                const btnClass = isSelected ? 'btn-primary' : 'btn-outline-primary';
                const icon = this.getDirectionIcon(value);

                return `
            <div class="col-2">
                <button class="btn ${btnClass} w-100 btn-square expected-btn" 
                        data-test-id="${testId}" 
                        data-value="${value}"
                        style="font-size: 20px; height: 40px;"
                        onclick="window.snake.processTabManager.setExpectedValue(${testId}, ${value})">
                    <div style="font-size: 15px;">${value}</div>
                    <div style="font-size: 15px;">${icon}</div>
                </button>
            </div>
        `;
            }).join('');

            return `<div class="col-12"><div class="row g-2 justify-content-center">${buttons}</div></div>`;
        }).join('');
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
     * Get status class for card
     */
    getStatusClass(status) {
        const classes = {
            pending: 'border-warning',
            ready: 'border-success',
            failed: 'border-danger'
        };
        return classes[status] || 'border-secondary';
    }

    /**
     * Get status badge
     */
    getStatusBadge(status) {
        const badges = {
            pending: '<span class="badge bg-warning text-dark">⏳ Pending</span>',
            ready: '<span class="badge bg-success">✅ Ready</span>',
            failed: '<span class="badge bg-danger">❌ Failed</span>'
        };
        return badges[status] || '<span class="badge bg-secondary">❓ Unknown</span>';
    }

    /**
     * Bind grid events
     */
    bindGridEvents() {
        // Events are handled by onclick attributes in HTML
        console.log('✅ Grid events bound');
    }

    /**
     * Set expected value for a test
     */
    setExpectedValue(testId, value) {
        const gridIndex = testId - this.startId;
        if (gridIndex >= 0 && gridIndex < this.grids.length) {
            this.grids[gridIndex].expectedValue = value;
            this.grids[gridIndex].status = 'ready';

            // Update display
            const expectedDisplay = document.getElementById(`expectedDisplay-${testId}`);
            if (expectedDisplay) {
                expectedDisplay.innerHTML = `<span class="badge bg-success">${value} - ${this.expectedLabels[value]}</span>`;
            }

            // Update buttons
            const card = document.querySelector(`[data-test-id="${testId}"]`);
            if (card) {
                card.className = card.className.replace(/border-\w+/, 'border-success');

                // Update all buttons in this card
                card.querySelectorAll('.expected-btn').forEach(btn => {
                    const btnValue = parseInt(btn.getAttribute('data-value'));
                    if (btnValue === value) {
                        btn.className = btn.className.replace('btn-outline-primary', 'btn-primary');
                    } else {
                        btn.className = btn.className.replace('btn-primary', 'btn-outline-primary');
                    }
                });

                // Update status badge
                const statusBadge = card.querySelector('.badge');
                if (statusBadge) {
                    statusBadge.className = 'badge bg-success';
                    statusBadge.textContent = '✅ Ready';
                }
            }

            this.updateStats();
            this.saveGridsToStorage();

            this.notify.success(`Expected value ${value} set for Test-${testId}`);
        }
    }

    /**
     * Preview test JSON
     */
    previewTest(testId) {
        if (!this.jsonFormatter) {
            this.notify.error('JSON Formatter non inizializzato');
            return;
        }

        const gridIndex = testId - this.startId;
        if (gridIndex >= 0 && gridIndex < this.grids.length) {
            const grid = this.grids[gridIndex];
            const testJson = this.jsonFormatter.gridToTest(grid, testId);

            alert(`Test-${testId} JSON Preview:\n\n${JSON.stringify(testJson, null, 2)}`);
        }
    }

    /**
     * Delete grid
     */
    deleteGrids() {
        if (confirm('Sei sicuro di voler eliminare questa griglia?')) {
            this.grids = [];
            this.renderGrids();
            this.updateStats();
            this.saveGridsToStorage();
            this.notify.success('Griglia eliminata');
        }
    }

    deleteGrid(gridId) {
        if (confirm('Sei sicuro di voler eliminare questa griglia?')) {
            this.grids = this.grids.filter(g => g.id !== gridId);
            this.renderGrids();
            this.updateStats();
            this.saveGridsToStorage();
            this.notify.success('Griglia eliminata');
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
            this.renderGrids(); // Re-render with new IDs
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

            // Show modal - try different approaches
            try {
                if (typeof bootstrap !== 'undefined' && bootstrap.Modal) {
                    const modal = new bootstrap.Modal(modalElement);
                    modal.show();
                } else {
                    // Fallback - show modal manually
                    modalElement.style.display = 'block';
                    modalElement.classList.add('show');
                    document.body.classList.add('modal-open');

                    // Add backdrop
                    const backdrop = document.createElement('div');
                    backdrop.className = 'modal-backdrop fade show';
                    backdrop.id = 'json-modal-backdrop';
                    document.body.appendChild(backdrop);
                }
            } catch (error) {
                console.error('Modal error:', error);
                // Ultimate fallback - use alert with JSON
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
                // Fallback method
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

        this.updateStatElement('totalGrids', stats.total);
        this.updateStatElement('pendingGrids', stats.pending);
        this.updateStatElement('readyGrids', stats.ready);
        this.updateStatElement('failedGrids', stats.failed);
    }

    /**
     * Update stats when empty
     */
    updateStatsEmpty() {
        this.updateStatElement('totalGrids', 0);
        this.updateStatElement('pendingGrids', 0);
        this.updateStatElement('readyGrids', 0);
        this.updateStatElement('failedGrids', 0);
    }

    /**
     * Update stat element
     */
    updateStatElement(elementId, value) {
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

            // Aggiorna il badge nel tab
            if (window.snake?.updateGridsBadge) {
                window.snake.updateGridsBadge();
            }

        } catch (error) {
            console.error('Errore nel salvataggio:', error);
            this.notify.error('Errore nel salvataggio delle griglie');
        }
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