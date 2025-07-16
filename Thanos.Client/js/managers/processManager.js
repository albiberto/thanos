/**
 * ProcessManager.js - Gestisce il processamento e la visualizzazione delle griglie
 */

import {StorageService} from '../services/StorageService.js';
import {JsonFormatter} from '../utils/JsonFormatter.js';
import {GridMatrixComponent} from '../components/GridMatrix.js';
import {EXPECTED_LABELS, GRID_STATUS, UI_CONFIG} from '../config/constants.js';

export class ProcessManager {
    constructor(notificationService) {
        this.notify = notificationService;
        this.storage = new StorageService();
        this.formatter = new JsonFormatter();
        this.gridComponent = new GridMatrixComponent();

        this.grids = [];
        this.currentIndex = 0;
        this.startId = UI_CONFIG.DEFAULT_START_ID;
    }

    /**
     * Inizializza il manager
     */
    initialize() {
        this.loadGrids();
    }

    /**
     * Carica le griglie dallo storage
     */
    loadGrids() {
        this.grids = this.storage.loadGrids();

        if (this.grids.length === 0) {
            this.showNoGridsMessage();
            return;
        }

        // Assegna test ID
        this.grids.forEach((grid, index) => {
            grid.testId = this.startId + index;
        });

        this.currentIndex = 0;
        this.showGridsInterface();
        this.renderCurrentGrid();
        this.updateStats();

        this.notify.success(`${this.grids.length} griglie caricate`);
    }

    /**
     * Mostra la griglia corrente
     */
    renderCurrentGrid() {
        if (this.grids.length === 0) return;

        const grid = this.grids[this.currentIndex];

        // Aggiorna informazioni
        this.updateGridInfo(grid);

        // Renderizza matrice
        this.renderMatrix(grid);

        // Aggiorna controlli expected value
        this.updateExpectedControls(grid);
    }

    /**
     * Aggiorna informazioni griglia
     */
    updateGridInfo(grid) {
        this.updateElement('currentTestId', grid.testId);
        this.updateElement('currentDimensions', `${grid.width}×${grid.height}`);
        this.updateElement('mySnakeLength', grid.analysis?.mySnakeLength || 0);
        this.updateElement('enemyCount', grid.analysis?.totalEnemies || 0);
        this.updateElement('foodCount', grid.analysis?.food?.length || 0);
        this.updateElement('hazardCount', grid.analysis?.hazards?.length || 0);

        // Status badge
        const statusEl = document.getElementById('currentGridStatus');
        if (statusEl) {
            const statusInfo = this.getStatusInfo(grid.status);
            statusEl.className = `badge ${statusInfo.class}`;
            statusEl.textContent = statusInfo.text;
        }
    }

    /**
     * Renderizza la matrice
     */
    renderMatrix(grid) {
        const container = document.getElementById('gridMatrixContainer');
        if (!container) return;

        container.innerHTML = this.gridComponent.render(grid, {
            expectedValue: grid.expectedValue,
            myHead: grid.analysis?.myHead
        });
    }

    /**
     * Aggiorna controlli expected value
     */
    updateExpectedControls(grid) {
        // Display corrente
        const display = document.getElementById('currentExpectedDisplay');
        if (display) {
            if (grid.expectedValue) {
                display.innerHTML = `<span class="badge bg-success">${grid.expectedValue} - ${EXPECTED_LABELS[grid.expectedValue]}</span>`;
            } else {
                display.innerHTML = `<span class="badge bg-warning">Not Set</span>`;
            }
        }

        // Griglia bottoni
        this.renderExpectedButtons(grid.expectedValue);
    }

    /**
     * Renderizza bottoni expected value
     */
    renderExpectedButtons(currentValue) {
        const container = document.getElementById('expectedValueGrid');
        if (!container) return;

        let html = '';
        for (let value = 1; value <= 15; value++) {
            const isSelected = currentValue === value;
            const btnClass = isSelected ? 'btn-primary' : 'btn-outline-primary';

            html += `
                <div class="col-4">
                    <button class="btn ${btnClass} w-100 btn-sm expected-btn" 
                            onclick="window.processManager.setExpectedValue(${value})"
                            title="${EXPECTED_LABELS[value]}">
                        <div>${value}</div>
                        <div style="font-size: 10px;">${this.getDirectionIcon(value)}</div>
                    </button>
                </div>
            `;
        }

        container.innerHTML = html;
    }

    /**
     * Imposta expected value
     */
    setExpectedValue(value) {
        const grid = this.grids[this.currentIndex];
        if (!grid) return;

        grid.setExpectedValue(value);
        this.storage.saveGrids(this.grids);

        // Re-render per mostrare overlay direzioni
        this.renderCurrentGrid();
        this.updateStats();

        this.notify.success(`Expected value ${value} impostato`);
    }

    /**
     * Calcola expected value automaticamente
     */
    calculateExpected() {
        const grid = this.grids[this.currentIndex];
        if (!grid) return;

        const safeMoves = grid.calculateSafeMoves();

        if (safeMoves.length === 0) {
            this.notify.warning('Nessuna mossa sicura!');
            return;
        }

        // Calcola valore combinato
        let value = 0;
        safeMoves.forEach(move => {
            value |= move.value;
        });

        // Se tutte le direzioni sono sicure, suggerisci UP
        if (value === 15) {
            value = 1;
        }

        this.setExpectedValue(value);
    }

