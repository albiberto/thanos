/**
 * Battlesnake Board Converter - Formatter Tab JavaScript
 * Handles JSON list formatting, validation, and export functionality
 * Updated: HTML moved to formatter-tab.html, uses global NotifyService
 */

class FormatterTab {
    constructor() {
        this.initialized = false;
        this.elements = {};
        this.debounceTimeout = null;
    }

    /**
     * Initialize the formatter tab
     */
    init() {
        if (this.initialized) return;

        // Check if HTML is already loaded
        const formatterTab = document.getElementById('formatter-tab');
        if (!formatterTab) {
            console.error('Formatter tab container not found');
            return;
        }

        // HTML should be loaded from formatter-tab.html, just cache elements
        this.cacheElements();
        this.setupEventListeners();
        this.updateStats();
        this.initialized = true;
        console.log('FormatterTab initialized');
    }

    /**
     * Cache DOM elements for performance
     */
    cacheElements() {
        this.elements = {
            jsonListInput: document.getElementById('jsonListInput'),
            formatBtn: document.getElementById('formatBtn'),
            validateBtn: document.getElementById('validateBtn'),
            prettifyBtn: document.getElementById('prettifyBtn'),
            minifyBtn: document.getElementById('minifyBtn'),
            clearJsonBtn: document.getElementById('clearJsonBtn'),
            exampleJsonBtn: document.getElementById('exampleJsonBtn'),
            copyFormatterBtn: document.getElementById('copyFormatterBtn'),
            exportBtn: document.getElementById('exportBtn'),
            hideOutputBtn: document.getElementById('hideOutputBtn'),
            formatterOutput: document.getElementById('formatterOutput'),
            formatterCode: document.getElementById('formatterCode'),
            formatProgress: document.getElementById('formatProgress'),
            jsonValidation: document.getElementById('jsonValidation'),
            jsonLineCount: document.getElementById('jsonLineCount'),
            jsonCharCount: document.getElementById('jsonCharCount'),
            jsonItemCount: document.getElementById('jsonItemCount'),
            jsonStatus: document.getElementById('jsonStatus')
        };
    }

    /**
     * Setup all event listeners
     */
    setupEventListeners() {
        // Format button
        if (this.elements.formatBtn) {
            this.elements.formatBtn.addEventListener('click', () => this.formatJSON());
        }

        // Validate button
        if (this.elements.validateBtn) {
            this.elements.validateBtn.addEventListener('click', () => this.validateJSON());
        }

        // Prettify button
        if (this.elements.prettifyBtn) {
            this.elements.prettifyBtn.addEventListener('click', () => this.prettifyJSON());
        }

        // Minify button
        if (this.elements.minifyBtn) {
            this.elements.minifyBtn.addEventListener('click', () => this.minifyJSON());
        }

        // Clear button
        if (this.elements.clearJsonBtn) {
            this.elements.clearJsonBtn.addEventListener('click', () => this.clearInput());
        }

        // Example button
        if (this.elements.exampleJsonBtn) {
            this.elements.exampleJsonBtn.addEventListener('click', () => this.loadExample());
        }

        // Copy button
        if (this.elements.copyFormatterBtn) {
            this.elements.copyFormatterBtn.addEventListener('click', () => this.copyToClipboard());
        }

        // Export button
        if (this.elements.exportBtn) {
            this.elements.exportBtn.addEventListener('click', () => this.exportJSON());
        }

        // Hide output button
        if (this.elements.hideOutputBtn) {
            this.elements.hideOutputBtn.addEventListener('click', () => this.hideOutput());
        }

        // Input change for live stats and validation
        if (this.elements.jsonListInput) {
            this.elements.jsonListInput.addEventListener('input', () => {
                this.updateStats();
                this.debounceValidation();
            });
        }

        // Register tab with tab manager
        if (window.BattlesnakeTabManager) {
            window.BattlesnakeTabManager.registerTab('formatter', {
                onActivate: () => this.onTabActivate(),
                onDeactivate: () => this.onTabDeactivate()
            });
        }
    }

    /**
     * Tab activation handler
     */
    onTabActivate() {
        if (this.elements.jsonListInput) {
            this.elements.jsonListInput.focus();
        }
    }

    /**
     * Tab deactivation handler
     */
    onTabDeactivate() {
        // Cleanup when leaving tab
    }

    /**
     * Format JSON with inline body
     */
    formatJSON() {
        const input = this.elements.jsonListInput?.value?.trim();

        if (!input) {
            window.NotifyService?.error('❌ Inserisci del JSON nel campo di input');
            return;
        }

        try {
            this.showProgress(true);
            window.NotifyService?.info('🔄 Formattazione in corso...');

            // Parse and validate JSON
            const jsonList = JSON.parse(input);

            if (!Array.isArray(jsonList)) {
                throw new Error('Input must be an array of JSON objects');
            }

            // Format each item with inline body
            const formattedList = jsonList.map(item => this.formatItemWithInlineBody(item));

            // Generate output
            const formattedJSON = JSON.stringify(formattedList, null, 2);

            // Show output
            this.showOutput(formattedJSON);

            window.NotifyService?.success(`✅ ${formattedList.length} elementi formattati con successo`);

        } catch (error) {
            console.error('Format error:', error);
            window.NotifyService?.error(`❌ Errore nella formattazione: ${error.message}`);
        } finally {
            this.showProgress(false);
        }
    }

