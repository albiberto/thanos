// Battlesnake Board Converter - Tab Manager Semplificato per Bootstrap

class TabManager {
    constructor(currentTab, tabs, buttonsDiv, contentDiv) {
        this.currentTab = currentTab;
        this.tabs = tabs; // array di nomi tab, es: ['import', 'process', 'formatter']
        this.buttonsDiv = buttonsDiv; // elemento div che contiene i bottoni
        this.contentDiv = contentDiv; // elemento div dove iniettare l'html
        this.init();
    }

    init() {
        this.tabs.forEach(tabName => {
            const button = this.buttonsDiv.querySelector(`#${tabName}`);
            if (button) {
                button.addEventListener('click', () => this.switchTab(tabName));
            }
        });
        this.switchTab(this.currentTab);
    }

    async switchTab(tabName) {

        this.currentTab = tabName;
        const fileName = `${tabName}-tab.html`;

        const response = await fetch(fileName);
        this.contentDiv.innerHTML = response.ok
            ? await response.text()
            : `<div class='alert alert-danger'>Errore caricamento ${fileName}</div>`;
    }
}