/**
 * Battlesnake Board Converter - Main Entry Point
 * Updated: Integrates with global NotifyService, removed individual messaging
 */

class BattlesnakeApp {
    constructor() {
        this.initialized = false;
        this.version = '2.0.0';
        this.boards = [];
    }

    /**
     * Initialize the entire application
     */
    async init() {
        if (this.initialized) return;

        console.log(`🐍 Battlesnake Board Converter v${this.version} - Starting...`);

        try {
            // Wait for DOM to be ready
            await this.waitForDOM();

            // Initialize notification service first
            this.initializeNotificationService();

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

    /**
     * Wait for DOM to be fully loaded
     */
    waitForDOM() {
        return new Promise((resolve) => {
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', resolve);
            } else {
                resolve();
            }
        });
    }

    /**
     * Initialize notification service
     */
    initializeNotificationService() {
        if (window.NotifyService) {
            window.NotifyService.init();
            console.log('✅ Notification service initialized');
        } else {
            console.warn('⚠️ Notification service not available');
        }
    }

    /**
     * Initialize tab manager
     */
    initializeTabManager() {
        if (window.BattlesnakeTabManager) {
            window.BattlesnakeTabManager.tabManager.init();
            console.log('✅ Tab manager initialized');
        } else {
            console.warn('⚠️ Tab manager not available');
        }
    }

    /**
     * Initialize all tabs
     */
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

    /**
     * Wait for tab to be initialized
     */
    waitForTabInitialization(tabName) {
        return new Promise((resolve) => {
            const checkInterval = setInterval(() => {
                const tabManager = window.BattlesnakeTabManager?.tabManager;
                if (tabManager && tabManager.hasTab && tabManager.hasTab(tabName)) {
                    clearInterval(checkInterval);
                    resolve();
                }
            }, 100);

            // Timeout after 5 seconds
            setTimeout(() => {
                clearInterval(checkInterval);
                resolve();
            }, 5000);
        });
    }

    /**
     * Setup global event listeners
     */
    setupGlobalEventListeners() {
        // Global error handler
        window.addEventListener('error', (event) => {
            console.error('Global error:', event.error);
            this.showErrorMessage('Si è verificato un errore imprevisto');
        });

        // Handle unhandled promise rejections
        window.addEventListener('unhandledrejection', (event) => {
            console.error('Unhandled promise rejection:', event.reason);
            this.showErrorMessage('Errore durante l\'elaborazione');
        });

        // Handle beforeunload to save state
        window.addEventListener('beforeunload', () => {
            this.saveApplicationState();
        });

        // Handle visibility change to pause/resume operations
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                this.onApplicationPause();
            } else {
                this.onApplicationResume();
            }
        });
    }

    /**
     * Setup keyboard shortcuts
     */
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (event) => {
            // Ctrl/Cmd + 1, 2, 3 for tab switching
            if ((event.ctrlKey || event.metaKey) && !event.shiftKey && !event.altKey) {
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
                    case 's':
                        // Ctrl/Cmd + S to save/export
                        event.preventDefault();
                        this.handleQuickSave();
                        break;
                    case 'h':
                        // Ctrl/Cmd + H for help
                        event.preventDefault();
                        this.showHelp();
                        break;
                }
            }

            // Escape to close notifications
            if (event.key === 'Escape') {
                this.hideNotifications();
            }
        });
    }

    /**
     * Setup clipboard handlers
     */
    setupClipboardHandlers() {
        // Global copy handler
        document.addEventListener('copy', (event) => {
            // Custom copy logic can be added here
        });

        // Handle paste events globally
        document.addEventListener('paste', (event) => {
            // Handle global paste operations
            this.handleGlobalPaste(event);
        });
    }

    /**
     * Handle global paste operations
     */
    handleGlobalPaste(event) {
        const activeElement = document.activeElement;
        const pastedData = event.clipboardData?.getData('text');

        if (!pastedData) return;

        // Check if pasted data looks like JSON
        if (this.looksLikeJSON(pastedData)) {
            // If we're in import tab, suggest switching to formatter
            const currentTab = window.BattlesnakeTabManager?.tabManager?.getCurrentTab();
            if (currentTab === 'import') {
                setTimeout(() => {
                    window.NotifyService?.info('💡 Sembra JSON! Considera di usare il tab Formatter');
                }, 1000);
            }
        }
    }

    /**
     * Check if text looks like JSON
     */
    looksLikeJSON(text) {
        const trimmed = text.trim();
        return (trimmed.startsWith('{') && trimmed.endsWith('}')) ||
            (trimmed.startsWith('[') && trimmed.endsWith(']'));
    }

    /**
     * Switch to specific tab
     */
    switchToTab(tabName) {
        if (window.BattlesnakeTabManager?.tabManager?.switchTab) {
            window.BattlesnakeTabManager.tabManager.switchTab(tabName);
        }
    }

    /**
     * Show welcome message
     */
    showWelcomeMessage() {
        setTimeout(() => {
            window.NotifyService?.success('🐍 Battlesnake Board Converter caricato!', 2000);
        }, 500);
    }

    /**
     * Show error message
     */
    showErrorMessage(message) {
        window.NotifyService?.error(message);
    }

    /**
     * Show success message
     */
    showSuccessMessage(message) {
        window.NotifyService?.success(message);
    }

    /**
     * Handle quick save (Ctrl+S)
     */
    handleQuickSave() {
        const currentTab = window.BattlesnakeTabManager?.tabManager?.getCurrentTab();

        switch (currentTab) {
            case 'formatter':
                // Trigger formatter export
                const formatterTab = window.BattlesnakeFormatterTab;
                if (formatterTab && formatterTab.exportJSON) {
                    formatterTab.exportJSON();
                }
                break;
            case 'import':
                // Trigger import
                const importTab = window.BattlesnakeImportTab;
                if (importTab && importTab.importBoards) {
                    importTab.importBoards();
                }
                break;
            case 'process':
                // Save current processor state
                window.NotifyService?.info('💾 Stato processori salvato');
                break;
            default:
                window.NotifyService?.info('💾 Nessuna azione di salvataggio disponibile per questo tab');
        }
    }

    /**
     * Show help
     */
    showHelp() {
        const helpMessage = `
🔤 Scorciatoie da tastiera:
• Ctrl/Cmd + 1: Tab Import
• Ctrl/Cmd + 2: Tab Process  
• Ctrl/Cmd + 3: Tab Formatter
• Ctrl/Cmd + S: Salva/Esporta
• Ctrl/Cmd + H: Mostra aiuto
• Esc: Chiudi notifiche
        `.trim();

        window.NotifyService?.info(helpMessage, 8000);
    }

    /**
     * Hide notifications
     */
    hideNotifications() {
        window.NotifyService?.hide();
    }

    /**
     * Save application state
     */
    saveApplicationState() {
        try {
            const state = {
                version: this.version,
                timestamp: Date.now(),
                currentTab: window.BattlesnakeTabManager?.tabManager?.getCurrentTab(),
                boards: this.boards
            };

            localStorage.setItem('battlesnake-app-state', JSON.stringify(state));
        } catch (error) {
            console.warn('Failed to save application state:', error);
        }
    }

    /**
     * Load application state
     */
    loadApplicationState() {
        try {
            const savedState = localStorage.getItem('battlesnake-app-state');
            if (savedState) {
                const state = JSON.parse(savedState);

                // Restore boards if available
                if (state.boards && Array.isArray(state.boards)) {
                    this.boards = state.boards;
                    window.NotifyService?.info(`📁 ${state.boards.length} board caricate dallo stato precedente`);
                }

                // Switch to previous tab
                if (state.currentTab) {
                    setTimeout(() => {
                        this.switchToTab(state.currentTab);
                    }, 1000);
                }
            }
        } catch (error) {
            console.warn('Failed to load application state:', error);
        }
    }

    /**
     * Handle application pause (tab hidden)
     */
    onApplicationPause() {
        // Pause auto-refresh in process tab
        if (window.BattlesnakeProcessTab && window.BattlesnakeProcessTab.stopAutoRefresh) {
            window.BattlesnakeProcessTab.stopAutoRefresh();
        }
    }

    /**
     * Handle application resume (tab visible)
     */
    onApplicationResume() {
        // Resume auto-refresh in process tab
        if (window.BattlesnakeProcessTab && window.BattlesnakeProcessTab.startAutoRefresh) {
            window.BattlesnakeProcessTab.startAutoRefresh();
        }
    }

    /**
     * Import state from external source
     */
    importState(state) {
        try {
            if (state.boards && Array.isArray(state.boards)) {
                this.boards = state.boards;
                window.NotifyService?.success(`✅ ${state.boards.length} board importate`);
                this.switchToTab('process');
            }
        } catch (error) {
            window.NotifyService?.error('❌ Errore durante l\'importazione dello stato');
            console.error('Import state error:', error);
        }
    }

    /**
     * Get current application state
     */
    getState() {
        return {
            version: this.version,
            initialized: this.initialized,
            boards: this.boards,
            currentTab: window.BattlesnakeTabManager?.tabManager?.getCurrentTab()
        };
    }
}

// Create and initialize the application
const battlesnakeApp = new BattlesnakeApp();

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    battlesnakeApp.init();

    // Load previous state after initialization
    setTimeout(() => {
        battlesnakeApp.loadApplicationState();
    }, 1000);
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