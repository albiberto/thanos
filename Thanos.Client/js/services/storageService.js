/**
 * StorageService.js - Gestisce la persistenza dei dati
 */

import { STORAGE_KEYS } from '../config/constants.js';
import { Grid } from '../models/Grid.js';

export class StorageService {
    constructor() {
        this.storage = window.localStorage;
    }

    /**
     * Salva le griglie
     */
    saveGrids(grids) {
        try {
            const data = {
                grids: grids.map(grid => grid.toJSON()),
                timestamp: new Date().toISOString(),
                version: '1.0',
                totalGrids: grids.length
            };

            this.storage.setItem(STORAGE_KEYS.GRIDS, JSON.stringify(data));
            return true;
        } catch (error) {
            if (error.name === 'QuotaExceededError') {
                throw new Error('Spazio storage esaurito');
            }
            throw error;
        }
    }

    /**
     * Carica le griglie
     */
    loadGrids() {
        try {
            const data = this.storage.getItem(STORAGE_KEYS.GRIDS);
            if (!data) return [];

            const parsed = JSON.parse(data);
            if (!parsed.grids || !Array.isArray(parsed.grids)) return [];

            return parsed.grids.map(gridData => new Grid(gridData));
        } catch (error) {
            console.error('Error loading grids:', error);
            return [];
        }
    }

    /**
     * Elimina tutte le griglie
     */
    clearGrids() {
        this.storage.removeItem(STORAGE_KEYS.GRIDS);
    }

    /**
     * Ottieni statistiche sullo storage
     */
    getStorageInfo() {
        const data = this.storage.getItem(STORAGE_KEYS.GRIDS);
        if (!data) {
            return { count: 0, size: 0 };
        }

        const parsed = JSON.parse(data);
        return {
            count: parsed.grids?.length || 0,
            size: new Blob([data]).size,
            timestamp: parsed.timestamp
        };
    }

    /**
     * Esporta tutti i dati
     */
    exportAllData() {
        const data = {};
        for (let key in this.storage) {
            if (this.storage.hasOwnProperty(key)) {
                data[key] = this.storage[key];
            }
        }
        return data;
    }

    /**
     * Importa dati
     */
    importData(data) {
        for (let key in data) {
            if (data.hasOwnProperty(key)) {
                this.storage.setItem(key, data[key]);
            }
        }
    }
}