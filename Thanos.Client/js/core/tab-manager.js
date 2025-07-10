class TabManager {
    constructor(contentId, notifyService) {
        this.content = document.getElementById(contentId);
        this.notify = notifyService;
        this.onTabLoaded = {}; // Callback per quando il tab è caricato
    }

    // Registra callback per quando un tab viene caricato
    registerTabCallback(tabName, callback) {
        this.onTabLoaded[tabName] = callback;
    }

    // Nel TabManager, modifica il metodo switchTab:
    async switchTab(name) {
        try {
            const response = await fetch(`${name}-tab.html`);
            this.content.innerHTML = await response.text();

            if (this.callbacks[name]) {
                this.callbacks[name]();
            }

            // Se è il tab import, inizializza l'ImportTabManager
            if (name === 'import' && window.snake?.importTabManager) {
                window.snake.importTabManager.initialize();
            }

            this.notify.success(`Tab ${name} caricato`);
        } catch (error) {
            this.notify.error(`Errore caricamento ${name}`);
        }
    }
}