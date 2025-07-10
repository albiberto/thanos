// Tab Loader - Carica il contenuto HTML per ogni tab
class TabLoader {
    constructor() {
        this.loadedTabs = new Set();
        this.initializeTabLoading();
    }

    /**
     * Inizializza il caricamento dei tab
     */
    initializeTabLoading() {
        // Carica il primo tab attivo immediatamente
        this.loadTabContent('import-tab', 'tabs/import-tab.html');

        // Ascolta i cambi di tab per caricare i contenuti al bisogno
        const tabButtons = document.querySelectorAll('[data-bs-toggle="tab"]');
        tabButtons.forEach(button => {
            button.addEventListener('shown.bs.tab', (event) => {
                const targetTabId = event.target.getAttribute('data-bs-target').substring(1);
                this.loadTabContentIfNeeded(targetTabId);
            });
        });
    }

    /**
     * Carica il contenuto di un tab se non è già stato caricato
     * @param {string} tabId - ID del tab
     */
    loadTabContentIfNeeded(tabId) {
        if (this.loadedTabs.has(tabId)) {
            return;
        }

        const tabToFileMap = {
            'import-tab': 'tabs/import-tab.html',
            'process-tab': 'tabs/process-tab.html',
            'formatter-tab': 'tabs/formatter-tab.html'
        };

        const filePath = tabToFileMap[tabId];
        if (filePath) {
            this.loadTabContent(tabId, filePath);
        }
    }

    /**
     * Carica il contenuto HTML di un tab
     * @param {string} tabId - ID del tab
     * @param {string} filePath - Percorso del file HTML
     */
    async loadTabContent(tabId, filePath) {
        if (this.loadedTabs.has(tabId)) {
            return;
        }

        const tabElement = document.getElementById(tabId);
        if (!tabElement) {
            console.error(`Tab element with ID "${tabId}" not found`);
            return;
        }

        try {
            // Mostra un loading spinner
            this.showLoadingSpinner(tabElement);

            const response = await fetch(filePath);
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const htmlContent = await response.text();

            // Inserisce il contenuto HTML
            tabElement.innerHTML = htmlContent;

            // Marca il tab come caricato
            this.loadedTabs.add(tabId);

            // Emette un evento personalizzato per notificare che il tab è stato caricato
            const loadEvent = new CustomEvent('tabContentLoaded', {
                detail: { tabId, filePath }
            });
            document.dispatchEvent(loadEvent);

            console.log(`✅ Tab "${tabId}" loaded successfully from "${filePath}"`);

        } catch (error) {
            console.error(`❌ Error loading tab "${tabId}" from "${filePath}":`, error);
            this.showErrorMessage(tabElement, error.message);
        }
    }

    /**
     * Mostra uno spinner di caricamento nel tab
     * @param {HTMLElement} tabElement - Elemento del tab
     */
    showLoadingSpinner(tabElement) {
        tabElement.innerHTML = `
            <div class="d-flex flex-column justify-content-center align-items-center h-100">
                <div class="spinner-border text-success mb-3" role="status" style="width: 3rem; height: 3rem;">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <h5 class="text-success">Loading tab content...</h5>
                <p class="text-muted">Please wait while we load the interface</p>
            </div>
        `;
    }

    /**
     * Mostra un messaggio di errore nel tab
     * @param {HTMLElement} tabElement - Elemento del tab
     * @param {string} errorMessage - Messaggio di errore
     */
    showErrorMessage(tabElement, errorMessage) {
        tabElement.innerHTML = `
            <div class="d-flex flex-column justify-content-center align-items-center h-100">
                <div class="alert alert-danger text-center" role="alert">
                    <h4 class="alert-heading">❌ Loading Error</h4>
                    <p class="mb-0">Failed to load tab content: ${errorMessage}</p>
                    <hr>
                    <p class="mb-0">
                        <button class="btn btn-outline-danger" onclick="location.reload()">
                            🔄 Reload Page
                        </button>
                    </p>
                </div>
            </div>
        `;
    }

    /**
     * Ricarica il contenuto di un tab specifico
     * @param {string} tabId - ID del tab da ricaricare
     */
    reloadTab(tabId) {
        this.loadedTabs.delete(tabId);
        this.loadTabContentIfNeeded(tabId);
    }

    /**
     * Ricarica tutti i tab
     */
    reloadAllTabs() {
        this.loadedTabs.clear();

        // Ricarica il tab attualmente attivo
        const activeTab = document.querySelector('.tab-pane.active');
        if (activeTab) {
            this.loadTabContentIfNeeded(activeTab.id);
        }
    }

    /**
     * Verifica se un tab è stato caricato
     * @param {string} tabId - ID del tab
     * @returns {boolean}
     */
    isTabLoaded(tabId) {
        return this.loadedTabs.has(tabId);
    }

    /**
     * Ottiene la lista dei tab caricati
     * @returns {Array<string>}
     */
    getLoadedTabs() {
        return Array.from(this.loadedTabs);
    }
}

// Inizializza il tab loader quando il DOM è pronto
document.addEventListener('DOMContentLoaded', () => {
    window.tabLoader = new TabLoader();

    // Debug: Espone metodi utili globalmente in development
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
        window.reloadTab = (tabId) => window.tabLoader.reloadTab(tabId);
        window.reloadAllTabs = () => window.tabLoader.reloadAllTabs();
        window.getLoadedTabs = () => window.tabLoader.getLoadedTabs();
    }
});

// Gestisce gli errori di rete globalmente
window.addEventListener('online', () => {
    console.log('🌐 Connection restored. You may want to reload any failed tabs.');
});

window.addEventListener('offline', () => {
    console.warn('🚫 Connection lost. Tab loading may fail.');
});