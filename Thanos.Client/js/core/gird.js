/**
 * GridMatrixComponent - Standalone Grid Visualization Component
 * Handles matrix rendering with consistent sizing and styling
 */
class GridMatrixComponent {
    constructor(options = {}) {
        this.options = {
            cellSize: options.cellSize || 40,
            fontSize: options.fontSize || 25,
            margin: options.margin || 2,
            borderRadius: options.borderRadius || 3,
            showBorder: options.showBorder !== false,
            className: options.className || 'grid-matrix-component',
            ...options
        };

        // Character to display mapping
        this.charDisplayMap = {
            'H': 'H',    // My Head
            'B': 'B',    // My Body  
            'E': 'E',    // Enemy Head
            'b': 'e',    // Enemy Body (lowercase display)
            'F': 'F',    // Food
            '#': 'X',    // Hazard
            '.': '·',    // Empty
            '^': '↑',    // Up direction
            'v': '↓',    // Down direction
            '<': '←',    // Left direction
            '>': '→',    // Right direction
        };

        // CSS class mapping for cell types
        this.cellClassMap = {
            'H': 'my-head',
            'B': 'my-body',
            'E': 'enemy-head',
            'b': 'enemy-body',
            'F': 'food',
            '#': 'hazard',
            '.': 'empty',
            '^': 'direction',
            'v': 'direction',
            '<': 'direction',
            '>': 'direction'
        };
    }

    /**
     * Render grid matrix from grid data
     * @param {Object} grid - Grid object with cells array
     * @param {Object} options - Rendering options
     * @returns {string} - HTML string for the matrix
     */
    render(grid, options = {}) {
        if (!grid || !grid.cells || !Array.isArray(grid.cells)) {
            return this.renderEmptyMatrix();
        }

        const mergedOptions = { ...this.options, ...options };
        const { cellSize, fontSize, margin, borderRadius, showBorder } = mergedOptions;

        // Calculate container dimensions
        const containerWidth = grid.width * (cellSize + margin * 2);
        const containerHeight = grid.height * (cellSize + margin * 2);

        // Generate CSS for this specific instance
        const instanceId = `grid-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
        const css = this.generateCSS(instanceId, mergedOptions);

        // Generate matrix HTML
        const matrixHTML = this.renderMatrixCells(grid.cells, instanceId);

        return `
            <style>${css}</style>
            <div class="${this.options.className}" 
                 id="${instanceId}"
                 style="width: ${containerWidth}px; height: ${containerHeight}px; overflow: auto;">
                ${matrixHTML}
            </div>
        `;
    }

    /**
     * Render matrix cells
     * @param {Array} cells - 2D array of cell characters
     * @param {string} instanceId - Unique instance identifier
     * @returns {string} - HTML for matrix cells
     */
    renderMatrixCells(cells, instanceId) {
        const rows = cells.map((row, rowIndex) => {
            const cellsHTML = row.map((cell, colIndex) => {
                const cellClass = this.getCellClass(cell);
                const cellContent = this.getCellDisplay(cell);

                return `<span class="matrix-cell ${cellClass}" 
                             data-row="${rowIndex}" 
                             data-col="${colIndex}"
                             data-value="${cell}">${cellContent}</span>`;
            }).join('');

            return `<div class="matrix-row" data-row="${rowIndex}">${cellsHTML}</div>`;
        }).join('');

        return rows;
    }

    /**
     * Get CSS class for cell type
     * @param {string} cell - Cell character
     * @returns {string} - CSS class name
     */
    getCellClass(cell) {
        return this.cellClassMap[cell] || 'empty';
    }

    /**
     * Get display character for cell
     * @param {string} cell - Cell character
     * @returns {string} - Display character
     */
    getCellDisplay(cell) {
        return this.charDisplayMap[cell] || cell || '·';
    }

    /**
     * Generate CSS for this component instance
     * @param {string} instanceId - Unique instance identifier
     * @param {Object} options - Styling options
     * @returns {string} - CSS string
     */
    generateCSS(instanceId, options) {
        const { cellSize, fontSize, margin, borderRadius } = options;

        return `
            #${instanceId} {
                font-family: 'Courier New', monospace;
                line-height: 1;
                text-align: center;
                display: flex;
                flex-direction: column;
                justify-content: flex-start;
                background: var(--color-white, #ffffff);
                border: 1px solid var(--color-border-default, #dee2e6);
                border-radius: 8px;
                padding: 8px;
            }

            #${instanceId} .matrix-row {
                display: flex;
                justify-content: center;
                align-items: center;
                height: auto;
            }

            #${instanceId} .matrix-cell {
                display: inline-block;
                width: ${cellSize}px;
                height: ${cellSize}px;
                margin: ${margin}px;
                text-align: center;
                font-size: ${fontSize}px;
                line-height: ${cellSize}px;
                font-weight: bold;
                border-radius: ${borderRadius}px;
                font-family: monospace;
                cursor: default;
                user-select: none;
            }

