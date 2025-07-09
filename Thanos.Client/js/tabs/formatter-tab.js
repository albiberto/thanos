/**
 * Battlesnake Board Converter - Formatter Tab JavaScript
 * Handles JSON list formatting, validation, and export functionality
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

        this.loadHTML().then(() => {
            this.cacheElements();
            this.setupEventListeners();
            this.updateStats();
            this.initialized = true;
            console.log('FormatterTab initialized');
        }).catch(error => {
            console.error('Failed to initialize FormatterTab:', error);
        });
    }

    /**
     * Load the HTML content for the formatter tab
     */
    async loadHTML() {
        const formatterTab = document.getElementById('formatter-tab');
        if (!formatterTab) {
            throw new Error('Formatter tab container not found');
        }

        // HTML content for the formatter tab
        const htmlContent = `
            <div class="formatter-content">
                <!-- JSON Input Section -->
                <div class="formatter-section">
                    <h2>📝 JSON List Input</h2>
                    
                    <div class="hint">
                        <div class="hint-title">📋 Formato richiesto:</div>
                        Incolla qui una lista di JSON (array) da formattare con body inline per i test Battlesnake
                    </div>

                    <div class="quick-actions">
                        <button class="quick-action-btn" id="prettifyBtn">🎨 Prettify</button>
                        <button class="quick-action-btn" id="minifyBtn">📦 Minify</button>
                        <button class="quick-action-btn" id="clearJsonBtn">🗑️ Clear</button>
                        <button class="quick-action-btn" id="exampleJsonBtn">📋 Example</button>
                    </div>

                    <div class="json-input-container">
                        <textarea 
                            id="jsonListInput" 
                            class="json-input" 
                            placeholder="Incolla qui la lista di JSON...

Esempio:
[
  {
    &quot;Id&quot;: 127,
    &quot;Name&quot;: &quot;Test Movement Up&quot;,
    &quot;Expected&quot;: 1,
    &quot;MoveRequest&quot;: {
      &quot;game&quot;: {
        &quot;id&quot;: &quot;test-game&quot;,
        &quot;ruleset&quot;: {
          &quot;name&quot;: &quot;standard&quot;
        }
      },
      &quot;turn&quot;: 1,
      &quot;board&quot;: {
        &quot;height&quot;: 11,
        &quot;width&quot;: 11,
        &quot;food&quot;: [
          {&quot;x&quot;: 5, &quot;y&quot;: 4}
        ],
        &quot;snakes&quot;: [
          {
            &quot;id&quot;: &quot;snake-1&quot;,
            &quot;name&quot;: &quot;Test Snake&quot;,
            &quot;health&quot;: 100,
            &quot;body&quot;: [
              {&quot;x&quot;: 5, &quot;y&quot;: 5},
              {&quot;x&quot;: 5, &quot;y&quot;: 6}
            ],
            &quot;head&quot;: {&quot;x&quot;: 5, &quot;y&quot;: 5}
          }
        ]
      },
      &quot;you&quot;: {
        &quot;id&quot;: &quot;snake-1&quot;,
        &quot;name&quot;: &quot;Test Snake&quot;,
        &quot;health&quot;: 100,
        &quot;body&quot;: [
          {&quot;x&quot;: 5, &quot;y&quot;: 5},
          {&quot;x&quot;: 5, &quot;y&quot;: 6}
        ],
        &quot;head&quot;: {&quot;x&quot;: 5, &quot;y&quot;: 5}
      }
    }
  }
]"></textarea>

                        <div class="input-stats" id="jsonInputStats">
                            <span>Lines: <span class="stat-number" id="jsonLineCount">0</span></span>
                            <span>Characters: <span class="stat-number" id="jsonCharCount">0</span></span>
                            <span>Items: <span class="stat-number" id="jsonItemCount">0</span></span>
                            <span>Status: <span class="stat-number" id="jsonValidStatus">-</span></span>
                        </div>

                        <div class="validation-status" id="jsonValidation" style="display: none;"></div>
                    </div>
                </div>

                <!-- Controls Section -->
                <div class="formatter-controls">
                    <h3>🔧 Formatting Controls</h3>
                    
                    <div class="control-row">
                        <button class="button" id="formatBtn">
                            🔄 Format JSON List
                        </button>
                        <button class="button secondary" id="validateBtn">
                            ✅ Validate Only
                        </button>
                    </div>

                    <div class="format-progress" id="formatProgress">
                        Processing JSON list...
                    </div>
                </div>
            </div>

            <!-- Formatter Output Section (appears when formatting is done) -->
            <div class="formatter-output" id="formatterOutput" style="display: none;">
                <h2>📄 Formatted JSON List</h2>
                
                <div class="formatter-output-code" id="formatterCode"></div>
                
                <div class="formatter-output-actions">
                    <button class="button" id="copyFormatterBtn">
                        📋 Copy to Clipboard
                    </button>
                    <button class="button export" id="exportBtn">
                        💾 Export as File
                    </button>
                    <button class="button secondary" id="hideOutputBtn">
                        🙈 Hide Output
                    </button>
                </div>
            </div>
        `;

        // Insert the HTML content
        formatterTab.innerHTML = htmlContent;
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
            jsonValidStatus: document.getElementById('jsonValidStatus')
        };
    }

    /**
     * Setup all event listeners
     */
    setupEventListeners() {
        // Main action buttons
        if (this.elements.formatBtn) {
            this.elements.formatBtn.addEventListener('click', () => this.formatJSONList());
        }

        if (this.elements.validateBtn) {
            this.elements.validateBtn.addEventListener('click', () => this.validateOnly());
        }

        // Quick action buttons
        if (this.elements.prettifyBtn) {
            this.elements.prettifyBtn.addEventListener('click', () => this.prettifyInput());
        }

        if (this.elements.minifyBtn) {
            this.elements.minifyBtn.addEventListener('click', () => this.minifyInput());
        }

        if (this.elements.clearJsonBtn) {
            this.elements.clearJsonBtn.addEventListener('click', () => this.clearInput());
        }

        if (this.elements.exampleJsonBtn) {
            this.elements.exampleJsonBtn.addEventListener('click', () => this.loadExample());
        }

        // Output action buttons
        if (this.elements.copyFormatterBtn) {
            this.elements.copyFormatterBtn.addEventListener('click', () => this.copyFormatterOutput());
        }

        if (this.elements.exportBtn) {
            this.elements.exportBtn.addEventListener('click', () => this.exportFormatted());
        }

        if (this.elements.hideOutputBtn) {
            this.elements.hideOutputBtn.addEventListener('click', () => this.hideOutput());
        }

        // Input change handlers with debouncing
        if (this.elements.jsonListInput) {
            this.elements.jsonListInput.addEventListener('input', () => this.debouncedUpdateStats());
            this.elements.jsonListInput.addEventListener('paste', () => {
                setTimeout(() => this.debouncedUpdateStats(), 10);
            });
        }

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => this.handleKeyboardShortcuts(e));
    }

    /**
     * Handle keyboard shortcuts
     */
    handleKeyboardShortcuts(event) {
        // Only handle shortcuts when formatter tab is active
        const formatterTab = document.getElementById('formatter-tab');
        if (!formatterTab || !formatterTab.classList.contains('active')) return;

        // Ctrl/Cmd + Enter to format
        if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
            event.preventDefault();
            this.formatJSONList();
        }

        // Ctrl/Cmd + Shift + F to prettify
        if ((event.ctrlKey || event.metaKey) && event.shiftKey && event.key === 'F') {
            event.preventDefault();
            this.prettifyInput();
        }

        // Ctrl/Cmd + K to clear
        if ((event.ctrlKey || event.metaKey) && event.key === 'k') {
            event.preventDefault();
            this.clearInput();
        }
    }

    /**
     * Debounced update stats function
     */
    debouncedUpdateStats() {
        clearTimeout(this.debounceTimeout);
        this.debounceTimeout = setTimeout(() => {
            this.updateStats();
            this.validateInput();
        }, 300);
    }

    /**
     * Main format JSON list function
     */
    formatJSONList() {
        try {
            this.showProgress(true);

            const inputText = this.elements.jsonListInput.value.trim();

            if (!inputText) {
                throw new Error('Please enter a JSON list to format');
            }

            const jsonList = JSON.parse(inputText);

            if (!Array.isArray(jsonList)) {
                throw new Error('Input must be a JSON array');
            }

            // Format each item in the list using BattlesnakeCommon if available
            const formattedList = jsonList.map(item => {
                if (window.BattlesnakeCommon && window.BattlesnakeCommon.formatSingleJSON) {
                    return window.BattlesnakeCommon.formatSingleJSON(item);
                } else {
                    // Fallback formatting
                    return this.formatSingleItem(item);
                }
            });

            // Create final formatted output
            const output = '[\n' + formattedList.map(json =>
                json.replace(/^/gm, '  ')
            ).join(',\n') + '\n]';

            // Display output
            this.displayFormattedOutput(output);
            this.showStatus('✅ JSON list formatted successfully!', 'success');

        } catch (error) {
            this.showStatus(`❌ Error: ${error.message}`, 'error');
            console.error('Formatting error:', error);
            this.hideOutput();
        } finally {
            this.showProgress(false);
        }
    }

    /**
     * Fallback formatting for single item
     */
    formatSingleItem(item) {
        return JSON.stringify(item, null, 2);
    }

    /**
     * Validate input only (without formatting)
     */
    validateOnly() {
        try {
            const inputText = this.elements.jsonListInput.value.trim();

            if (!inputText) {
                throw new Error('Please enter JSON to validate');
            }

            const parsed = JSON.parse(inputText);

            if (!Array.isArray(parsed)) {
                throw new Error('Input must be a JSON array');
            }

            this.showStatus(`✅ Valid JSON array with ${parsed.length} items`, 'success');

        } catch (error) {
            this.showStatus(`❌ Validation error: ${error.message}`, 'error');
        }
    }

    /**
     * Display formatted output
     */
    displayFormattedOutput(output) {
        if (this.elements.formatterCode && this.elements.formatterOutput) {
            this.elements.formatterCode.textContent = output;
            this.elements.formatterOutput.style.display = 'flex';

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
     * Copy formatter output to clipboard
     */
    copyFormatterOutput() {
        const content = this.elements.formatterCode.textContent;

        if (!content) {
            this.showStatus('❌ No output to copy', 'error');
            return;
        }

        navigator.clipboard.writeText(content)
        .then(() => {
            this.showStatus('✅ Copied to clipboard!', 'success');
            this.animateButton(this.elements.copyFormatterBtn, 'Copied!');
        })
        .catch(err => {
            console.error('Copy failed:', err);
            this.fallbackCopy(content);
        });
    }

    /**
     * Fallback copy method
     */
    fallbackCopy(text) {
        const textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-999999px';
        document.body.appendChild(textArea);
        textArea.select();

        try {
            const successful = document.execCommand('copy');
            if (successful) {
                this.showStatus('✅ Copied to clipboard!', 'success');
            } else {
                this.showStatus('❌ Copy failed', 'error');
            }
        } catch (err) {
            this.showStatus('❌ Copy not supported', 'error');
        }

        document.body.removeChild(textArea);
    }

    /**
     * Export formatted result as file
     */
    exportFormatted() {
        const content = this.elements.formatterCode.textContent;

        if (!content) {
            this.showStatus('❌ No output to export', 'error');
            return;
        }

        try {
            // Create download link
            const blob = new Blob([content], { type: 'application/json' });
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `formatted-battlesnake-tests-${Date.now()}.json`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);

            this.showStatus('✅ File exported successfully!', 'success');
            this.animateButton(this.elements.exportBtn, 'Exported!');

        } catch (error) {
            this.showStatus('❌ Export failed', 'error');
            console.error('Export error:', error);
        }
    }

    /**
     * Prettify JSON input
     */
    prettifyInput() {
        try {
            const inputText = this.elements.jsonListInput.value.trim();
            if (!inputText) {
                this.showStatus('❌ No JSON to prettify', 'error');
                return;
            }

            const parsed = JSON.parse(inputText);
            const prettified = JSON.stringify(parsed, null, 2);
            this.elements.jsonListInput.value = prettified;
            this.updateStats();
            this.validateInput();
            this.showStatus('✅ JSON prettified!', 'success');

        } catch (error) {
            this.showStatus('❌ Invalid JSON for prettifying', 'error');
        }
    }

    /**
     * Minify JSON input
     */
    minifyInput() {
        try {
            const inputText = this.elements.jsonListInput.value.trim();
            if (!inputText) {
                this.showStatus('❌ No JSON to minify', 'error');
                return;
            }

            const parsed = JSON.parse(inputText);
            const minified = JSON.stringify(parsed);
            this.elements.jsonListInput.value = minified;
            this.updateStats();
            this.validateInput();
            this.showStatus('✅ JSON minified!', 'success');

        } catch (error) {
            this.showStatus('❌ Invalid JSON for minifying', 'error');
        }
    }

    /**
     * Clear input
     */
    clearInput() {
        this.elements.jsonListInput.value = '';
        this.updateStats();
        this.clearValidationStatus();
        this.hideOutput();
        this.showStatus('✅ Input cleared', 'success');
        this.elements.jsonListInput.focus();
    }

    /**
     * Load example JSON
     */
    loadExample() {
        const exampleJSON = this.getExampleJSON();
        this.elements.jsonListInput.value = exampleJSON;
        this.updateStats();
        this.validateInput();
        this.showStatus('✅ Example loaded', 'success');
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
  },
  {
    "Id": 128,
    "Name": "Test Movement Down",
    "Expected": 2,
    "MoveRequest": {
      "game": {
        "id": "test-game-id-2",
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
          {"x": 5, "y": 6}
        ],
        "hazards": [],
        "snakes": [
          {
            "id": "snake-2",
            "name": "Test Snake 2",
            "health": 100,
            "body": [
              {"x": 5, "y": 5},
              {"x": 5, "y": 4}
            ],
            "head": {"x": 5, "y": 5},
            "length": 2,
            "latency": "0",
            "shout": ""
          }
        ]
      },
      "you": {
        "id": "snake-2",
        "name": "Test Snake 2",
        "health": 100,
        "body": [
          {"x": 5, "y": 5},
          {"x": 5, "y": 4}
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
     * Update input statistics
     */
    updateStats() {
        const text = this.elements.jsonListInput.value;
        const stats = this.getInputStats(text);

        this.elements.jsonLineCount.textContent = stats.lines;
        this.elements.jsonCharCount.textContent = stats.chars;
        this.elements.jsonItemCount.textContent = stats.items;

        // Update status with color coding
        const statusElement = this.elements.jsonValidStatus;
        if (stats.valid) {
            statusElement.textContent = 'Valid';
            statusElement.className = 'stat-number stat-valid';
        } else if (text.trim()) {
            statusElement.textContent = 'Invalid';
            statusElement.className = 'stat-number stat-invalid';
        } else {
            statusElement.textContent = '-';
            statusElement.className = 'stat-number';
        }
    }

    /**
     * Get input statistics
     */
    getInputStats(text) {
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

    /**
     * Validate input and show status
     */
    validateInput() {
        const inputText = this.elements.jsonListInput.value.trim();

        if (!inputText) {
            this.clearValidationStatus();
            this.updateInputStyle('normal');
            return;
        }

        try {
            const parsed = JSON.parse(inputText);

            if (!Array.isArray(parsed)) {
                this.showValidationError('Must be a JSON array');
                this.updateInputStyle('error');
                return;
            }

            this.showValidationSuccess(`Valid array with ${parsed.length} items`);
            this.updateInputStyle('valid');

        } catch (error) {
            this.showValidationError(`Invalid JSON: ${error.message}`);
            this.updateInputStyle('error');
        }
    }

    /**
     * Update input styling based on validation
     */
    updateInputStyle(type) {
        const input = this.elements.jsonListInput;
        input.classList.remove('error', 'valid');

        if (type === 'error') {
            input.classList.add('error');
        } else if (type === 'valid') {
            input.classList.add('valid');
        }
    }

    /**
     * Show validation success message
     */
    showValidationSuccess(message) {
        this.updateValidationStatus(message, 'success');
    }

    /**
     * Show validation error message
     */
    showValidationError(message) {
        this.updateValidationStatus(message, 'error');
    }

    /**
     * Update validation status display
     */
    updateValidationStatus(message, type) {
        const statusElement = this.elements.jsonValidation;
        statusElement.textContent = message;
        statusElement.className = `validation-status validation-${type}`;
        statusElement.style.display = 'block';
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
     * Show status message
     */
    showStatus(message, type = 'success') {
        // Use BattlesnakeCommon if available
        if (window.BattlesnakeCommon && window.BattlesnakeCommon.showStatus) {
            window.BattlesnakeCommon.showStatus(message, type === 'error');
        } else {
            // Fallback to console
            console.log(`${type.toUpperCase()}: ${message}`);
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

    /**
     * Called when tab becomes active
     */
    onActivate() {
        // Focus on input if empty
        if (this.elements.jsonListInput && !this.elements.jsonListInput.value.trim()) {
            setTimeout(() => this.elements.jsonListInput.focus(), 100);
        }
    }

    /**
     * Called when tab becomes inactive
     */
    onDeactivate() {
        // Clear any visible status messages
        this.clearValidationStatus();
    }

    /**
     * Get current formatter state
     */
    getFormatterState() {
        return {
            hasInput: this.elements.jsonListInput.value.trim().length > 0,
            hasOutput: this.elements.formatterOutput.style.display !== 'none',
            isValid: this.getInputStats(this.elements.jsonListInput.value).valid,
            stats: this.getInputStats(this.elements.jsonListInput.value)
        };
    }
}

// Create global instance
const formatterTab = new FormatterTab();

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => formatterTab.init());
} else {
    formatterTab.init();
}

// Global functions for backward compatibility
window.formatJSONList = () => formatterTab.formatJSONList();
window.copyFormatterOutput = () => formatterTab.copyFormatterOutput();

// Export for module use and integration
window.BattlesnakeFormatterTab = {
    instance: formatterTab,
    FormatterTab: FormatterTab
};

// Auto-register with tab manager when available
document.addEventListener('DOMContentLoaded', () => {
    if (window.BattlesnakeTabManager) {
        window.BattlesnakeTabManager.registerTab('formatter', formatterTab);
    }
});