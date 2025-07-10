// Battlesnake Board Converter - Main Application
// Inizializzazione semplificata

class BattlesnakeApp {
    constructor() {
        this.initialized = false;
    }

    /**
     * Initialize the application
     */
    async init() {
        if (this.initialized) return;

        console.log('🚀 Initializing Battlesnake Board Converter...');

        try {
            // Aspetta che il DOM sia completamente caricato
            await this.waitForDOM();

            // Inizializza i servizi di base
            this.initializeServices();

            // Inizializza il tab manager
            this.initializeTabManager();

            // Inizializza i tabs (simulati per ora)
            this.initializeTabs();

            this.initialized = true;
            console.log('✅ Application initialized successfully!');

        } catch (error) {
            console.error('❌ Failed to initialize application:', error);
        }
    }

    /**
     * Wait for DOM to be ready
     */
    waitForDOM() {
        return new Promise((resolve) => {
            if (document.readyState === 'complete' || document.readyState === 'interactive') {
                resolve();
            } else {
                document.addEventListener('DOMContentLoaded', resolve);
            }
        });
    }

    /**
     * Initialize basic services
     */
    initializeServices() {
        // Inizializza un semplice servizio di notifiche
        window.NotifyService = {
            info: (message) => console.log('ℹ️', message),
            success: (message) => console.log('✅', message),
            warning: (message) => console.log('⚠️', message),
            error: (message) => console.error('❌', message)
        };

        console.log('✅ Basic services initialized');
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
    initializeTabs() {
        // Per ora registriamo solo handler vuoti
        const tabs = ['import', 'process', 'formatter'];

        tabs.forEach(tabName => {
            if (window.BattlesnakeTabManager) {
                window.BattlesnakeTabManager.registerTab(tabName, {
                    onActivate: () => console.log(`${tabName} tab activated`),
                    onDeactivate: () => console.log(`${tabName} tab deactivated`)
                });
            }
        });

        console.log('✅ Tabs registered');
    }

    /**
     * Show loading overlay
     */
    showLoading() {
        const overlay = document.getElementById('loadingOverlay');
        if (overlay) {
            overlay.classList.remove('d-none');
        }
    }

    /**
     * Hide loading overlay
     */
    hideLoading() {
        const overlay = document.getElementById('loadingOverlay');
        if (overlay) {
            overlay.classList.add('d-none');
        }
    }

    /**
     * Update status message
     */
    updateStatus(message, type = 'info') {
        const statusElement = document.getElementById('statusMessage');
        if (statusElement) {
            statusElement.textContent = message;
            statusElement.className = `text-center text-${type}`;
        }
    }
}

// Create and initialize app instance
const app = new BattlesnakeApp();

// Start initialization
app.init().catch(error => {
    console.error('Fatal error during initialization:', error);
});