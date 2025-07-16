/**
 * ImportManager.js - Gestisce l'importazione delle griglie
 */

import {GridParser} from '../utils/GridParser.js';
import {StorageService} from '../services/StorageService.js';

export class ImportManager {
    constructor(inputElementId, notificationService, tabManager) {
        this.inputElementId = inputElementId;
        this.notify = notificationService;
        this.tabManager = tabManager;
        this.parser = new GridParser();
        this.storage = new StorageService();

        this.initialize();
    }

    /**
     * Importa le griglie
     */
    importBoards() {
        const input = this.getInputElement();
        if (!input || !input.value.trim()) {
            this.notify.error('Inserisci del contenuto da importare');
            return;
        }

        try {
            // Parsa il contenuto
            const {grids, errors} = this.parser.parse(input.value);

            if (errors.length > 0) {
                errors.forEach(error => this.notify.warning(error));
            }

            if (grids.length === 0) {
                this.notify.error('Nessuna griglia valida trovata');
                return;
            }

            // Carica griglie esistenti e aggiungi le nuove
            const existingGrids = this.storage.loadGrids();
            const allGrids = [...existingGrids, ...grids];

            // Salva
            this.storage.saveGrids(allGrids);

            this.notify.success(`${grids.length} griglie importate con successo`);

            // Naviga al tab process
            setTimeout(() => {
                this.tabManager.switchTab('process');
            }, 1000);

        } catch (error) {
            console.error('Import error:', error);
            this.notify.error('Errore durante l\'importazione');
        }
    }

    /**
     * Pulisce l'input
     */
    clearInput() {
        const input = this.getInputElement();
        if (input) {
            input.value = '';
            this.updateStats();
            input.focus();
            this.notify.success('Campo pulito');
        }
    }

    /**
     * Carica esempio
     */
    loadExample() {
        const input = this.getInputElement();
        if (!input) return;

        input.value = [
            '👽 💲 💲 ⬛ ⬛',
            '⬛ ⬛ ⬛ ⬛ 🍎',
            '⬛ ⬛ 😈 ⛔ ⬛',
            '💀 ⬛ ⬛ ⛔ ⛔',
            '⬛ ⬛ ⬛ ⬛ ⬛',
            '',
            '⬛ ⬛ 😈 ⛔ ⛔',
            '⬛ ⬛ ⬛ ⬛ ⛔',
            '👽 💲 💲 🍎 ⬛',
            '⬛ ⬛ ⬛ ⬛ ⬛',
            '💀 ⬛ ⬛ ⬛ ⬛'
        ].join('\n');
        this.updateStats();
        this.notify.success('Esempio caricato');
    }

    /**
     * Copia icona negli appunti
     */
    async copyIcon(icon) {
        try {
            await navigator.clipboard.writeText(icon);
            this.notify.success(`${icon} copiato`);
        } catch (error) {
            // Fallback
            const temp = document.createElement('input');
            temp.value = icon;
            document.body.appendChild(temp);
            temp.select();
            document.execCommand('copy');
            document.body.removeChild(temp);
            this.notify.success(`${icon} copiato`);
        }
    }

    /**
     * Aggiorna statistiche
     */
    updateStats() {
        const input = this.getInputElement();
        if (!input) return;

        const content = input.value;
        const lines = content ? content.split('\n').length : 0;
        const chars = content.length;
        const grids = content.trim() ?
            content.split(/\n\s*\n/).filter(g => g.trim()).length : 0;

        this.updateElement('lineCount', lines);
        this.updateElement('charCount', chars);
        this.updateElement('gridCount', grids);
    }

    /**
     * Mostra info storage
     */
    showStorageInfo() {
        const info = this.storage.getStorageInfo();
        if (info.count > 0) {
            this.notify.info(`${info.count} griglie già salvate`);
        }
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
     * Inizializza il manager
     */
    initialize() {
        this.setupEventListeners();
        this.updateStats();
        this.showStorageInfo();
    }

    /**
     * Setup degli event listener
     */
    setupEventListeners() {
        const input = this.getInputElement();
        if (input) {
            input.addEventListener('input', () => this.updateStats());
        }
    }

    /**
     * Ottiene l'elemento input
     */
    getInputElement() {
        return document.getElementById(this.inputElementId);
    }
}