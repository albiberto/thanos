/**
 * FormatterManager.js - Gestisce la formattazione JSON
 */

import { JsonFormatter } from '../utils/JsonFormatter.js';

export class FormatterManager {
    constructor(inputElementId, notificationService) {
        this.inputElementId = inputElementId;
        this.notify = notificationService;
        this.formatter = new JsonFormatter();
    }

    /**
     * Inizializza il manager
     */
    initialize() {
        this.setupEventListeners();
        this.updateStats();
    }

    /**
     * Setup event listeners
     */
    setupEventListeners() {
        const input = this.getInputElement();
        if (input) {
            input.addEventListener('input', () => this.updateStats());
        }
    }

    /**
     * Ottiene elemento input
     */
    getInputElement() {
        return document.getElementById(this.inputElementId);
    }

    /**
     * Formatta JSON
     */
    formatJSON() {
        const input = this.getInputElement();
        if (!input || !input.value.trim()) {
            this.notify.error('Inserisci del JSON');
            return;
        }

        try {
            const jsonList = JSON.parse(input.value);

            if (!Array.isArray(jsonList)) {
                throw new Error('Input deve essere un array');
            }

            // Usa il formatter per pulire la struttura
            const formatted = jsonList.map(item => {
                if (item.MoveRequest?.board?.snakes) {
                    item.MoveRequest.board.snakes.forEach(snake => {
                        // Assicura che body sia array
                        if (typeof snake.body === 'string') {
                            try {
                                snake.body = JSON.parse(snake.body);
                            } catch (e) {
                                snake.body = [];
                            }
                        }
                    });
                }
                return item;
            });

            const formattedJSON = JSON.stringify(formatted, null, 2);
            this.showOutput(formattedJSON);

            this.notify.success(`${formatted.length} elementi formattati`);

        } catch (error) {
            this.notify.error(`Errore: ${error.message}`);
        }
    }

    /**
     * Valida JSON
     */
    validateJSON() {
        const input = this.getInputElement();
        if (!input || !input.value.trim()) {
            this.notify.warning('Nessun contenuto');
            return;
        }

        try {
            const parsed = JSON.parse(input.value);
            const isArray = Array.isArray(parsed);
            const count = isArray ? parsed.length : 1;

            this.updateValidationStatus(
                `JSON valido - ${count} elemento${count !== 1 ? 'i' : ''}`,
                'success'
            );

            this.notify.success('JSON valido');

        } catch (error) {
            this.updateValidationStatus(
                `JSON non valido: ${error.message}`,
                'error'
            );
            this.notify.error('JSON non valido');
        }
    }

    /**
     * Prettify JSON
     */
    prettifyJSON() {
        const input = this.getInputElement();
        if (!input || !input.value.trim()) {
            this.notify.error('Nessun contenuto');
            return;
        }

        try {
            const parsed = JSON.parse(input.value);
            input.value = JSON.stringify(parsed, null, 2);
            this.updateStats();
            this.notify.success('JSON formattato');
        } catch (error) {
            this.notify.error('JSON non valido');
        }
    }

    /**
     * Minify JSON
     */
    minifyJSON() {
        const input = this.getInputElement();
        if (!input || !input.value.trim()) {
            this.notify.error('Nessun contenuto');
            return;
        }

        try {
            const parsed = JSON.parse(input.value);
            input.value = JSON.stringify(parsed);
            this.updateStats();
            this.notify.success('JSON compresso');
        } catch (error) {
            this.notify.error('JSON non valido');
        }
    }

    /**
     * Pulisce input
     */
    clearInput() {
        const input = this.getInputElement();
        if (input) {
            input.value = '';
            this.updateStats();
            this.hideOutput();
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

        const example = [
            {
                "Id": 101,
                "Name": "Test-101",
                "Expected": 1,
                "MoveRequest": {
                    "game": {
                        "id": "game-001",
                        "ruleset": {
                            "name": "standard",
                            "version": "v1.0.0"
                        },
                        "timeout": 500
                    },
                    "turn": 5,
                    "board": {
                        "height": 11,
                        "width": 11,
                        "food": [{"x": 5, "y": 5}],
                        "hazards": [],
                        "snakes": [{
                            "id": "thanos",
                            "name": "Thanos",
                            "health": 100,
                            "body": [{"x": 3, "y": 3}, {"x": 3, "y": 4}],
                            "head": {"x": 3, "y": 3},
                            "length": 2,
                            "latency": 50
                        }]
                    },
                    "you": {
                        "id": "thanos",
                        "name": "Thanos",
                        "health": 100,
                        "body": [{"x": 3, "y": 3}, {"x": 3, "y": 4}],
                        "head": {"x": 3, "y": 3},
                        "length": 2,
                        "latency": 50
                    }
                }
            }
        ];

        input.value = JSON.stringify(example, null, 2);
        this.updateStats();
        this.notify.success('Esempio caricato');
    }

    /**
     * Copia output
     */
    async copyToClipboard() {
        const code = document.getElementById('formatterCode');
        if (!code || !code.textContent) {
            this.notify.error('Nessun contenuto');
            return;
        }

        try {
            await navigator.clipboard.writeText(code.textContent);
            this.notify.success('Copiato');
        } catch (error) {
            // Fallback
            const temp = document.createElement('textarea');
            temp.value = code.textContent;
            document.body.appendChild(temp);
            temp.select();
            document.execCommand('copy');
            document.body.removeChild(temp);
            this.notify.success('Copiato');
        }
    }

    /**
     * Esporta JSON
     */
    exportJSON() {
        const code = document.getElementById('formatterCode');
        if (!code || !code.textContent) {
            this.notify.error('Nessun contenuto');
            return;
        }

        const blob = new Blob([code.textContent], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');

        link.href = url;
        link.download = `battlesnake-formatted-${Date.now()}.json`;
        link.click();

        URL.revokeObjectURL(url);
        this.notify.success('File esportato');
    }

    /**
     * Mostra output
     */
    showOutput(content) {
        const output = document.getElementById('formatterOutput');
        const placeholder = document.getElementById('formatterPlaceholder');
        const code = document.getElementById('formatterCode');

        if (output && placeholder && code) {
            code.textContent = content;
            output.style.display = 'block';
            placeholder.style.display = 'none';
        }
    }

    /**
     * Nascondi output
     */
    hideOutput() {
        const output = document.getElementById('formatterOutput');
        const placeholder = document.getElementById('formatterPlaceholder');

        if (output && placeholder) {
            output.style.display = 'none';
            placeholder.style.display = 'block';
        }
    }

    /**
     * Aggiorna statistiche
     */
    updateStats() {
        const input = this.getInputElement();
        if (!input) return;

        const value = input.value || '';
        const lines = value.split('\n').length;
        const chars = value.length;
        let items = 0;

        try {
            if (value.trim()) {
                const parsed = JSON.parse(value);
                items = Array.isArray(parsed) ? parsed.length : 1;
            }
        } catch (e) {
            // Invalid JSON
        }

        this.updateElement('jsonLineCount', lines);
        this.updateElement('jsonCharCount', chars);
        this.updateElement('jsonItemCount', items);
        this.updateElement('jsonStatus', value.trim() ? 'Content present' : 'Empty');
    }

    /**
     * Aggiorna stato validazione
     */
    updateValidationStatus(message, type) {
        const validation = document.getElementById('jsonValidation');
        if (validation) {
            validation.textContent = message;
            validation.className = `validation-status validation-${type}`;
            validation.style.display = 'block';
        }
    }

    /**
     * Helper aggiornamento elementi
     */
    updateElement(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }
}