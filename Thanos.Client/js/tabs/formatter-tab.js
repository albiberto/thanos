// Battlesnake Board Converter - Formatter Tab

class FormatterTab {
    constructor() {
        this.initialized = false;
    }

    // Initialize the formatter tab
    init() {
        if (this.initialized) return;

        this.setupEventListeners();
        this.initialized = true;

        console.log('FormatterTab initialized');
    }

    // Setup event listeners
    setupEventListeners() {
        // Format JSON button
        const formatBtn = document.getElementById('formatBtn');
        if (formatBtn) {
            formatBtn.addEventListener('click', () => this.formatJSONList());
        }

        // Copy formatter output button
        const copyFormatterBtn = document.getElementById('copyFormatterBtn');
        if (copyFormatterBtn) {
            copyFormatterBtn.addEventListener('click', () => this.copyFormatterOutput());
        }

        // Auto-format on input change (debounced)
        const jsonListInput = document.getElementById('jsonListInput');
        if (jsonListInput) {
            let timeout;
            jsonListInput.addEventListener('input', () => {
                clearTimeout(timeout);
                timeout = setTimeout(() => this.validateInput(), 500);
            });
        }
    }

    // Format JSON list
    formatJSONList() {
        try {
            const input = document.getElementById('jsonListInput');
            if (!input) {
                throw new Error('JSON input element not found');
            }

            const inputText = input.value.trim();

            if (!inputText) {
                throw new Error('Inserisci una lista di JSON da formattare');
            }

            const jsonList = JSON.parse(inputText);

            if (!Array.isArray(jsonList)) {
                throw new Error('Il JSON deve essere un array di oggetti');
            }

            // Format each item in the list
            const formattedList = jsonList.map(item =>
                window.BattlesnakeCommon.formatSingleJSON(item)
            );

            // Create final formatted output
            const output = '[\n' + formattedList.map(json =>
                json.replace(/^/gm, '  ')
            ).join(',\n') + '\n]';

            // Display output
            this.displayFormattedOutput(output);

            window.BattlesnakeCommon.showStatus('✅ Lista JSON formattata con successo!');

        } catch (error) {
            window.BattlesnakeCommon.showStatus(`❌ Errore: ${error.message}`, true);
            console.error('Formatting error:', error);
            this.clearFormattedOutput();
        }
    }

    // Display formatted output
    displayFormattedOutput(output) {
        const formatterCode = document.getElementById('formatterCode');
        const formatterOutput = document.getElementById('formatterOutput');

        if (formatterCode && formatterOutput) {
            formatterCode.textContent = output;
            formatterOutput.style.display = 'flex';
        }
    }

    // Clear formatted output
    clearFormattedOutput() {
        const formatterOutput = document.getElementById('formatterOutput');
        if (formatterOutput) {
            formatterOutput.style.display = 'none';
        }
    }

    // Copy formatter output to clipboard
    copyFormatterOutput() {
        if (window.BattlesnakeCommon) {
            window.BattlesnakeCommon.copyToClipboard('formatterCode');
        }
    }

    // Validate input JSON
    validateInput() {
        const input = document.getElementById('jsonListInput');
        if (!input) return;

        const inputText = input.value.trim();
        if (!inputText) {
            this.clearValidationStatus();
            return;
        }

        try {
            const parsed = JSON.parse(inputText);

            if (!Array.isArray(parsed)) {
                this.showValidationError('Deve essere un array JSON');
                return;
            }

            this.showValidationSuccess(`Array valido con ${parsed.length} elementi`);

        } catch (error) {
            this.showValidationError('JSON non valido: ' + error.message);
        }
    }

    // Show validation success
    showValidationSuccess(message) {
        this.updateValidationStatus(message, false);
    }

    // Show validation error
    showValidationError(message) {
        this.updateValidationStatus(message, true);
    }

    // Update validation status
    updateValidationStatus(message, isError) {
        let statusElement = document.getElementById('formatterValidation');

        if (!statusElement) {
            // Create validation status element if it doesn't exist
            statusElement = document.createElement('div');
            statusElement.id = 'formatterValidation';
            statusElement.style.cssText = `
                padding: 8px 12px;
                border-radius: 4px;
                font-size: 12px;
                margin-top: 8px;
                transition: opacity 0.2s ease;
            `;

            const input = document.getElementById('jsonListInput');
            if (input && input.parentNode) {
                input.parentNode.insertBefore(statusElement, input.nextSibling);
            }
        }

        statusElement.textContent = message;
        statusElement.className = isError ? 'status-error' : 'status-success';
        statusElement.style.display = 'block';
    }