    /**
     * Format item with inline body for Battlesnake tests
     */
    formatItemWithInlineBody(item) {
        if (!item.MoveRequest?.board?.snakes) {
            return item;
        }

        const formatted = JSON.parse(JSON.stringify(item)); // Deep clone

        // Format snakes with inline body
        formatted.MoveRequest.board.snakes = formatted.MoveRequest.board.snakes.map(snake => ({
            ...snake,
            body: snake.body ? JSON.stringify(snake.body) : "[]"
        }));

        // Format 'you' snake if exists
        if (formatted.MoveRequest.you?.body) {
            formatted.MoveRequest.you.body = JSON.stringify(formatted.MoveRequest.you.body);
        }

        return formatted;
    }

    /**
     * Validate JSON only
     */
    validateJSON() {
        const input = this.elements.jsonListInput?.value?.trim();

        if (!input) {
            window.NotifyService?.warning('⚠️ Nessun contenuto da validare');
            return;
        }

        try {
            const parsed = JSON.parse(input);
            const isArray = Array.isArray(parsed);
            const itemCount = isArray ? parsed.length : 1;

            this.updateValidationStatus(
                `✅ JSON valido - ${itemCount} elemento${itemCount !== 1 ? 'i' : ''}`,
                'success'
            );

            window.NotifyService?.success('✅ JSON valido');

        } catch (error) {
            this.updateValidationStatus(`❌ JSON non valido: ${error.message}`, 'error');
            window.NotifyService?.error('❌ JSON non valido');
        }
    }

    /**
     * Debounced validation for live feedback
     */
    debounceValidation() {
        if (this.debounceTimeout) {
            clearTimeout(this.debounceTimeout);
        }

        this.debounceTimeout = setTimeout(() => {
            this.validateInput();
        }, 500);
    }

    /**
     * Validate input for live feedback
     */
    validateInput() {
        const input = this.elements.jsonListInput?.value?.trim();

        if (!input) {
            this.clearValidationStatus();
            return;
        }

        try {
            JSON.parse(input);
            this.updateValidationStatus('✅ JSON valido', 'success');
        } catch (error) {
            this.updateValidationStatus('❌ JSON non valido', 'error');
        }
    }

    /**
     * Prettify JSON
     */
    prettifyJSON() {
        const input = this.elements.jsonListInput?.value?.trim();

        if (!input) {
            window.NotifyService?.error('❌ Nessun contenuto da formattare');
            return;
        }

        try {
            const parsed = JSON.parse(input);
            const prettified = JSON.stringify(parsed, null, 2);
            this.elements.jsonListInput.value = prettified;
            this.updateStats();
            window.NotifyService?.success('✅ JSON formattato');

        } catch (error) {
            window.NotifyService?.error('❌ JSON non valido per la formattazione');
        }
    }

    /**
     * Minify JSON
     */
    minifyJSON() {
        const input = this.elements.jsonListInput?.value?.trim();

        if (!input) {
            window.NotifyService?.error('❌ Nessun contenuto da comprimere');
            return;
        }

        try {
            const parsed = JSON.parse(input);
            const minified = JSON.stringify(parsed);
            this.elements.jsonListInput.value = minified;
            this.updateStats();
            window.NotifyService?.success('✅ JSON compresso');

        } catch (error) {
            window.NotifyService?.error('❌ JSON non valido per la compressione');
        }
    }

    /**
     * Clear input
     */
    clearInput() {
        if (this.elements.jsonListInput) {
            this.elements.jsonListInput.value = '';
            this.updateStats();
            this.clearValidationStatus();
            this.hideOutput();
            this.elements.jsonListInput.focus();
            window.NotifyService?.success('✅ Campo pulito');
        }
    }

    /**
     * Load example JSON
     */
    loadExample() {
        const exampleJSON = this.getExampleJSON();
        if (this.elements.jsonListInput) {
            this.elements.jsonListInput.value = exampleJSON;
            this.updateStats();
            this.validateInput();
            window.NotifyService?.success('✅ Esempio caricato');
        }
    }

