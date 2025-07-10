/**
 * BattleSnake Board Converter - State Management
 * Gestione centralizzata dello stato dell'applicazione
 */

class StateManager {
    constructor() {
        this.state = {
            app: {
                version: '2.0.0',
                initialized: false,
                currentTab: 'import'
            },
            boards: {
                imported: [],
                processed: [],
                expectedValues: []
            },
            processors: {
                list: [],
                current: null,
                stats: {
                    total: 0,
                    pending: 0,
                    processing: 0,
                    completed: 0,
                    error: 0
                }
            },
            ui: {
                loading: false,
                activeModals: [],
                filters: {
                    status: 'all',
                    type: 'all',
                    search: ''
                }
            },
            settings: {
                autoRefresh: true,
                refreshInterval: 5000,
                maxRetries: 3,
                notifications: true
            }
        };

        this.subscribers = new Map();
        this.storageKey = 'battlesnake_state';
        this.debounceTimeout = null;
    }

    /**
     * Inizializza lo state manager
     */
    init() {
        this.loadFromStorage();
        this.state.app.initialized = true;
        this.notify('app.initialized', this.state.app);
        console.log('✅ StateManager initialized');
    }

    /**
     * Ottieni stato completo o parte specifica
     */
    getState(path = null) {
        if (!path) return { ...this.state };

        const keys = path.split('.');
        let value = this.state;

        for (const key of keys) {
            if (value && typeof value === 'object' && key in value) {
                value = value[key];
            } else {
                return undefined;
            }
        }

        return value;
    }

    /**
     * Aggiorna stato
     */
    setState(path, value, notify = true) {
        const keys = path.split('.');
        const lastKey = keys.pop();
        let target = this.state;

        // Naviga fino al parent object
        for (const key of keys) {
            if (!(key in target)) {
                target[key] = {};
            }
            target = target[key];
        }

        // Salva valore precedente per confronto
        const oldValue = target[lastKey];
        target[lastKey] = value;

        // Notifica subscribers se il valore è cambiato
        if (notify && oldValue !== value) {
            this.notify(path, value, oldValue);
        }

        // Salva in storage (debounced)
        this.debouncedSave();

        return this;
    }

    /**
     * Merge oggetto nello stato
     */
    mergeState(path, updates, notify = true) {
        const currentValue = this.getState(path);

        if (currentValue && typeof currentValue === 'object') {
            const mergedValue = { ...currentValue, ...updates };
            this.setState(path, mergedValue, notify);
        } else {
            this.setState(path, updates, notify);
        }

        return this;
    }

    /**
     * Aggiungi elemento ad array
     */
    pushToArray(path, item, maxItems = null) {
        const currentArray = this.getState(path) || [];

        if (!Array.isArray(currentArray)) {
            console.warn(`State at ${path} is not an array`);
            return this;
        }

        const newArray = [...currentArray, item];

        // Limita dimensione array se specificato
        if (maxItems && newArray.length > maxItems) {
            newArray.splice(0, newArray.length - maxItems);
        }

        this.setState(path, newArray);
        return this;
    }

    /**
     * Rimuovi elemento da array
     */
    removeFromArray(path, predicate) {
        const currentArray = this.getState(path) || [];

        if (!Array.isArray(currentArray)) {
            console.warn(`State at ${path} is not an array`);
            return this;
        }

        const newArray = currentArray.filter(item => !predicate(item));
        this.setState(path, newArray);
        return this;
    }

    /**
     * Aggiorna elemento in array
     */
    updateInArray(path, predicate, updates) {
        const currentArray = this.getState(path) || [];

        if (!Array.isArray(currentArray)) {
            console.warn(`State at ${path} is not an array`);
            return this;
        }

        const newArray = currentArray.map(item => {
            if (predicate(item)) {
                return typeof updates === 'function' ? updates(item) : { ...item, ...updates };
            }
            return item;
        });

        this.setState(path, newArray);
        return this;
    }

    /**
     * Sottoscrivi cambiamenti di stato
     */
    subscribe(path, callback, immediate = false) {
        if (!this.subscribers.has(path)) {
            this.subscribers.set(path, new Set());
        }

        this.subscribers.get(path).add(callback);

        // Chiama immediatamente se richiesto
        if (immediate) {
            const currentValue = this.getState(path);
            callback(currentValue, undefined, path);
        }

        // Restituisci funzione di unsubscribe
        return () => {
            const pathSubscribers = this.subscribers.get(path);
            if (pathSubscribers) {
                pathSubscribers.delete(callback);
                if (pathSubscribers.size === 0) {
                    this.subscribers.delete(path);
                }
            }
        };
    }

    /**
     * Notifica subscribers
     */
    notify(path, newValue, oldValue) {
        // Notifica exact path
        this.notifyPath(path, newValue, oldValue);

        // Notifica parent paths
        const pathParts = path.split('.');
        for (let i = pathParts.length - 1; i > 0; i--) {
            const parentPath = pathParts.slice(0, i).join('.');
            this.notifyPath(parentPath, this.getState(parentPath), undefined);
        }

        // Notifica global listeners
        this.notifyPath('*', this.state, undefined);
    }

