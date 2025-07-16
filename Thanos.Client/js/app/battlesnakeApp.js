/**
 * BattlesnakeApp.js - Classe principale dell'applicazione
 */

import { NotificationService } from '../services/NotificationService.js';
import { TabManager } from '../components/TabManager.js';
import { ImportManager } from '../managers/ImportManager.js';
import { ProcessManager } from '../managers/ProcessManager.js';
import { FormatterManager } from '../managers/FormatterManager.js';

export class BattlesnakeApp {
    constructor() {
        this.services = {};
        this.managers = {};
    }

    /**
     * Inizializza l'applicazione
     */
    async initialize() {
        try {
            console.log('🚀 Inizializzazione BattleSnake Board Converter...');

            // Inizializza servizi
            this.initializeServices();

            // Inizializza managers
            this.initializeManagers();

            // Setup tab callbacks
            this.setupTabCallbacks();

            // Carica tab iniziale
            await this.services.tabManager.switchTab('import');

            console.log('✅ Applicazione inizializzata con successo');

        } catch (error) {
            console.error('❌ Errore inizializzazione:', error);
            this.showInitError(error);
        }
    }

    /**
     * Inizializza i servizi
     */
    initializeServices() {
        // Notification Service
        this.services.notify = new NotificationService('statusToast');

        // Tab Manager
        this.services.tabManager = new TabManager('mainTabContent', this.services.notify);
    }

    /**
     * Inizializza i managers
     */
    initializeManagers() {
        // Import Manager
        this.managers.import = new ImportManager(
            'boardInput',
            this.services.notify,
            this.services.tabManager
        );

        // Process Manager  
        this.managers.process = new ProcessManager(
            this.services.notify
        );

        // Formatter Manager
        this.managers.formatter = new FormatterManager(
            'jsonListInput',
            this.services.notify
        );

        // Esponi globalmente per event handlers
        window.importManager = this.managers.import;
        window.processManager = this.managers.process;
        window.formatterManager = this.managers.formatter;
    }

    /**
     * Setup callbacks per i tab
     */
    setupTabCallbacks() {
        this.services.tabManager.registerCallback('import', () => {
            console.log('📥 Tab Import attivato');
            this.updateGridsBadge();
        });

        this.services.tabManager.registerCallback('process', () => {
            console.log('⚙️ Tab Process attivato');
            this.managers.process.initialize();
            this.updateGridsBadge();
        });

        this.services.tabManager.registerCallback('formatter', () => {
            console.log('📝 Tab Formatter attivato');
            this.managers.formatter.initialize();
        });
    }

    /**
     * Aggiorna badge numero griglie
     */
    updateGridsBadge() {
        const badge = document.getElementById('gridsBadge');
        if (!badge) return;

        try {
            const data = localStorage.getItem('battlesnake_grids');
            if (data) {
                const parsed = JSON.parse(data);
                const count = parsed.grids?.length || 0;

                if (count > 0) {
                    badge.textContent = count;
                    badge.style.display = 'inline';
                } else {
                    badge.style.display = 'none';
                }
            }
        } catch (error) {
            console.warn('Errore aggiornamento badge:', error);
        }
    }

    /**
     * Mostra errore di inizializzazione
     */
    showInitError(error) {
        const container = document.getElementById('mainTabContent');
        if (container) {
            container.innerHTML = `
                <div class="alert alert-danger" role="alert">
                    <h4 class="alert-heading">❌ Errore di inizializzazione</h4>
                    <p>Si è verificato un errore durante l'avvio dell'applicazione.</p>
                    <p class="mb-0"><strong>Errore:</strong> ${error.message}</p>
                    <hr>
                    <button class="btn btn-outline-danger" onclick="location.reload()">
                        🔄 Ricarica Pagina
                    </button>
                </div>
            `;
        }
    }

    /**
     * Metodi utility esposti
     */
    exportData() {
        try {
            const data = {};
            for (let key in localStorage) {
                if (localStorage.hasOwnProperty(key)) {
                    data[key] = localStorage[key];
                }
            }

            const blob = new Blob([JSON.stringify(data, null, 2)], {
                type: 'application/json'
            });
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');

            link.href = url;
            link.download = `battlesnake_backup_${new Date().toISOString().split('T')[0]}.json`;
            link.click();

            URL.revokeObjectURL(url);
            this.services.notify.success('Backup esportato');

        } catch (error) {
            this.services.notify.error('Errore esportazione');
        }
    }

    clearAllData() {
        if (confirm('Cancellare TUTTI i dati?')) {
            localStorage.clear();
            location.reload();
        }
    }
}