/**
 * Grid.js - Modello per rappresentare una griglia di gioco
 */

import { CELL_TYPES, GRID_STATUS } from '../config/constants.js';

export class Grid {
    constructor(data = {}) {
        this.id = data.id || `grid_${Date.now()}`;
        this.width = data.width || 0;
        this.height = data.height || 0;
        this.cells = data.cells || [];
        this.status = data.status || GRID_STATUS.IMPORTED;
        this.expectedValue = data.expectedValue || null;
        this.timestamp = data.timestamp || new Date().toISOString();
        this.testId = data.testId || null;

        // Analisi della griglia
        this.analysis = data.analysis || this.analyze();
    }

    /**
     * Analizza il contenuto della griglia
     */
    analyze() {
        const analysis = {
            myHead: null,
            myTail: null,
            myBody: [],
            enemyHeads: [],
            enemyBodies: [],
            food: [],
            hazards: [],
            empty: [],
            totalCells: this.width * this.height
        };

        for (let y = 0; y < this.height; y++) {
            for (let x = 0; x < this.width; x++) {
                const cell = this.cells[y]?.[x];
                const position = { x, y };

                switch (cell) {
                    case CELL_TYPES.MY_HEAD:
                        analysis.myHead = position;
                        break;
                    case CELL_TYPES.MY_BODY:
                        analysis.myBody.push(position);
                        break;
                    case CELL_TYPES.MY_TAIL:
                        analysis.myTail = position;
                        break;
                    case CELL_TYPES.ENEMY_HEAD:
                        analysis.enemyHeads.push(position);
                        break;
                    case CELL_TYPES.ENEMY_BODY:
                        analysis.enemyBodies.push(position);
                        break;
                    case CELL_TYPES.FOOD:
                        analysis.food.push(position);
                        break;
                    case CELL_TYPES.HAZARD:
                        analysis.hazards.push(position);
                        break;
                    case CELL_TYPES.EMPTY:
                        analysis.empty.push(position);
                        break;
                }
            }
        }

        // Calcola statistiche
        analysis.mySnakeLength = (analysis.myHead ? 1 : 0) + analysis.myBody.length;
        analysis.totalEnemies = analysis.enemyHeads.length;
        analysis.emptyPercentage = Math.round((analysis.empty.length / analysis.totalCells) * 100);

        return analysis;
    }

    /**
     * Calcola le mosse sicure per il serpente
     */
    calculateSafeMoves() {
        if (!this.analysis.myHead) {
            return [];
        }

        const head = this.analysis.myHead;
        const safeMoves = [];

        // Controlla ogni direzione
        const directions = [
            { name: 'UP', value: 1, dx: 0, dy: -1 },
            { name: 'DOWN', value: 2, dx: 0, dy: 1 },
            { name: 'LEFT', value: 4, dx: -1, dy: 0 },
            { name: 'RIGHT', value: 8, dx: 1, dy: 0 }
        ];

        for (const dir of directions) {
            const nx = head.x + dir.dx;
            const ny = head.y + dir.dy;

            // Controlla i limiti
            if (nx < 0 || nx >= this.width || ny < 0 || ny >= this.height) {
                continue;
            }

            const cell = this.cells[ny][nx];

            // Celle non sicure
            const unsafeCells = [CELL_TYPES.MY_BODY, CELL_TYPES.ENEMY_HEAD,
                CELL_TYPES.ENEMY_BODY, CELL_TYPES.HAZARD];

            if (!unsafeCells.includes(cell)) {
                safeMoves.push(dir);
            }
        }

        return safeMoves;
    }

    /**
     * Imposta il valore expected
     */
    setExpectedValue(value) {
        this.expectedValue = value;
        this.status = GRID_STATUS.READY;
    }

    /**
     * Valida la griglia
     */
    validate() {
        const errors = [];

        if (this.width < 3 || this.height < 3) {
            errors.push('Dimensioni minime 3x3');
        }

        if (this.cells.length !== this.height) {
            errors.push('Altezza non corrisponde alle righe');
        }

        for (let i = 0; i < this.cells.length; i++) {
            if (this.cells[i].length !== this.width) {
                errors.push(`Riga ${i} ha lunghezza errata`);
            }
        }

        return {
            isValid: errors.length === 0,
            errors
        };
    }

    /**
     * Clona la griglia
     */
    clone() {
        return new Grid({
            ...this,
            id: `grid_${Date.now()}_cloned`,
            cells: this.cells.map(row => [...row]),
            analysis: { ...this.analysis },
            timestamp: new Date().toISOString()
        });
    }

    /**
     * Converte in JSON
     */
    toJSON() {
        return {
            id: this.id,
            width: this.width,
            height: this.height,
            cells: this.cells,
            status: this.status,
            expectedValue: this.expectedValue,
            timestamp: this.timestamp,
            testId: this.testId,
            analysis: this.analysis
        };
    }
}