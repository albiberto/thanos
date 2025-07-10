class TabManager {
    constructor(contentId, notifyService) {
        this.content = document.getElementById(contentId);
        this.notify = notifyService;
        this.callbacks = {}; // Mancava questa proprietà!
        this.currentTab = null;
    }

    // Registra callback per quando un tab viene caricato
    registerTabCallback(tabName, callback) {
        this.callbacks[tabName] = callback;
    }

    // Carica e switcha tra i tab
    async switchTab(name) {
        try {
            // Mostra un loading se necessario
            this.content.innerHTML = '<div class="d-flex justify-content-center align-items-center h-100"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Loading...</span></div></div>';

            const response = await fetch(`${name}-tab.html`);

            if (!response.ok) {
                throw new Error(`Failed to load ${name}-tab.html: ${response.status}`);
            }

            this.content.innerHTML = await response.text();
            this.currentTab = name;

            // Esegui il callback se registrato
            if (this.callbacks[name]) {
                this.callbacks[name]();
            }

            this.notify.success(`Tab ${name} caricato`);
        } catch (error) {
            console.error(`Error loading tab ${name}:`, error);
            this.notify.error(`Errore caricamento ${name}`);
        }
    }

    // Ottieni il tab corrente
    getCurrentTab() {
        return this.currentTab;
    }

    // Metodo per aggiornare il tab corrente senza ricaricarlo
    refreshCurrentTab() {
        if (this.currentTab) {
            this.switchTab(this.currentTab);
        }
    }
}