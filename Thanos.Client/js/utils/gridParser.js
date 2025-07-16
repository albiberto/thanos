/**
 * GridParser.js - Parsing e sanitizzazione delle griglie
 */

import { EMOJI_MAP, CELL_TYPES, UI_CONFIG } from '../config/constants.js';
import { Grid } from '../models/Grid.js';

export class GridParser {
    constructor() {
        this.validChars = new Set(Object.values(CELL_TYPES));
    }

    /**
     * Sanitizza il contenuto grezzo
     */
    sanitize(content) {
        if (!content || typeof content !== 'string') {
            return '';
        }

        let result = '';

        // Processa ogni carattere
        for (const char of content) {
            // Controlla mapping emoji
            if (EMOJI_MAP[char]) {
                result += EMOJI_MAP[char];
            }
            // Mantieni caratteri validi
            else if (this.validChars.has(char) || /\s/.test(char)) {
                result += char;
            }
            // Ignora caratteri non validi
        }

        // Rimuovi righe con solo numeri (coordinate)
        const lines = result.split('\n');
        return lines
        .filter(line => !/^[\d\s]+$/.test(line.trim()))
        .map(line => line.replace(/^\s*\d+\s+/, '')) // Rimuovi numeri iniziali
        .join('\n');
    }

    /**
     * Estrae griglie multiple dal contenuto
     */
    extractGrids(content) {
        const sanitized = this.sanitize(content);

        // Separa griglie usando righe vuote
        return sanitized
        .split(/\n\s*\n/)
        .filter(grid => grid.trim())
        .map(grid => grid.trim());
    }

    /**
     * Parsa una singola griglia
     */
    parseGrid(gridText, index = 1) {
        if (!gridText || !gridText.trim()) {
            throw new Error(`Griglia ${index} vuota`);
        }

        const lines = gridText.split('\n').filter(line => line.trim());
        if (lines.length === 0) {
            throw new Error(`Griglia ${index} senza righe valide`);
        }

        // Converte in matrice
        const rows = lines.map(line =>
            Array.from(line.replace(/\s+/g, '')) // Rimuove spazi e crea array di caratteri
        );

        // Valida dimensioni
        const width = rows[0].length;
        const height = rows.length;

        if (!rows.every(row => row.length === width)) {
            throw new Error(`Griglia ${index}: righe con lunghezze diverse`);
        }

        if (width < UI_CONFIG.GRID_MIN_SIZE || height < UI_CONFIG.GRID_MIN_SIZE) {
            throw new Error(`Griglia ${index}: dimensioni minime ${UI_CONFIG.GRID_MIN_SIZE}x${UI_CONFIG.GRID_MIN_SIZE}`);
        }

        // Crea oggetto Grid
        return new Grid({
            width,
            height,
            cells: rows,
            index
        });
    }

    /**
     * Parsa contenuto completo in array di Grid
     */
    parse(content) {
        const gridTexts = this.extractGrids(content);
        const grids = [];
        const errors = [];

        for (let i = 0; i < gridTexts.length; i++) {
            try {
                const grid = this.parseGrid(gridTexts[i], i + 1);
                grids.push(grid);
            } catch (error) {
                errors.push(error.message);
            }
        }

        return { grids, errors };
    }
}