    /**
     * Notifica path specifico
     */
    notifyPath(path, newValue, oldValue) {
        const subscribers = this.subscribers.get(path);
        if (subscribers) {
            subscribers.forEach(callback => {
                try {
                    callback(newValue, oldValue, path);
                } catch (error) {
                    console.error(`Error in state subscriber for ${path}:`, error);
                }
            });
        }
    }

    /**
     * Reset stato
     */
    reset() {
        const defaultState = {
            app: { ...this.state.app, initialized: true },
            boards: { imported: [], processed: [], expectedValues: [] },
            processors: {
                list: [],
                current: null,
                stats: { total: 0, pending: 0, processing: 0, completed: 0, error: 0 }
            },
            ui: {
                loading: false,
                activeModals: [],
                filters: { status: 'all', type: 'all', search: '' }
            },
            settings: { ...this.state.settings }
        };

        this.state = defaultState;
        this.notify('*', this.state, undefined);
        this.saveToStorage();
    }

    /**
     * Salva stato in localStorage
     */
    saveToStorage() {
        try {
            const serializedState = JSON.stringify({
                ...this.state,
                ui: { ...this.state.ui, loading: false, activeModals: [] } // Non salvare UI temporaneo
            });
            localStorage.setItem(this.storageKey, serializedState);
        } catch (error) {
            console.error('Failed to save state to storage:', error);
        }
    }

    /**
     * Carica stato da localStorage
     */
    loadFromStorage() {
        try {
            const serializedState = localStorage.getItem(this.storageKey);
            if (serializedState) {
                const savedState = JSON.parse(serializedState);

                // Merge con stato default per compatibilità
                this.state = {
                    ...this.state,
                    ...savedState,
                    app: { ...this.state.app, ...savedState.app, initialized: false },
                    ui: { ...this.state.ui } // Reset UI state
                };
            }
        } catch (error) {
            console.error('Failed to load state from storage:', error);
        }
    }

    /**
     * Salvataggio debounced per performance
     */
    debouncedSave() {
        clearTimeout(this.debounceTimeout);
        this.debounceTimeout = setTimeout(() => {
            this.saveToStorage();
        }, 500);
    }

    /**
     * Ottieni statistiche stato
     */
    getStats() {
        return {
            totalBoards: this.state.boards.imported.length,
            processedBoards: this.state.boards.processed.length,
            totalProcessors: this.state.processors.list.length,
            activeSubscribers: Array.from(this.subscribers.keys()).length,
            stateSize: JSON.stringify(this.state).length
        };
    }

    /**
     * Metodi di comodo per operazioni comuni
     */

    // Boards
    setBoards(boards) {
        this.setState('boards.imported', boards);
        this.setState('boards.expectedValues', new Array(boards.length).fill(13));
    }

    addBoard(board) {
        this.pushToArray('boards.imported', board);
        this.pushToArray('boards.expectedValues', 13);
    }

    setBoardExpectedValue(index, value) {
        const expectedValues = [...this.getState('boards.expectedValues')];
        if (index >= 0 && index < expectedValues.length) {
            expectedValues[index] = value;
            this.setState('boards.expectedValues', expectedValues);
        }
    }

    // UI
    setLoading(loading) {
        this.setState('ui.loading', loading);
    }

    addModal(modalId) {
        const modals = this.getState('ui.activeModals') || [];
        if (!modals.includes(modalId)) {
            this.pushToArray('ui.activeModals', modalId);
        }
    }

    removeModal(modalId) {
        this.removeFromArray('ui.activeModals', modal => modal === modalId);
    }

    // Filters
    setFilter(filterName, value) {
        this.setState(`ui.filters.${filterName}`, value);
    }

    // Current tab
    setCurrentTab(tab) {
        this.setState('app.currentTab', tab);
    }

    // Processors
    addProcessor(processor) {
        this.pushToArray('processors.list', processor);
        this.updateProcessorStats();
    }

    updateProcessor(id, updates) {
        this.updateInArray('processors.list', p => p.id === id, updates);
        this.updateProcessorStats();
    }

    removeProcessor(id) {
        this.removeFromArray('processors.list', p => p.id === id);
        this.updateProcessorStats();
    }

    updateProcessorStats() {
        const processors = this.getState('processors.list') || [];
        const stats = {
            total: processors.length,
            pending: processors.filter(p => p.status === 'pending').length,
            processing: processors.filter(p => p.status === 'processing').length,
            completed: processors.filter(p => p.status === 'completed').length,
            error: processors.filter(p => p.status === 'error').length
        };
        this.setState('processors.stats', stats);
    }
}

// === GLOBAL INSTANCE ===
const stateManager = new StateManager();

// Auto-initialize
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        stateManager.init();
    });
} else {
    stateManager.init();
}

// === EXPORTS ===
window.StateManager = stateManager;
window.BattleSnakeState = stateManager;