/**
 * index.js - Entry point principale dell'applicazione
 * Punto unico di bootstrap come richiesto
 */

import { BattlesnakeApp } from './app/BattlesnakeApp.js';

// Istanza globale dell'app
let app = null;

/**
 * Inizializza l'applicazione quando il DOM è pronto
 */
document.addEventListener('DOMContentLoaded', async () => {
    try {
        // Crea e inizializza l'app
        app = new BattlesnakeApp();
        await app.initialize();

        // Esponi metodi utility globalmente
        window.battlesnakeApp = {
            exportData: () => app.exportData(),
            clearAllData: () => app.clearAllData(),
            updateBadge: () => app.updateGridsBadge()
        };

        // Setup keyboard shortcuts
        setupKeyboardShortcuts();

        // Setup global error handlers
        setupErrorHandlers();

        console.log('✅ Applicazione pronta');

    } catch (error) {
        console.error('❌ Errore fatale:', error);
        showFatalError(error);
    }
});

/**
 * Setup keyboard shortcuts
 */
function setupKeyboardShortcuts() {
    document.addEventListener('keydown', (e) => {
        // Ctrl+1/2/3 per cambiare tab
        if (e.ctrlKey && !e.shiftKey && !e.altKey) {
            switch (e.key) {
                case '1':
                    e.preventDefault();
                    app?.services.tabManager?.switchTab('import');
                    break;
                case '2':
                    e.preventDefault();
                    app?.services.tabManager?.switchTab('process');
                    break;
                case '3':
                    e.preventDefault();
                    app?.services.tabManager?.switchTab('formatter');
                    break;
            }
        }
    });
}

/**
 * Setup error handlers globali
 */
function setupErrorHandlers() {
    window.addEventListener('error', (event) => {
        console.error('Errore globale:', event.error);
        app?.services.notify?.error('Si è verificato un errore imprevisto');
    });

    window.addEventListener('unhandledrejection', (event) => {
        console.error('Promise rejection:', event.reason);
        app?.services.notify?.error('Errore asincrono nell\'applicazione');
    });
}

/**
 * Mostra errore fatale
 */
function showFatalError(error) {
    const container = document.getElementById('mainTabContent');
    if (container) {
        container.innerHTML = `
            <div class="alert alert-danger m-4" role="alert">
                <h4 class="alert-heading">❌ Errore Fatale</h4>
                <p>Impossibile avviare l'applicazione.</p>
                <p class="mb-0"><strong>Dettagli:</strong> ${error.message}</p>
                <hr>
                <div class="d-flex gap-2">
                    <button class="btn btn-danger" onclick="location.reload()">
                        🔄 Ricarica
                    </button>
                    <button class="btn btn-outline-danger" onclick="localStorage.clear(); location.reload()">
                        🗑️ Reset Completo
                    </button>
                </div>
            </div>
        `;
    }
}

/**
 * Debug helper (sviluppo)
 */
window.debugApp = () => {
    console.clear();
    console.log('🐍 === DEBUG INFO ===');
    console.log('App instance:', app);
    console.log('Services:', app?.services);
    console.log('Managers:', app?.managers);

    // Storage info
    const storageInfo = {};
    for (let key in localStorage) {
        if (localStorage.hasOwnProperty(key)) {
            storageInfo[key] = localStorage[key].length;
        }
    }
    console.log('Storage:', storageInfo);
};