    /**
     * Calcola expected per tutte le griglie
     */
    calculateAllExpected() {
        let processed = 0;
        const total = this.grids.length;

        for (let i = 0; i < total; i++) {
            this.currentIndex = i;
            try {
                this.calculateExpected();
                processed++;
            } catch (error) {
                console.error(`Errore griglia ${i}:`, error);
            }
        }

        this.currentIndex = 0;
        this.renderCurrentGrid();

        this.notify.success(`Processate ${processed}/${total} griglie`);
    }

    /**
     * Naviga tra le griglie
     */
    navigate(direction) {
        const newIndex = this.currentIndex + direction;
        if (newIndex >= 0 && newIndex < this.grids.length) {
            this.currentIndex = newIndex;
            this.renderCurrentGrid();
            this.updateNavigation();
        }
    }

    /**
     * Genera JSON per tutti i test
     */
    generateAllTests() {
        const readyGrids = this.grids.filter(g => g.expectedValue);

        if (readyGrids.length === 0) {
            this.notify.warning('Nessuna griglia pronta');
            return;
        }

        const tests = readyGrids.map(grid =>
            this.formatter.gridToTest(grid, grid.testId)
        );

        this.showJsonModal(tests);
    }

    /**
     * Mostra modal JSON
     */
    showJsonModal(tests) {
        const output = document.getElementById('jsonOutput');
        const count = document.getElementById('jsonTestCount');

        if (output && count) {
            output.textContent = this.formatter.formatTests(tests);
            count.textContent = tests.length;

            // Store per copy/download
            this.generatedTests = tests;

            // Mostra modal
            const modal = new bootstrap.Modal(document.getElementById('jsonOutputModal'));
            modal.show();
        }
    }

    /**
     * Copia JSON negli appunti
     */
    async copyJson() {
        if (!this.generatedTests) return;

        try {
            const json = this.formatter.formatTests(this.generatedTests);
            await navigator.clipboard.writeText(json);
            this.notify.success('JSON copiato');
        } catch (error) {
            this.notify.error('Errore copia');
        }
    }

    /**
     * Download JSON
     */
    downloadJson() {
        if (!this.generatedTests) return;

        const json = this.formatter.formatTests(this.generatedTests);
        const blob = new Blob([json], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = `battlesnake-tests-${Date.now()}.json`;
        link.click();

        URL.revokeObjectURL(url);
        this.notify.success('File scaricato');
    }

    /**
     * Elimina griglia corrente
     */
    deleteCurrentGrid() {
        if (confirm('Eliminare questa griglia?')) {
            this.grids.splice(this.currentIndex, 1);

            if (this.grids.length === 0) {
                this.showNoGridsMessage();
            } else {
                this.currentIndex = Math.min(this.currentIndex, this.grids.length - 1);
                this.renderCurrentGrid();
            }

            this.storage.saveGrids(this.grids);
            this.updateStats();
            this.notify.success('Griglia eliminata');
        }
    }

    /**
     * Elimina tutte le griglie
     */
    deleteAllGrids() {
        if (confirm('Eliminare TUTTE le griglie?')) {
            this.grids = [];
            this.storage.clearGrids();
            this.showNoGridsMessage();
            this.notify.success('Tutte le griglie eliminate');
        }
    }

    /**
     * Aggiorna statistiche
     */
    updateStats() {
        const stats = {
            total: this.grids.length,
            pending: this.grids.filter(g => !g.expectedValue).length,
            ready: this.grids.filter(g => g.status === GRID_STATUS.READY).length,
            failed: this.grids.filter(g => g.status === GRID_STATUS.FAILED).length
        };

        Object.entries(stats).forEach(([key, value]) => {
            this.updateElement(`${key}Grids`, value);
        });
    }

    /**
     * Helper per aggiornare elementi
     */
    updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }

    /**
     * Ottieni info status
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
     * Ottieni icona direzione
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
     * Mostra/nasconde interfacce
     */
    showNoGridsMessage() {
        this.setDisplay('singleGridContainer', 'none');
        this.setDisplay('noGridsMessage', 'block');
        this.setDisplay('gridNavigation', 'none');
    }

    showGridsInterface() {
        this.setDisplay('singleGridContainer', 'block');
        this.setDisplay('noGridsMessage', 'none');
        this.setDisplay('gridNavigation', this.grids.length > 1 ? 'flex' : 'none');
        this.updateNavigation();
    }

    updateNavigation() {
        this.updateElement('currentGridIndex', this.currentIndex + 1);
        this.updateElement('totalGridsNav', this.grids.length);

        const prevBtn = document.getElementById('prevGridBtn');
        const nextBtn = document.getElementById('nextGridBtn');

        if (prevBtn) prevBtn.disabled = this.currentIndex === 0;
        if (nextBtn) nextBtn.disabled = this.currentIndex === this.grids.length - 1;
    }

    setDisplay(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.style.display = value;
        }
    }
}