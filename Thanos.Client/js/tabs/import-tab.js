class ImportTabManager {
    constructor(inputId, notifyService) {
        this.notify = notifyService;
        this.input = document.getElementById(inputId);
    }

    importBoards() {
        if (!this.input.value.trim()) {
            this.notify.error('Inserisci del contenuto');
            return;
        }

        const grids = this.input.textContent.split('\n\n').filter(g => g.trim());
        this.notify.success(`${grids.length} griglie importate`);
    }

    clearInput() {
        this.input.textContent = '';
        this.notify.success('Campo pulito');
    }

    loadExample() {
        this.input.textContent = `  👽 💲 💲 ⬛ ⬛
                                    ⬛ ⬛ ⬛ ⬛ ⬛
                                    ⬛ ⬛ 😈 ⛔ ⬛
                                    💀 ⬛ ⬛ ⛔ ⛔
                                    ⬛ ⬛ ⬛ ⬛ ⬛
                                    ⬛ ⬛ 😈 ⛔ ⛔`;
        this.notify.success('Esempio caricato');
    }

    copyIcon(icon) {
        navigator.clipboard.writeText(icon);
        this.notify.success(`Icona ${icon} copiata`);
    }
}