    // Clear validation status
    clearValidationStatus() {
        const statusElement = document.getElementById('formatterValidation');
        if (statusElement) {
            statusElement.style.display = 'none';
        }
    }

    // Set example JSON input
    setExampleInput() {
        const input = document.getElementById('jsonListInput');
        if (input) {
            input.value = this.getExampleJSON();
            this.validateInput();
        }
    }

    // Get example JSON
    getExampleJSON() {
        return `[
  {
    "Id": 101,
    "Name": "Test Movement 1",
    "Expected": 13,
    "MoveRequest": {
      "game": {
        "id": "game-101",
        "ruleset": {
          "name": "standard"
        },
        "timeout": 500
      },
      "turn": 5,
      "board": {
        "height": 11,
        "width": 11,
        "food": [],
        "hazards": [],
        "snakes": [
          {
            "id": "thanos",
            "name": "Thanos",
            "health": 100,
            "body": [
              {"x": 5, "y": 3},
              {"x": 5, "y": 4},
              {"x": 5, "y": 5}
            ],
            "head": {"x": 5, "y": 3},
            "length": 3
          }
        ]
      }
    }
  }
]`;
    }

    // Clear input
    clearInput() {
        const input = document.getElementById('jsonListInput');
        if (input) {
            input.value = '';
            this.clearValidationStatus();
            this.clearFormattedOutput();
        }
    }

    // Called when tab becomes active
    onActivate() {
        // Focus on input if empty
        const input = document.getElementById('jsonListInput');
        if (input && !input.value.trim()) {
            setTimeout(() => input.focus(), 100);
        }
    }

    // Get input statistics
    getInputStats() {
        const input = document.getElementById('jsonListInput');
        if (!input) return null;

        const text = input.value;
        const lines = text.split('\n').length;
        const chars = text.length;

        try {
            const parsed = JSON.parse(text);
            const items = Array.isArray(parsed) ? parsed.length : 1;
            return { lines, chars, items, valid: true };
        } catch {
            return { lines, chars, items: 0, valid: false };
        }
    }

    // Prettify JSON input
    prettifyInput() {
        const input = document.getElementById('jsonListInput');
        if (!input) return;

        try {
            const parsed = JSON.parse(input.value);
            const prettified = JSON.stringify(parsed, null, 2);
            input.value = prettified;
            this.validateInput();
            window.BattlesnakeCommon.showStatus('✅ JSON formattato!');
        } catch (error) {
            window.BattlesnakeCommon.showStatus('❌ JSON non valido', true);
        }
    }

    // Minify JSON input
    minifyInput() {
        const input = document.getElementById('jsonListInput');
        if (!input) return;

        try {
            const parsed = JSON.parse(input.value);
            const minified = JSON.stringify(parsed);
            input.value = minified;
            this.validateInput();
            window.BattlesnakeCommon.showStatus('✅ JSON minimizzato!');
        } catch (error) {
            window.BattlesnakeCommon.showStatus('❌ JSON non valido', true);
        }
    }

    // Export formatted result
    exportFormatted() {
        const formatterCode = document.getElementById('formatterCode');
        if (!formatterCode || !formatterCode.textContent) {
            window.BattlesnakeCommon.showStatus('❌ Nessun output da esportare', true);
            return;
        }

        // Create download link
        const blob = new Blob([formatterCode.textContent], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'formatted-battlesnake-tests.json';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);

        window.BattlesnakeCommon.showStatus('✅ File esportato!');
    }
}

// Create formatter tab instance
const formatterTab = new FormatterTab();

// Global functions for backward compatibility
function formatJSONList() {
    formatterTab.formatJSONList();
}

function copyFormatterOutput() {
    formatterTab.copyFormatterOutput();
}

// Auto-register with tab manager when available
document.addEventListener('DOMContentLoaded', () => {
    if (window.BattlesnakeTabManager) {
        window.BattlesnakeTabManager.tabManager.registerTab('formatter', formatterTab);
    }
    formatterTab.init();
});

// Export for module use
window.BattlesnakeFormatterTab = {
    formatterTab,
    formatJSONList,
    copyFormatterOutput
};