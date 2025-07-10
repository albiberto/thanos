class TabManager {
    constructor(contentId, notifyService) {
        this.content = document.getElementById(contentId);
        this.notify = notifyService;
        this.callbacks = {};
    }

    async switchTab(name) {
        try {
            const response = await fetch(`${name}-tab.html`);
            this.content.innerHTML = await response.text();

            if (this.callbacks[name]) {
                this.callbacks[name]();
            }

            this.notify.success(`Tab ${name} caricato`);
        } catch (error) {
            this.notify.error(`Errore caricamento ${name}`);
        }
    }
}