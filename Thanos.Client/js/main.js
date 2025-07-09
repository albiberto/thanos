// Battlesnake Board Converter - Main Entry Point

class BattlesnakeApp {
    constructor() {
        this.initialized = false;
        this.version = '2.0.0';
    }

    // Initialize the entire application
    async init() {
        if (this.initialized) return;

        console.log(`🐍 Battlesnake Board Converter v${this.version} - Starting...`);

        try {
            // Wait for DOM to be ready
            await this.waitForDOM();

            // Initialize core systems
            this.initializeTabManager();
            this.setupGlobalEventListeners();
            this.setupKeyboardShortcuts();

            // Initialize all tabs
            await this.initializeTabs();

            // Setup copy to clipboard handlers
            this.setupClipboardHandlers();

            // Show welcome message
            this.showWelcomeMessage();

            this.initialized = true;
            console.log('✅ Battlesnake Board Converter initialized successfully');

        } catch (error) {
            console.error('❌ Failed to initialize application:', error);
            this.showErrorMessage('Errore durante l\'inizializzazione dell\'applicazione');
        }
    }

    // Wait for DOM to be fully loaded
    waitForDOM() {
        return new Promise((resolve) => {
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', resolve);
            } else {
                resolve();
            }
        });
    }

    // Initialize tab manager
    initializeTabManager() {
        if (window.BattlesnakeTabManager) {
            window.BattlesnakeTabManager.tabManager.init();
            console.log('✅ Tab manager initialized');
        } else {
            console.warn('⚠️ Tab manager not available');
        }
    }

    // Initialize all tabs
    async initializeTabs() {
        const tabs = ['import', 'process', 'formatter'];

        for (const tabName of tabs) {
            try {
                // Tabs auto-register themselves, just need to ensure they're initialized
                await this.waitForTabInitialization(tabName);
                console.log(`✅ ${tabName} tab initialized`);
            } catch (error) {
                console.warn(`⚠️ Failed to initialize ${tabName} tab:`, error);
            }
        }
    }

    // Wait for tab to be initialized
    waitForTabInitialization(tabName, timeout = 2000) {
        return new Promise((resolve, reject) => {
            const startTime = Date.now();

            const checkTab = () => {
                if (window.BattlesnakeTabManager?.tabManager.hasTab(tabName)) {
                    resolve();
                } else if (Date.now() - startTime > timeout) {
                    reject(new Error(`Tab ${tabName} initialization timeout`));
                } else {
                    setTimeout(checkTab, 50);
                }
            };

            checkTab();
        });
    }

    // Setup global event listeners
    setupGlobalEventListeners() {
        // Handle board import events
        document.addEventListener('boardsImported', (event) => {
            console.log(`📥 Boards imported: ${event.detail.count} boards`);
        });

        // Handle window beforeunload
        window.addEventListener('beforeunload', (event) => {
            const boards = window.BattlesnakeCommon?.getImportedBoards() || [];
            if (boards.length > 0) {
                event.preventDefault();
                event.returnValue = 'Hai delle board caricate. Sei sicuro di voler uscire?';
            }
        });

        // Handle errors
        window.addEventListener('error', (event) => {
            console.error('Global error:', event.error);
            this.showErrorMessage('Si è verificato un errore imprevisto');
        });

        // Handle unhandled promise rejections
        window.addEventListener('unhandledrejection', (event) => {
            console.error('Unhandled promise rejection:', event.reason);
            this.showErrorMessage('Errore durante un\'operazione asincrona');
        });

        console.log('✅ Global event listeners setup');
    }

    // Setup keyboard shortcuts
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (event) => {
            // Ctrl/Cmd + Tab numbers for quick tab switching
            if ((event.ctrlKey || event.metaKey) && !event.shiftKey) {
                switch (event.key) {
                    case '1':
                        event.preventDefault();
                        this.switchToTab('import');
                        break;
                    case '2':
                        event.preventDefault();
                        this.switchToTab('process');
                        break;
                    case '3':
                        event.preventDefault();
                        this.switchToTab('formatter');
                        break;
                    case 'Enter':
                        // Quick actions based on current tab
                        event.preventDefault();
                        this.handleQuickAction();
                        break;
                }
            }

            // ESC to clear status messages
            if (event.key === 'Escape') {
                this.clearStatusMessage();
            }
        });

        console.log('✅ Keyboard shortcuts setup');
    }

    // Setup clipboard handlers
    setupClipboardHandlers() {
        // Main JSON output copy button
        const copyButton = document.getElementById('copyButton');
        if (copyButton) {
            copyButton.addEventListener('click', () => {
                window.BattlesnakeCommon?.copyToClipboard('jsonCode', 'copyButton');
            });
        }

        // Formatter output copy button
        const copyFormatterBtn = document.querySelector('[onclick*="copyFormatterOutput"]');
        if (copyFormatterBtn) {
            copyFormatterBtn.addEventListener('click', () => {
                window.BattlesnakeFormatterTab?.copyFormatterOutput();
            });
        }

        console.log('✅ Clipboard handlers setup');
    }

    // Switch to a specific tab
    switchToTab(tabName) {
        if (window.BattlesnakeTabManager) {
            window.BattlesnakeTabManager.switchTab(tabName);
        }
    }

    // Handle quick actions based on current tab
    handleQuickAction() {
        const currentTab = window.BattlesnakeTabManager?.tabManager.getCurrentTab();

        switch (currentTab) {
            case 'import':
                window.BattlesnakeImportTab?.importBoards();
                break;
            case 'process':
                window.BattlesnakeProcessTab?.generateAllJSON();
                break;
            case 'formatter':
                window.BattlesnakeFormatterTab?.formatJSONList();
                break;
        }
    }

    // Show welcome message
    showWelcomeMessage() {
        if (window.BattlesnakeCommon) {
            window.BattlesnakeCommon.showStatus(
                '🐍 Benvenuto in Battlesnake Board Converter! Inizia importando le tue griglie.'
            );
        }
    }

    // Show error message
    showErrorMessage(message) {
        if (window.BattlesnakeCommon) {
            window.BattlesnakeCommon.showStatus(message, true);
        } else {
            console.error(message);
        }
    }

    // Clear status message
    clearStatusMessage() {
        const statusDiv = document.getElementById('statusMessage');
        if (statusDiv) {
            statusDiv.innerHTML = '';
        }
    }

    // Get application info
    getInfo() {
        return {
            version: this.version,
            initialized: this.initialized,
            currentTab: window.BattlesnakeTabManager?.tabManager.getCurrentTab(),
            boardsLoaded: window.BattlesnakeCommon?.getImportedBoards().length || 0,
            registeredTabs: window.BattlesnakeTabManager?.tabManager.getRegisteredTabs() || []
        };
    }

    // Export current state
    exportState() {
        const boards = window.BattlesnakeCommon?.getImportedBoards() || [];
        const expectedValues = window.BattlesnakeCommon?.getBoardExpectedValues() || [];

        return {
            version: this.version,
            timestamp: new Date().toISOString(),
            boards: boards,
            expectedValues: expectedValues,
            configuration: window.BattlesnakeProcessTab?.processTab.getConfiguration() || {}
        };
    }

    // Import state
    importState(state) {
        try {
            if (state.boards && Array.isArray(state.boards)) {
                window.BattlesnakeCommon?.setImportedBoards(state.boards);

                if (state.expectedValues && Array.isArray(state.expectedValues)) {
                    state.expectedValues.forEach((value, index) => {
                        window.BattlesnakeCommon?.setBoardExpectedValue(index, value);
                    });
                }

                // Update UI
                window.BattlesnakeProcessTab?.processTab.updateUI();

                this.showSuccessMessage(`Stato importato: ${state.boards.length} board caricate`);
                this.switchToTab('process');
            }
        } catch (error) {
            this.showErrorMessage('Errore durante l\'importazione dello stato');
            console.error('Import state error:', error);
        }
    }

    // Show success message
    showSuccessMessage(message) {
        if (window.BattlesnakeCommon) {
            window.BattlesnakeCommon.showStatus(message);
        }
    }
}

// Create and initialize the application
const battlesnakeApp = new BattlesnakeApp();

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    battlesnakeApp.init();
});

// Make app globally available for debugging
window.BattlesnakeApp = battlesnakeApp;

// Global helper functions for backward compatibility
function copyToClipboard() {
    window.BattlesnakeCommon?.copyToClipboard('jsonCode', 'copyButton');
}

// Export for module use
window.BattlesnakeMain = {
    app: battlesnakeApp,
    copyToClipboard
};