/**
 * GridMatrix.js - Componente per visualizzare la griglia di gioco
 */

import { CELL_TYPES, DIRECTIONS } from '../config/constants.js';

export class GridMatrixComponent {
    constructor(options = {}) {
        this.options = {
            cellSize: options.cellSize || 40,
            fontSize: options.fontSize || 25,
            margin: options.margin || 2,
            borderRadius: options.borderRadius || 3,
            ...options
        };

        // Mapping caratteri display
        this.displayMap = {
            [CELL_TYPES.MY_HEAD]: 'H',
            [CELL_TYPES.MY_BODY]: 'B',
            [CELL_TYPES.MY_TAIL]: 'T',
            [CELL_TYPES.ENEMY_HEAD]: 'E',
            [CELL_TYPES.ENEMY_BODY]: 'e',
            [CELL_TYPES.FOOD]: 'F',
            [CELL_TYPES.HAZARD]: 'X',
            [CELL_TYPES.EMPTY]: '·'
        };

        // CSS classes per celle
        this.cellClasses = {
            [CELL_TYPES.MY_HEAD]: 'my-head',
            [CELL_TYPES.MY_BODY]: 'my-body',
            [CELL_TYPES.MY_TAIL]: 'my-tail',
            [CELL_TYPES.ENEMY_HEAD]: 'enemy-head',
            [CELL_TYPES.ENEMY_BODY]: 'enemy-body',
            [CELL_TYPES.FOOD]: 'food',
            [CELL_TYPES.HAZARD]: 'hazard',
            [CELL_TYPES.EMPTY]: 'empty'
        };
    }

    /**
     * Renderizza la griglia
     */
    render(grid, options = {}) {
        if (!grid || !grid.cells) {
            return this.renderEmpty();
        }

        const instanceId = `grid-${Date.now()}`;
        const css = this.generateCSS(instanceId, options);
        const cells = this.renderCells(grid, options);

        return `
            <style>${css}</style>
            <div class="grid-matrix" id="${instanceId}">
                ${cells}
            </div>
        `;
    }

    /**
     * Renderizza le celle
     */
    renderCells(grid, options) {
        const { expectedValue, myHead } = options;

        return grid.cells.map((row, y) => {
            const cells = row.map((cell, x) => {
                const cellClass = this.cellClasses[cell] || 'empty';
                const display = this.displayMap[cell] || cell;
                const overlay = this.getDirectionOverlay(x, y, expectedValue, myHead);

                return `<div class="matrix-cell ${cellClass} ${overlay.class}">${display}${overlay.content}</div>`;
            }).join('');

            return `<div class="matrix-row">${cells}</div>`;
        }).join('');
    }

    /**
     * Ottieni overlay direzione
     */
    getDirectionOverlay(x, y, expectedValue, myHead) {
        if (!expectedValue || !myHead) {
            return { class: '', content: '' };
        }

        // Controlla se cella è adiacente alla testa
        const dx = Math.abs(x - myHead.x);
        const dy = Math.abs(y - myHead.y);

        if ((dx === 1 && dy === 0) || (dx === 0 && dy === 1)) {
            // Determina direzione
            let direction = null;
            if (y < myHead.y) direction = DIRECTIONS.UP;
            else if (y > myHead.y) direction = DIRECTIONS.DOWN;
            else if (x < myHead.x) direction = DIRECTIONS.LEFT;
            else if (x > myHead.x) direction = DIRECTIONS.RIGHT;

            // Controlla se direzione è nel valore expected
            if (direction && (expectedValue & direction.value)) {
                return {
                    class: 'direction-overlay',
                    content: `<span class="direction-arrow">${direction.symbol}</span>`
                };
            }
        }

        return { class: '', content: '' };
    }

    /**
     * Genera CSS per l'istanza
     */
    generateCSS(instanceId, options) {
        const { cellSize, fontSize, margin, borderRadius } = this.options;

        return `
            #${instanceId} {
                display: flex;
                flex-direction: column;
                gap: ${margin}px;
            }

            #${instanceId} .matrix-row {
                display: flex;
                gap: ${margin}px;
                justify-content: center;
            }

            #${instanceId} .matrix-cell {
                width: ${cellSize}px;
                height: ${cellSize}px;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: ${fontSize}px;
                font-weight: bold;
                border-radius: ${borderRadius}px;
                position: relative;
            }

            /* Cell types */
            #${instanceId} .my-head { background: #22c55e; color: white; }
            #${instanceId} .my-body { background: #16a34a; color: white; }
            #${instanceId} .my-tail { background: #0dcaf0; color: white; }
            #${instanceId} .enemy-head { background: #ef4444; color: white; }
            #${instanceId} .enemy-body { background: #dc2626; color: white; }
            #${instanceId} .food { background: #f59e0b; color: white; }
            #${instanceId} .hazard { background: #8b5cf6; color: white; }
            #${instanceId} .empty { background: #f3f4f6; color: #9ca3af; }

            /* Direction overlay */
            #${instanceId} .direction-overlay {
                border: 3px solid #10b981 !important;
                background: #86efac !important;
                color: #166534 !important;
            }

            #${instanceId} .direction-arrow {
                position: absolute;
                font-size: ${fontSize * 1.2}px;
                text-shadow: 1px 1px 2px rgba(0,0,0,0.3);
            }

            /* Responsive */
            @media (max-width: 768px) {
                #${instanceId} .matrix-cell {
                    width: ${cellSize * 0.7}px;
                    height: ${cellSize * 0.7}px;
                    font-size: ${fontSize * 0.7}px;
                }
            }
        `;
    }

    /**
     * Renderizza griglia vuota
     */
    renderEmpty() {
        return `
            <div class="text-center text-muted p-5">
                <div style="font-size: 3rem; margin-bottom: 1rem;">📝</div>
                <p>Nessuna griglia da visualizzare</p>
            </div>
        `;
    }

    /**
     * Calcola dimensioni ottimali
     */
    calculateOptimalSize(containerWidth, containerHeight, gridWidth, gridHeight) {
        const padding = 20;
        const availableWidth = containerWidth - padding;
        const availableHeight = containerHeight - padding;

        const cellWidth = Math.floor(availableWidth / gridWidth);
        const cellHeight = Math.floor(availableHeight / gridHeight);

        const cellSize = Math.min(cellWidth, cellHeight, 50);
        const fontSize = Math.floor(cellSize * 0.6);

        return {
            cellSize: Math.max(cellSize, 20),
            fontSize: Math.max(fontSize, 12),
            margin: 2,
            borderRadius: 3
        };
    }
}