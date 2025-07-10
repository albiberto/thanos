// Battlesnake Board Converter - Tab Manager Semplificato per Bootstrap

class TabManager {
    constructor(contentDiv) {
        this.contentDiv = contentDiv;
    }

    async switchTab(tabName) {
        const fileName = `${tabName}-tab.html`;

        const response = await fetch(fileName);
        this.contentDiv.innerHTML = response.ok
            ? await response.text()
            : `<div class='alert alert-danger'>Errore caricamento ${fileName}</div>`;
    }
}