            /* Cell type styles */
            #${instanceId} .matrix-cell.my-head {
                background: var(--color-my-head, #22c55e);
                color: var(--color-white, #ffffff);
            }

            #${instanceId} .matrix-cell.my-body {
                background: var(--color-my-body, #16a34a);
                color: var(--color-white, #ffffff);
            }

            #${instanceId} .matrix-cell.enemy-head {
                background: var(--color-enemy-head, #ef4444);
                color: var(--color-white, #ffffff);
            }

            #${instanceId} .matrix-cell.enemy-body {
                background: var(--color-enemy-body, #dc2626);
                color: var(--color-white, #ffffff);
            }

            #${instanceId} .matrix-cell.food {
                background: var(--color-food, #f59e0b);
                color: var(--color-white, #ffffff);
            }

            #${instanceId} .matrix-cell.hazard {
                background: var(--color-hazard, #8b5cf6);
                color: var(--color-white, #ffffff);
            }

            #${instanceId} .matrix-cell.empty {
                background: var(--color-empty, #f3f4f6);
                color: var(--color-empty-text, #9ca3af);
            }

            #${instanceId} .matrix-cell.direction {
                background: var(--color-info-light, #3b82f6);
                color: var(--color-white, #ffffff);
            }

            /* Responsive sizing */
            @media (max-width: 768px) {
                #${instanceId} .matrix-cell {
                    width: ${Math.max(20, cellSize * 0.6)}px;
                    height: ${Math.max(20, cellSize * 0.6)}px;
                    font-size: ${Math.max(12, fontSize * 0.6)}px;
                    line-height: ${Math.max(20, cellSize * 0.6)}px;
                    margin: ${Math.max(1, margin * 0.5)}px;
                }
            }

            @media (max-width: 576px) {
                #${instanceId} .matrix-cell {
                    width: ${Math.max(16, cellSize * 0.4)}px;
                    height: ${Math.max(16, cellSize * 0.4)}px;
                    font-size: ${Math.max(10, fontSize * 0.4)}px;
                    line-height: ${Math.max(16, cellSize * 0.4)}px;
                    margin: ${Math.max(0.5, margin * 0.25)}px;
                }
            }
        `;
    }

    /**
     * Render empty matrix placeholder
     * @returns {string} - HTML for empty matrix
     */
    renderEmptyMatrix() {
        return `
            <div class="${this.options.className} empty-matrix">
                <div class="text-muted p-3 text-center">
                    <div style="font-size: 2rem; margin-bottom: 8px;">📝</div>
                    <div>No grid data available</div>
                </div>
            </div>
        `;
    }

    /**
     * Update an existing rendered matrix
     * @param {string} containerId - Container element ID
     * @param {Object} grid - New grid data
     * @param {Object} options - Rendering options
     */
    update(containerId, grid, options = {}) {
        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = this.render(grid, options);
        }
    }

    /**
     * Get cell at specific coordinates
     * @param {Object} grid - Grid data
     * @param {number} row - Row index
     * @param {number} col - Column index
     * @returns {string|null} - Cell value or null if out of bounds
     */
    getCellAt(grid, row, col) {
        if (!grid?.cells || !Array.isArray(grid.cells)) return null;
        if (row < 0 || row >= grid.cells.length) return null;
        if (col < 0 || col >= grid.cells[row].length) return null;
        return grid.cells[row][col];
    }

    /**
     * Set cell at specific coordinates
     * @param {Object} grid - Grid data (will be modified)
     * @param {number} row - Row index
     * @param {number} col - Column index
     * @param {string} value - New cell value
     * @returns {boolean} - True if successful
     */
    setCellAt(grid, row, col, value) {
        if (!grid?.cells || !Array.isArray(grid.cells)) return false;
        if (row < 0 || row >= grid.cells.length) return false;
        if (col < 0 || col >= grid.cells[row].length) return false;

        grid.cells[row][col] = value;
        return true;
    }

    /**
     * Calculate optimal cell size based on container dimensions
     * @param {number} containerWidth - Container width in pixels
     * @param {number} containerHeight - Container height in pixels
     * @param {number} gridWidth - Grid width in cells
     * @param {number} gridHeight - Grid height in cells
     * @returns {Object} - Optimal sizing options
     */
    calculateOptimalSize(containerWidth, containerHeight, gridWidth, gridHeight) {
        const padding = 16; // Account for container padding
        const margin = 2;   // Margin between cells

        const availableWidth = containerWidth - padding;
        const availableHeight = containerHeight - padding;

        const maxCellWidth = Math.floor((availableWidth - (gridWidth - 1) * margin * 2) / gridWidth);
        const maxCellHeight = Math.floor((availableHeight - (gridHeight - 1) * margin * 2) / gridHeight);

        const cellSize = Math.min(maxCellWidth, maxCellHeight, 50); // Max 50px per cell
        const fontSize = Math.max(Math.floor(cellSize * 0.6), 10);

        return {
            cellSize: Math.max(cellSize, 16), // Minimum 16px
            fontSize: Math.max(fontSize, 8),  // Minimum 8px
            margin: margin,
            borderRadius: 3
        };
    }

    /**
     * Generate stats for the grid
     * @param {Object} grid - Grid data
     * @returns {Object} - Grid statistics
     */
    generateStats(grid) {
        if (!grid?.cells) {
            return {
                width: 0,
                height: 0,
                totalCells: 0,
                cellCounts: {},
                isEmpty: true
            };
        }

        const stats = {
            width: grid.width || grid.cells[0]?.length || 0,
            height: grid.height || grid.cells.length || 0,
            totalCells: 0,
            cellCounts: {},
            isEmpty: false
        };

        stats.totalCells = stats.width * stats.height;

        // Count cell types
        grid.cells.forEach(row => {
            row.forEach(cell => {
                stats.cellCounts[cell] = (stats.cellCounts[cell] || 0) + 1;
            });
        });

        return stats;
    }

    /**
     * Validate grid data structure
     * @param {Object} grid - Grid data to validate
     * @returns {Object} - Validation result
     */
    validate(grid) {
        const result = {
            isValid: true,
            errors: [],
            warnings: []
        };

        if (!grid) {
            result.isValid = false;
            result.errors.push('Grid is null or undefined');
            return result;
        }

        if (!grid.cells || !Array.isArray(grid.cells)) {
            result.isValid = false;
            result.errors.push('Grid cells must be an array');
            return result;
        }

        if (grid.cells.length === 0) {
            result.isValid = false;
            result.errors.push('Grid cells array is empty');
            return result;
        }

        // Check row consistency
        const firstRowLength = grid.cells[0]?.length || 0;
        if (firstRowLength === 0) {
            result.isValid = false;
            result.errors.push('First row is empty');
            return result;
        }

        for (let i = 0; i < grid.cells.length; i++) {
            if (!Array.isArray(grid.cells[i])) {
                result.isValid = false;
                result.errors.push(`Row ${i} is not an array`);
                continue;
            }

            if (grid.cells[i].length !== firstRowLength) {
                result.warnings.push(`Row ${i} has ${grid.cells[i].length} cells, expected ${firstRowLength}`);
            }
        }

        // Validate dimensions
        if (grid.width && grid.width !== firstRowLength) {
            result.warnings.push(`Grid width property (${grid.width}) doesn't match actual width (${firstRowLength})`);
        }

        if (grid.height && grid.height !== grid.cells.length) {
            result.warnings.push(`Grid height property (${grid.height}) doesn't match actual height (${grid.cells.length})`);
        }

        return result;
    }
}

// Export for global access
if (typeof window !== 'undefined') {
    window.GridMatrixComponent = GridMatrixComponent;
}

if (typeof module !== 'undefined' && module.exports) {
    module.exports = GridMatrixComponent;
}