    /**
     * Get example JSON content
     */
    getExampleJSON() {
        return `[
  {
    "Id": 127,
    "Name": "Test Movement Up",
    "Expected": 1,
    "MoveRequest": {
      "game": {
        "id": "test-game-id",
        "ruleset": {
          "name": "standard",
          "version": "v1.0.0"
        },
        "timeout": 500
      },
      "turn": 1,
      "board": {
        "height": 11,
        "width": 11,
        "food": [
          {"x": 5, "y": 4}
        ],
        "hazards": [],
        "snakes": [
          {
            "id": "snake-1",
            "name": "Test Snake",
            "health": 100,
            "body": [
              {"x": 5, "y": 5},
              {"x": 5, "y": 6}
            ],
            "head": {"x": 5, "y": 5},
            "length": 2,
            "latency": "0",
            "shout": ""
          }
        ]
      },
      "you": {
        "id": "snake-1",
        "name": "Test Snake",
        "health": 100,
        "body": [
          {"x": 5, "y": 5},
          {"x": 5, "y": 6}
        ],
        "head": {"x": 5, "y": 5},
        "length": 2,
        "latency": "0",
        "shout": ""
      }
    }
  }
]`;
    }

    /**
     * Update statistics
     */
    updateStats() {
        const input = this.elements.jsonListInput?.value || '';
        const lines = input.split('\n').length;
        const chars = input.length;
        let itemCount = 0;

        try {
            if (input.trim()) {
                const parsed = JSON.parse(input);
                itemCount = Array.isArray(parsed) ? parsed.length : 1;
            }
        } catch (error) {
            // Invalid JSON, keep count at 0
        }

        if (this.elements.jsonLineCount) {
            this.elements.jsonLineCount.textContent = lines;
        }
        if (this.elements.jsonCharCount) {
            this.elements.jsonCharCount.textContent = chars;
        }
        if (this.elements.jsonItemCount) {
            this.elements.jsonItemCount.textContent = itemCount;
        }
        if (this.elements.jsonStatus) {
            this.elements.jsonStatus.textContent = input.trim() ? 'Contenuto presente' : 'Vuoto';
        }
    }

    /**
     * Show output section
     */
    showOutput(content) {
        if (this.elements.formatterOutput && this.elements.formatterCode) {
            this.elements.formatterCode.textContent = content;
            this.elements.formatterOutput.style.display = 'block';

            // Scroll to output
            this.elements.formatterOutput.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    }

    /**
     * Hide output section
     */
    hideOutput() {
        if (this.elements.formatterOutput) {
            this.elements.formatterOutput.style.display = 'none';
        }
    }

    /**
     * Copy formatted JSON to clipboard
     */
    async copyToClipboard() {
        const content = this.elements.formatterCode?.textContent;

        if (!content) {
            window.NotifyService?.error('❌ Nessun contenuto da copiare');
            return;
        }

        try {
            await navigator.clipboard.writeText(content);

            // Animate button
            this.animateButton(this.elements.copyFormatterBtn, '✅ Copiato!');
            window.NotifyService?.success('✅ JSON copiato negli appunti');

        } catch (error) {
            console.error('Failed to copy:', error);
            window.NotifyService?.error('❌ Errore durante la copia');
        }
    }

    /**
     * Export JSON as file
     */
    exportJSON() {
        const content = this.elements.formatterCode?.textContent;

        if (!content) {
            window.NotifyService?.error('❌ Nessun contenuto da esportare');
            return;
        }

        try {
            const blob = new Blob([content], { type: 'application/json' });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');

            link.href = url;
            link.download = `battlesnake-tests-${Date.now()}.json`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            URL.revokeObjectURL(url);

            window.NotifyService?.success('✅ File esportato');

        } catch (error) {
            console.error('Export error:', error);
            window.NotifyService?.error('❌ Errore durante l\'esportazione');
        }
    }

    /**
     * Update validation status display
     */
    updateValidationStatus(message, type) {
        if (this.elements.jsonValidation) {
            this.elements.jsonValidation.textContent = message;
            this.elements.jsonValidation.className = `validation-status validation-${type}`;
            this.elements.jsonValidation.style.display = 'block';
        }
    }

    /**
     * Clear validation status
     */
    clearValidationStatus() {
        if (this.elements.jsonValidation) {
            this.elements.jsonValidation.style.display = 'none';
        }
    }

    /**
     * Show progress indicator
     */
    showProgress(show) {
        if (this.elements.formatProgress) {
            this.elements.formatProgress.classList.toggle('active', show);
        }

        if (this.elements.formatBtn) {
            this.elements.formatBtn.classList.toggle('loading', show);
            this.elements.formatBtn.disabled = show;
        }
    }

    /**
     * Animate button with temporary text
     */
    animateButton(button, tempText) {
        if (!button) return;

        const originalText = button.textContent;
        button.textContent = tempText;
        button.style.background = 'linear-gradient(45deg, #10b981, #059669)';

        setTimeout(() => {
            button.textContent = originalText;
            button.style.background = '';
        }, 2000);
    }
}

// Create and register the formatter tab
const formatterTab = new FormatterTab();

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        formatterTab.init();
    });
} else {
    formatterTab.init();
}

// Export for global access
window.BattlesnakeFormatterTab = formatterTab;