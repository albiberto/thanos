/**
 * TabManager.js - Componente per la gestione dei tab
 */

export class TabManager {
    constructor(contentElementId, notificationService) {
        this.contentElementId = contentElementId;
        this.notify = notificationService;
        this.currentTab = null;
        this.callbacks = {};

        this.setupTabListeners();
    }

    /**
     * Setup event listeners per i tab
     */
    setupTabListeners() {
        const tabs = document.querySelectorAll('[data-tab]');
        tabs.forEach(tab => {
            tab.addEventListener('click', (e) => {
                e.preventDefault();
                const tabName = tab.getAttribute('data-tab');
                this.switchTab(tabName);
            });
        });
    }

    /**
     * Registra callback per un tab
     */
    registerCallback(tabName, callback) {
        this.callbacks[tabName] = callback;
    }

    /**
     * Cambia tab
     */
    async switchTab(name) {
        try {
            // Aggiorna UI dei tab
            this.updateTabUI(name);

            // Carica contenuto
            const content = await this.loadTabContent(name);
            const container = document.getElementById(this.contentElementId);

            if (container) {
                container.innerHTML = content;
                this.currentTab = name;

                // Esegui callback
                if (this.callbacks[name]) {
                    setTimeout(() => {
                        this.callbacks[name]();
                    }, 100);
                }

                this.notify.success(`Tab ${name} caricato`);
            }

        } catch (error) {
            console.error(`Errore caricamento tab ${name}:`, error);
            this.notify.error(`Errore caricamento ${name}`);
        }
    }

    /**
     * Aggiorna UI dei tab
     */
    updateTabUI(activeName) {
        const tabs = document.querySelectorAll('[data-tab]');
        tabs.forEach(tab => {
            const tabName = tab.getAttribute('data-tab');
            if (tabName === activeName) {
                tab.classList.add('active');
                tab.setAttribute('aria-selected', 'true');
            } else {
                tab.classList.remove('active');
                tab.setAttribute('aria-selected', 'false');
            }
        });
    }

    /**
     * Associa gli event listener agli elementi del tab appena caricato
     */
    bindTabContentEvents(tabName) {
        const tabContent = document.querySelector(`#${tabName}-content`);
        if (!tabContent) return;
        // Esempio: associa listener ai bottoni con data-action
        tabContent.querySelectorAll('[data-action]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const action = btn.getAttribute('data-action');
                // Gestione azioni specifiche
                this.handleTabAction(tabName, action, e);
            });
        });
        // Puoi aggiungere qui altri binding unobtrusive
    }

    /**
     * Gestisce le azioni dei tab
     */
    handleTabAction(tabName, action, event) {
        // Implementa qui la logica per le varie azioni
        console.log(`Azione '${action}' nel tab '${tabName}'`);
        // Esempio: if (action === 'import') { ... }
    }

    /**
     * Carica contenuto del tab
     */
    async loadTabContent(name) {
        const response = await fetch(`${name}-tab.html`);
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        const html = await response.text();
        // Inserisci il contenuto nel DOM
        const tabContent = document.querySelector(`#${name}-content`);
        if (tabContent) {
            tabContent.innerHTML = html;
            // Binding unobtrusive dopo inserimento
            this.bindTabContentEvents(name);
        }
        return html;
    }

    /**
     * Ottieni tab corrente
     */
    getCurrentTab() {
        return this.currentTab;
    }
}