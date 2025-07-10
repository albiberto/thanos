/**
 * Process Tab - Complete Grid Processing and Display
 * File: js/tabs/process-tab.js
 * Loads grids from localStorage and displays them in a structured way
 */
class ProcessTabManager {
    constructor(containerId, notifyService) {
        this.containerId = containerId;
        this.notify = notifyService;
        this.grids = [];
        this.filteredGrids = [];
        this.currentFilter = { status: '', size: '', search: '' };
        this.initialized = false;
        this.processingQueue = [];

        // Storage key per le griglie (deve essere uguale a ImportTabManager)
        this.STORAGE_KEY = 'battlesnake_grids';
    }

    // Getter per il container
    get container() {
        return document.getElementById(this.containerId);
    }

    /**
     * Initialize the process tab
     */
    init() {
        if (this.initialized) return;

        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.doInit());
        } else {
            this.doInit();
        }
    }

    /**
     * Actual initialization after DOM is ready
     */
    doInit() {
        try {
            this.setupEventListeners();
            this.initialized = true;
            console.log('✅ ProcessTabManager initialized');
        } catch (error) {
            console.error('Failed to initialize ProcessTabManager:', error);
            this.notify.error('Errore nell\'inizializzazione del Process Tab');
        }
    }

    /**
     * Setup event listeners
     */
    setupEventListeners() {
        console.log('✅ Event listeners setup per process tab');
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

            this.grids = parsedData.grids;
            this.filteredGrids = [...this.grids];

            console.log(`✅ ${this.grids.length} griglie caricate`);
            this.notify.success(`${this.grids.length} griglie caricate dal localStorage`);

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
        const container = document.getElementById('processorsList');
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
     * Render grids in the UI
     */
    renderGrids() {
        const container = document.getElementById('processorsList');
        if (!container) return;

        if (this.filteredGrids.length === 0) {
            container.innerHTML = `
                <div class="col-12">
                    <div class="alert alert-warning text-center">
                        <h5>🔍 Nessuna griglia trovata con i filtri correnti</h5>
                        <button class="btn btn-outline-secondary" onclick="window.snake.processTabManager.clearFilters()">
                            🗑️ Pulisci Filtri
                        </button>
                    </div>
                </div>
            `;
            return;
        }

        const gridsHTML = this.filteredGrids.map(grid => this.renderGridCard(grid)).join('');
        container.innerHTML = gridsHTML;

        // Bind grid actions after rendering
        this.bindGridActions();
    }

    /**
     * Render a single grid card
     */
    renderGridCard(grid) {
        const statusBadge = this.getStatusBadge(grid.status);
        const sizeLabel = this.getSizeLabel(grid.width, grid.height);
        const analysisText = this.getAnalysisText(grid.analysis);

        return `
            <div class="col-lg-6 col-xl-4">
                <div class="card h-100 border-2 grid-card" data-status="${grid.status}" data-id="${grid.id}">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <h6 class="card-title mb-0 fw-semibold">Griglia ${grid.index}</h6>
                        ${statusBadge}
                    </div>
                    <div class="card-body d-flex flex-column">
                        <p class="card-text small text-muted mb-2">${sizeLabel}</p>
                        <p class="card-text small mb-3">${analysisText}</p>

                        <!-- Grid Preview -->
                        <div class="mb-3 p-2 bg-light border rounded">
                            <div class="small text-muted mb-1">Anteprima:</div>
                            <div class="grid-preview" style="font-family: monospace; font-size: 10px; line-height: 1.2; max-height: 80px; overflow: hidden;">
                                ${this.renderGridPreview(grid.cells)}
                            </div>
                        </div>

                        <!-- Metrics -->
                        <div class="row g-2 mb-3 text-center">
                            <div class="col-4">
                                <div class="fw-semibold">${grid.width}×${grid.height}</div>
                                <div class="small text-muted">Dimensioni</div>
                            </div>
                            <div class="col-4">
                                <div class="fw-semibold">${grid.analysis.mySnakeLength || 0}</div>
                                <div class="small text-muted">Mio Snake</div>
                            </div>
                            <div class="col-4">
                                <div class="fw-semibold">${grid.analysis.totalEnemies || 0}</div>
                                <div class="small text-muted">Nemici</div>
                            </div>
                        </div>

                        <!-- Actions -->
                        <div class="d-flex flex-wrap gap-1 mt-auto">
                            <button class="btn btn-outline-info btn-sm" data-action="view" data-id="${grid.id}">
                                👁️ Dettagli
                            </button>
                            ${this.getActionButtons(grid)}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Get action buttons based on grid status
     */
    getActionButtons(grid) {
        let buttons = '';

        if (grid.status === 'imported') {
            buttons += `<button class="btn btn-outline-success btn-sm" data-action="process" data-id="${grid.id}">⚙️ Processa</button>`;
        } else if (grid.status === 'processing') {
            buttons += `<button class="btn btn-outline-warning btn-sm" disabled>⏳ In corso...</button>`;
        } else if (grid.status === 'processed') {
            buttons += `<button class="btn btn-outline-primary btn-sm" data-action="reprocess" data-id="${grid.id}">🔄 Riprocessa</button>`;
        }

        buttons += `
            <button class="btn btn-outline-secondary btn-sm" data-action="export" data-id="${grid.id}">💾 Esporta</button>
            <button class="btn btn-outline-danger btn-sm" data-action="delete" data-id="${grid.id}">🗑️ Elimina</button>
        `;

        return buttons;
    }

    /**
     * Render grid preview (first 4 rows max)
     */
    renderGridPreview(cells) {
        const previewRows = cells.slice(0, 4);
        return previewRows.map(row => row.join(' ')).join('<br>') +
            (cells.length > 4 ? '<br>...' : '');
    }

    /**
     * Get status badge HTML
     */
    getStatusBadge(status) {
        const badges = {
            imported: '<span class="badge bg-primary">📥 Importata</span>',
            processing: '<span class="badge bg-warning text-dark">⚙️ In Elaborazione</span>',
            processed: '<span class="badge bg-success">✅ Processata</span>',
            error: '<span class="badge bg-danger">❌ Errore</span>'
        };
        return badges[status] || '<span class="badge bg-secondary">❓ Sconosciuto</span>';
    }

    /**
     * Get size label
     */
    getSizeLabel(width, height) {
        const size = width * height;
        if (size <= 100) return `📏 Piccola (${width}×${height})`;
        if (size <= 400) return `📏 Media (${width}×${height})`;
        return `📏 Grande (${width}×${height})`;
    }

    /**
     * Get analysis text
     */
    getAnalysisText(analysis) {
        const parts = [];
        if (analysis.myHead) parts.push('👽 Mio snake');
        if (analysis.totalEnemies > 0) parts.push(`😈 ${analysis.totalEnemies} nemici`);
        if (analysis.food && analysis.food.length > 0) parts.push(`🍎 ${analysis.food.length} cibo`);
        if (analysis.hazards && analysis.hazards.length > 0) parts.push(`💀 ${analysis.hazards.length} pericoli`);

        return parts.length > 0 ? parts.join(', ') : 'Griglia vuota';
    }

    /**
     * Bind grid action buttons
     */
    bindGridActions() {
        const container = document.getElementById('processorsList');
        if (!container) return;

        container.addEventListener('click', (e) => {
            const button = e.target.closest('button[data-action]');
            if (!button) return;

            const action = button.getAttribute('data-action');
            const id = button.getAttribute('data-id');

            this.handleGridAction(action, id);
        });
    }

    /**
     * Handle grid actions
     */
    handleGridAction(action, gridId) {
        const grid = this.grids.find(g => g.id === gridId);
        if (!grid) {
            this.notify.error('Griglia non trovata');
            return;
        }

        console.log(`🎯 Azione "${action}" su griglia ${grid.index}`);

        switch (action) {
            case 'view':
                this.viewGridDetails(grid);
                break;
            case 'process':
                this.processGrid(grid);
                break;
            case 'reprocess':
                this.reprocessGrid(grid);
                break;
            case 'export':
                this.exportGrid(grid);
                break;
            case 'delete':
                this.deleteGrid(grid);
                break;
        }
    }

    /**
     * View grid details
     */
    viewGridDetails(grid) {
        const analysis = grid.analysis;
        const details = `
GRIGLIA ${grid.index} - DETTAGLI COMPLETI
${'='.repeat(40)}

📏 DIMENSIONI:
• Larghezza: ${grid.width} celle
• Altezza: ${grid.height} celle  
• Totale celle: ${analysis.totalCells || (grid.width * grid.height)}

📊 STATUS:
• Stato: ${this.getStatusText(grid.status)}
• Importata: ${new Date(grid.importedAt || grid.timestamp).toLocaleString()}
${grid.processedAt ? `• Processata: ${new Date(grid.processedAt).toLocaleString()}` : ''}

🐍 ANALISI SNAKE:
• Mia testa: ${analysis.myHead ? `(${analysis.myHead.x}, ${analysis.myHead.y})` : 'Non trovata'}
• Mio corpo: ${analysis.myBody ? analysis.myBody.length : 0} segmenti
• Lunghezza totale: ${analysis.mySnakeLength || 0}

😈 NEMICI:
• Teste nemiche: ${analysis.totalEnemies || 0}
• Corpi nemici: ${analysis.totalEnemyBodies || 0}

🍎 RISORSE:
• Cibo disponibile: ${analysis.food ? analysis.food.length : 0}
• Pericoli: ${analysis.hazards ? analysis.hazards.length : 0}
• Celle vuote: ${analysis.empty ? analysis.empty.length : 0} (${analysis.emptyPercentage || 0}%)

🎯 DIREZIONI:
• Indicazioni movimento: ${analysis.directions ? analysis.directions.length : 0}

📝 GRIGLIA COMPLETA:
${grid.rawText}
        `;

        // Crea un modal o alert migliorato
        this.showModal('Dettagli Griglia', details);
        this.notify.info(`Dettagli griglia ${grid.index} visualizzati`);
    }

    /**
     * Show modal with content
     */
    showModal(title, content) {
        // Fallback con alert per ora, in futuro si potrebbe usare un modal Bootstrap
        alert(`${title}\n\n${content}`);
    }

    /**
     * Get status text
     */
    getStatusText(status) {
        const statusTexts = {
            imported: '📥 Importata',
            processing: '⚙️ In elaborazione',
            processed: '✅ Processata',
            error: '❌ Errore'
        };
        return statusTexts[status] || '❓ Sconosciuto';
    }

    /**
     * Process grid
     */
    processGrid(grid) {
        if (grid.status === 'processing') {
            this.notify.warning('Griglia già in elaborazione');
            return;
        }

        grid.status = 'processing';
        this.notify.info(`⚙️ Elaborazione griglia ${grid.index} avviata...`);

        // Simula elaborazione
        setTimeout(() => {
            // Simula elaborazione con possibilità di errore (5% di probabilità)
            if (Math.random() < 0.05) {
                grid.status = 'error';
                grid.errorMessage = 'Errore simulato durante l\'elaborazione';
                this.notify.error(`❌ Errore nell'elaborazione della griglia ${grid.index}`);
            } else {
                grid.status = 'processed';
                grid.processedAt = new Date().toISOString();
                grid.processingResult = this.generateProcessingResult(grid);
                this.notify.success(`✅ Griglia ${grid.index} processata con successo`);
            }

            this.renderGrids();
            this.updateStats();
            this.saveGridsToStorage();
        }, Math.random() * 2000 + 1000); // 1-3 secondi

        this.renderGrids();
        this.updateStats();
    }

    /**
     * Reprocess grid
     */
    reprocessGrid(grid) {
        grid.status = 'imported';
        delete grid.processedAt;
        delete grid.processingResult;
        delete grid.errorMessage;

        this.notify.info(`🔄 Griglia ${grid.index} reimpostata per riprocessamento`);
        this.processGrid(grid);
    }

    /**
     * Generate mock processing result
     */
    generateProcessingResult(grid) {
        return {
            processedAt: new Date().toISOString(),
            bestMove: ['up', 'down', 'left', 'right'][Math.floor(Math.random() * 4)],
            confidence: Math.floor(Math.random() * 100),
            analysisTime: Math.floor(Math.random() * 500) + 50,
            strategicValue: Math.floor(Math.random() * 10) + 1
        };
    }

    /**
     * Export grid
     */
    exportGrid(grid) {
        try {
            const exportData = {
                ...grid,
                exportedAt: new Date().toISOString(),
                exportVersion: '1.0'
            };

            const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');

            link.href = url;
            link.download = `battlesnake_grid_${grid.index}_${Date.now()}.json`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            URL.revokeObjectURL(url);

            this.notify.success(`💾 Griglia ${grid.index} esportata con successo`);

        } catch (error) {
            console.error('Export error:', error);
            this.notify.error('Errore durante l\'esportazione');
        }
    }

    /**
     * Delete grid
     */
    deleteGrid(grid) {
        if (confirm(`Sei sicuro di voler eliminare la griglia ${grid.index}?\n\nQuesta azione non può essere annullata.`)) {
            this.grids = this.grids.filter(g => g.id !== grid.id);
            this.applyFilters();
            this.updateStats();
            this.saveGridsToStorage();
            this.notify.success(`🗑️ Griglia ${grid.index} eliminata`);
        }
    }

    /**
     * Process all grids
     */
    processAllGrids() {
        const pendingGrids = this.grids.filter(g => g.status === 'imported');

        if (pendingGrids.length === 0) {
            this.notify.warning('Nessuna griglia da processare');
            return;
        }

        if (!confirm(`Vuoi processare tutte le ${pendingGrids.length} griglie in attesa?\n\nQuesta operazione potrebbe richiedere alcuni minuti.`)) {
            return;
        }

        this.notify.info(`⚙️ Avvio elaborazione di ${pendingGrids.length} griglie...`);

        // Mostra progress bar
        this.showProgressBar(true);

        let processed = 0;
        const total = pendingGrids.length;

        // Aggiorna progress bar
        this.updateProgress(0, total);

        pendingGrids.forEach((grid, index) => {
            setTimeout(() => {
                // Simula elaborazione
                if (Math.random() < 0.05) {
                    grid.status = 'error';
                    grid.errorMessage = 'Errore durante elaborazione batch';
                } else {
                    grid.status = 'processed';
                    grid.processedAt = new Date().toISOString();
                    grid.processingResult = this.generateProcessingResult(grid);
                }

                processed++;
                this.updateProgress(processed, total);

                if (processed === total) {
                    const successful = pendingGrids.filter(g => g.status === 'processed').length;
                    const errors = pendingGrids.filter(g => g.status === 'error').length;

                    this.renderGrids();
                    this.updateStats();
                    this.saveGridsToStorage();
                    this.showProgressBar(false);

                    this.notify.success(`✅ Elaborazione completata: ${successful} successi, ${errors} errori`);
                }
            }, index * 500); // Stagger processing
        });

        // Update UI immediately to show processing status
        pendingGrids.forEach(grid => grid.status = 'processing');
        this.renderGrids();
        this.updateStats();
    }

    /**
     * Show/hide progress bar
     */
    showProgressBar(show) {
        const progressSection = document.getElementById('progressSection');
        if (progressSection) {
            progressSection.style.display = show ? 'block' : 'none';
        }
    }

    /**
     * Update progress bar
     */
    updateProgress(current, total) {
        const percentage = Math.round((current / total) * 100);

        const progressBar = document.getElementById('progressBar');
        const progressText = document.getElementById('progressText');

        if (progressBar) {
            progressBar.style.width = `${percentage}%`;
            progressBar.setAttribute('aria-valuenow', percentage);
        }

        if (progressText) {
            progressText.textContent = `${current}/${total} (${percentage}%)`;
        }
    }

    /**
     * Apply filters
     */
    applyFilters() {
        const statusFilter = document.getElementById('statusFilter')?.value || '';
        const sizeFilter = document.getElementById('sizeFilter')?.value || '';
        const searchFilter = document.getElementById('searchFilter')?.value.toLowerCase() || '';

        this.currentFilter = { status: statusFilter, size: sizeFilter, search: searchFilter };

        this.filteredGrids = this.grids.filter(grid => {
            // Status filter
            if (statusFilter && grid.status !== statusFilter) return false;

            // Size filter
            if (sizeFilter) {
                const size = grid.width * grid.height;
                switch (sizeFilter) {
                    case 'small':
                        if (size > 100) return false;
                        break;
                    case 'medium':
                        if (size <= 100 || size > 400) return false;
                        break;
                    case 'large':
                        if (size <= 400) return false;
                        break;
                }
            }

            // Search filter
            if (searchFilter) {
                const searchText = `griglia ${grid.index} ${grid.width}x${grid.height}`.toLowerCase();
                if (!searchText.includes(searchFilter)) return false;
            }

            return true;
        });

        this.renderGrids();

        if (searchFilter || statusFilter || sizeFilter) {
            this.notify.info(`🔍 Filtri applicati: ${this.filteredGrids.length}/${this.grids.length} griglie`);
        }
    }

    /**
     * Clear filters
     */
    clearFilters() {
        const statusFilter = document.getElementById('statusFilter');
        const sizeFilter = document.getElementById('sizeFilter');
        const searchFilter = document.getElementById('searchFilter');

        if (statusFilter) statusFilter.value = '';
        if (sizeFilter) sizeFilter.value = '';
        if (searchFilter) searchFilter.value = '';

        this.currentFilter = { status: '', size: '', search: '' };
        this.filteredGrids = [...this.grids];
        this.renderGrids();

        this.notify.success('🗑️ Filtri cancellati');
    }

    /**
     * Refresh grids
     */
    refreshGrids() {
        this.notify.info('🔄 Aggiornamento griglie...');
        this.loadGridsFromStorage();
    }

    /**
     * Update statistics
     */
    updateStats() {
        const stats = {
            total: this.grids.length,
            processed: this.grids.filter(g => g.status === 'processed').length,
            pending: this.grids.filter(g => g.status === 'imported').length,
            error: this.grids.filter(g => g.status === 'error').length
        };

        this.updateStatElement('totalBoards', stats.total);
        this.updateStatElement('processedBoards', stats.processed);
        this.updateStatElement('pendingBoards', stats.pending);
        this.updateStatElement('errorBoards', stats.error);
    }

    /**
     * Update stats when empty
     */
    updateStatsEmpty() {
        this.updateStatElement('totalBoards', 0);
        this.updateStatElement('processedBoards', 0);
        this.updateStatElement('pendingBoards', 0);
        this.updateStatElement('errorBoards', 0);
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
     * Save grids back to localStorage
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
     * Clear all grids
     */
    clearAllGrids() {
        if (!confirm('Sei sicuro di voler eliminare TUTTE le griglie?\n\nQuesta azione non può essere annullata.')) {
            return;
        }

        try {
            localStorage.removeItem(this.STORAGE_KEY);
            this.grids = [];
            this.filteredGrids = [];
            this.showNoGridsMessage();
            this.notify.success('🗑️ Tutte le griglie sono state eliminate');

            // Aggiorna il badge nel tab
            if (window.snake?.updateGridsBadge) {
                window.snake.updateGridsBadge();
            }
        } catch (error) {
            this.notify.error('Errore nell\'eliminazione delle griglie');
        }
    }

    /**
     * Tab activation handler
     */
    onTabActivate() {
        console.log('🔄 Process tab attivato');
        this.loadGridsFromStorage();
    }

    /**
     * Tab deactivation handler
     */
    onTabDeactivate() {
        console.log('🔄 Process tab disattivato');
        this.showProgressBar(false);
    }

    /**
     * Initialize
     */
    initialize() {
        console.log('🔧 Inizializzazione ProcessTabManager...');
        this.loadGridsFromStorage();
    }

    /**
     * Check if initialized
     */
    isInitialized() {
        return this.initialized;
    }
}

// Export for global access if needed
if (typeof window !== 'undefined') {
    window.ProcessTabManager = ProcessTabManager;
}