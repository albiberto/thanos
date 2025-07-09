// Prosser Board JavaScript
class ProsserBoard {
    constructor() {
        this.processors = [];
        this.filteredProcessors = [];
        this.currentProcessor = null;
        this.refreshInterval = null;
        this.autoRefreshEnabled = true;
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.loadProcessors();
        this.updateStats();
        this.startAutoRefresh();
    }

    setupEventListeners() {
        // Header controls
        document.getElementById('addProcessorBtn').addEventListener('click', () => this.showAddProcessorModal());
        document.getElementById('refreshBtn').addEventListener('click', () => this.refreshProcessors());

        // Filters
        document.getElementById('statusFilter').addEventListener('change', () => this.applyFilters());
        document.getElementById('typeFilter').addEventListener('change', () => this.applyFilters());
        document.getElementById('searchFilter').addEventListener('input', () => this.applyFilters());

        // Modals
        document.getElementById('closeModal').addEventListener('click', () => this.closeModal('processorModal'));
        document.getElementById('cancelBtn').addEventListener('click', () => this.closeModal('processorModal'));
        document.getElementById('saveBtn').addEventListener('click', () => this.saveProcessor());
        document.getElementById('closeDetailsModal').addEventListener('click', () => this.closeModal('detailsModal'));
        document.getElementById('closeDetailsBtn').addEventListener('click', () => this.closeModal('detailsModal'));

        // Form
        document.getElementById('processorForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveProcessor();
        });

        // Close modal on outside click
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal')) {
                this.closeModal(e.target.id);
            }
        });
    }

    loadProcessors() {
        this.showLoading();

        // Simula caricamento dati
        setTimeout(() => {
            this.processors = this.generateMockProcessors();
            this.filteredProcessors = [...this.processors];
            this.renderProcessors();
            this.updateStats();
            this.hideLoading();
        }, 1000);
    }

    generateMockProcessors() {
        const types = ['data', 'image', 'text', 'queue'];
        const statuses = ['active', 'idle', 'processing', 'error'];
        const priorities = ['low', 'medium', 'high'];
        const processors = [];

        const processorNames = [
            'Data Aggregator', 'Image Optimizer', 'Text Analyzer', 'Queue Manager',
            'File Processor', 'Media Converter', 'Log Parser', 'Cache Handler',
            'Backup Service', 'Index Builder', 'Report Generator', 'Sync Engine'
        ];

        const descriptions = [
            'Elabora e aggrega dati da multiple sorgenti',
            'Ottimizza e ridimensiona immagini automaticamente',
            'Analizza contenuti testuali e estrae metadati',
            'Gestisce code di elaborazione distribuita',
            'Processa file caricati dagli utenti',
            'Converte file multimediali in diversi formati',
            'Analizza log di sistema e genera report',
            'Gestisce cache distribuita per performance',
            'Esegue backup automatici del sistema',
            'Costruisce indici per ricerca full-text',
            'Genera report periodici automatizzati',
            'Sincronizza dati tra sistemi esterni'
        ];

        for (let i = 0; i < 12; i++) {
            const type = types[i % types.length];
            const status = statuses[Math.floor(Math.random() * statuses.length)];
            const priority = priorities[Math.floor(Math.random() * priorities.length)];

            processors.push({
                id: i + 1,
                name: processorNames[i],
                type: type,
                status: status,
                description: descriptions[i],
                priority: priority,
                autoStart: Math.random() > 0.3, // 70% chance di auto-start
                metrics: {
                    cpu: status === 'active' || status === 'processing' ?
                        Math.floor(Math.random() * 80) + 20 : Math.floor(Math.random() * 10),
                    memory: status === 'active' || status === 'processing' ?
                        Math.floor(Math.random() * 60) + 30 : Math.floor(Math.random() * 20),
                    tasks: status === 'active' || status === 'processing' ?
                        Math.floor(Math.random() * 45) + 5 : 0
                },
                created: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000),
                lastActive: new Date(Date.now() - Math.random() * 24 * 60 * 60 * 1000),
                version: `v${Math.floor(Math.random() * 3) + 1}.${Math.floor(Math.random() * 10)}.${Math.floor(Math.random() * 20)}`,
                uptime: Math.floor(Math.random() * 1000000) // millisecondi
            });
        }

        return processors;
    }

    renderProcessors() {
        const grid = document.getElementById('processorsGrid');

        if (this.filteredProcessors.length === 0) {
            grid.innerHTML = `
                <div class="empty-state">
                    <h3>Nessun processore trovato</h3>
                    <p>Prova a modificare i filtri di ricerca o aggiungi un nuovo processore</p>
                    <button class="btn btn-primary" onclick="prosserBoard.showAddProcessorModal()">
                        Aggiungi Processore
                    </button>
                </div>
            `;
            return;
        }

        grid.innerHTML = this.filteredProcessors.map(processor => this.createProcessorCard(processor)).join('');
    }

    createProcessorCard(processor) {
        const typeLabels = {
            data: 'Data Processor',
            image: 'Image Processor',
            text: 'Text Processor',
            queue: 'Queue Processor'
        };

        const priorityLabels = {
            low: 'Bassa',
            medium: 'Media',
            high: 'Alta'
        };

        const statusLabels = {
            active: 'Attivo',
            idle: 'Inattivo',
            processing: 'In Elaborazione',
            error: 'Errore'
        };

        return `
            <div class="processor-card status-${processor.status}">
                <div class="processor-header">
                    <div class="processor-info">
                        <h3>${processor.name}</h3>
                        <div class="processor-type">${typeLabels[processor.type]}</div>
                    </div>
                    <div class="processor-status ${processor.status}">
                        ${statusLabels[processor.status]}
                    </div>
                </div>
                
                <div class="processor-description">
                    ${processor.description}
                </div>
                
                <div class="processor-metrics">
                    <div class="metric">
                        <div class="metric-value">${processor.metrics.cpu}%</div>
                        <div class="metric-label">CPU</div>
                    </div>
                    <div class="metric">
                        <div class="metric-value">${processor.metrics.memory}%</div>
                        <div class="metric-label">Memoria</div>
                    </div>
                    <div class="metric">
                        <div class="metric-value">${processor.metrics.tasks}</div>
                        <div class="metric-label">Tasks</div>
                    </div>
                </div>
                
                <div class="processor-actions">
                    ${this.getProcessorActions(processor)}
                </div>
            </div>
        `;
    }

    getProcessorActions(processor) {
        const actions = [];

        if (processor.status === 'idle') {
            actions.push(`<button class="btn btn-success" onclick="prosserBoard.startProcessor(${processor.id})">
                <i class="icon-play"></i> Avvia
            </button>`);
        } else if (processor.status === 'active' || processor.status === 'processing') {
            actions.push(`<button class="btn btn-warning" onclick="prosserBoard.pauseProcessor(${processor.id})">
                <i class="icon-pause"></i> Pausa
            </button>`);
        }

        if (processor.status !== 'idle') {
            actions.push(`<button class="btn btn-danger" onclick="prosserBoard.stopProcessor(${processor.id})">
                <i class="icon-stop"></i> Stop
            </button>`);
        }

        actions.push(`<button class="btn btn-info" onclick="prosserBoard.showProcessorDetails(${processor.id})">
            <i class="icon-info"></i> Dettagli
        </button>`);

        actions.push(`<button class="btn btn-secondary" onclick="prosserBoard.editProcessor(${processor.id})">
            <i class="icon-edit"></i> Modifica
        </button>`);

        return actions.join('');
    }

    applyFilters() {
        const statusFilter = document.getElementById('statusFilter').value;
        const typeFilter = document.getElementById('typeFilter').value;
        const searchFilter = document.getElementById('searchFilter').value.toLowerCase();

        this.filteredProcessors = this.processors.filter(processor => {
            const matchesStatus = !statusFilter || processor.status === statusFilter;
            const matchesType = !typeFilter || processor.type === typeFilter;
            const matchesSearch = !searchFilter ||
                processor.name.toLowerCase().includes(searchFilter) ||
                processor.description.toLowerCase().includes(searchFilter);

            return matchesStatus && matchesType && matchesSearch;
        });

        this.renderProcessors();
    }

    updateStats() {
        const stats = {
            active: 0,
            processing: 0,
            idle: 0,
            error: 0
        };

        this.processors.forEach(processor => {
            stats[processor.status]++;
        });

        document.getElementById('activeCount').textContent = stats.active;
        document.getElementById('processingCount').textContent = stats.processing;
        document.getElementById('idleCount').textContent = stats.idle;
        document.getElementById('errorCount').textContent = stats.error;
    }

    showAddProcessorModal() {
        document.getElementById('modalTitle').textContent = 'Aggiungi Processore';
        document.getElementById('processorForm').reset();
        this.currentProcessor = null;
        this.showModal('processorModal');
    }

    editProcessor(id) {
        const processor = this.processors.find(p => p.id === id);
        if (!processor) return;

        document.getElementById('modalTitle').textContent = 'Modifica Processore';
        document.getElementById('processorName').value = processor.name;
        document.getElementById('processorType').value = processor.type;
        document.getElementById('processorDescription').value = processor.description;
        document.getElementById('processorPriority').value = processor.priority;
        document.getElementById('processorAutoStart').checked = processor.autoStart;

        this.currentProcessor = processor;
        this.showModal('processorModal');
    }

    saveProcessor() {
        const form = document.getElementById('processorForm');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const formData = {
            name: document.getElementById('processorName').value,
            type: document.getElementById('processorType').value,
            description: document.getElementById('processorDescription').value,
            priority: document.getElementById('processorPriority').value,
            autoStart: document.getElementById('processorAutoStart').checked
        };

        if (this.currentProcessor) {
            // Modifica processore esistente
            Object.assign(this.currentProcessor, formData);
            this.showNotification('Processore modificato con successo', 'success');
        } else {
            // Aggiungi nuovo processore
            const newProcessor = {
                id: Math.max(...this.processors.map(p => p.id)) + 1,
                ...formData,
                status: formData.autoStart ? 'active' : 'idle',
                metrics: {
                    cpu: 0,
                    memory: 0,
                    tasks: 0
                },
                created: new Date(),
                lastActive: new Date()
            };

            this.processors.push(newProcessor);
            this.showNotification('Processore aggiunto con successo', 'success');
        }

        this.applyFilters();
        this.updateStats();
        this.closeModal('processorModal');
    }

    showProcessorDetails(id) {
        const processor = this.processors.find(p => p.id === id);
        if (!processor) return;

        const typeLabels = {
            data: 'Data Processor',
            image: 'Image Processor',
            text: 'Text Processor',
            queue: 'Queue Processor'
        };

        const priorityLabels = {
            low: 'Bassa',
            medium: 'Media',
            high: 'Alta'
        };

        const statusLabels = {
            active: 'Attivo',
            idle: 'Inattivo',
            processing: 'In Elaborazione',
            error: 'Errore'
        };

        document.getElementById('detailsTitle').textContent = `Dettagli - ${processor.name}`;
        document.getElementById('detailsContent').innerHTML = `
            <div class="detail-section">
                <h3>Informazioni Generali</h3>
                <div class="detail-item">
                    <span class="detail-label">Nome:</span>
                    <span class="detail-value">${processor.name}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Tipo:</span>
                    <span class="detail-value">${typeLabels[processor.type]}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Stato:</span>
                    <span class="detail-value">${statusLabels[processor.status]}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Priorità:</span>
                    <span class="detail-value">${priorityLabels[processor.priority]}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Avvio Automatico:</span>
                    <span class="detail-value">${processor.autoStart ? 'Sì' : 'No'}</span>
                </div>
            </div>

            <div class="detail-section">
                <h3>Metriche Correnti</h3>
                <div class="detail-item">
                    <span class="detail-label">Utilizzo CPU:</span>
                    <span class="detail-value">${processor.metrics.cpu}%</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Utilizzo Memoria:</span>
                    <span class="detail-value">${processor.metrics.memory}%</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Tasks Attivi:</span>
                    <span class="detail-value">${processor.metrics.tasks}</span>
                </div>
            </div>

            <div class="detail-section">
                <h3>Cronologia</h3>
                <div class="detail-item">
                    <span class="detail-label">Creato il:</span>
                    <span class="detail-value">${processor.created.toLocaleDateString('it-IT')} ${processor.created.toLocaleTimeString('it-IT')}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Ultima Attività:</span>
                    <span class="detail-value">${processor.lastActive.toLocaleDateString('it-IT')} ${processor.lastActive.toLocaleTimeString('it-IT')}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Versione:</span>
                    <span class="detail-value">${processor.version || 'N/A'}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Uptime:</span>
                    <span class="detail-value">${ProsserUtils.formatDuration(processor.uptime || 0)}</span>
                </div>
            </div>

            <div class="detail-section">
                <h3>Descrizione</h3>
                <p>${processor.description}</p>
            </div>
        `;

        this.showModal('detailsModal');
    }

    startProcessor(id) {
        const processor = this.processors.find(p => p.id === id);
        if (processor) {
            processor.status = 'active';
            processor.lastActive = new Date();
            this.renderProcessors();
            this.updateStats();
            this.showNotification(`Processore ${processor.name} avviato`, 'success');
        }
    }

    pauseProcessor(id) {
        const processor = this.processors.find(p => p.id === id);
        if (processor) {
            processor.status = 'idle';
            this.renderProcessors();
            this.updateStats();
            this.showNotification(`Processore ${processor.name} in pausa`, 'warning');
        }
    }

    stopProcessor(id) {
        const processor = this.processors.find(p => p.id === id);
        if (processor) {
            processor.status = 'idle';
            processor.metrics.cpu = 0;
            processor.metrics.memory = 0;
            processor.metrics.tasks = 0;
            this.renderProcessors();
            this.updateStats();
            this.showNotification(`Processore ${processor.name} fermato`, 'info');
        }
    }

    deleteProcessor(id) {
        if (confirm('Sei sicuro di voler eliminare questo processore?')) {
            this.processors = this.processors.filter(p => p.id !== id);
            this.applyFilters();
            this.updateStats();
            this.showNotification('Processore eliminato', 'success');
        }
    }

    refreshProcessors() {
        this.showLoading();

        // Simula refresh dei dati
        setTimeout(() => {
            // Aggiorna metriche random
            this.processors.forEach(processor => {
                if (processor.status === 'active' || processor.status === 'processing') {
                    processor.metrics.cpu = Math.floor(Math.random() * 100);
                    processor.metrics.memory = Math.floor(Math.random() * 100);
                    processor.metrics.tasks = Math.floor(Math.random() * 50);
                    processor.lastActive = new Date();
                }
            });

            this.renderProcessors();
            this.updateStats();
            this.hideLoading();
            this.showNotification('Dati aggiornati', 'success');
        }, 500);
    }

    startAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
        }

        this.refreshInterval = setInterval(() => {
            if (!document.hidden && this.autoRefreshEnabled) {
                // Aggiorna solo le metriche senza ricaricare tutto
                this.processors.forEach(processor => {
                    if (processor.status === 'active' || processor.status === 'processing') {
                        // Simula variazioni realistiche delle metriche
                        processor.metrics.cpu = Math.max(0, Math.min(100,
                            processor.metrics.cpu + (Math.random() - 0.5) * 10));
                        processor.metrics.memory = Math.max(0, Math.min(100,
                            processor.metrics.memory + (Math.random() - 0.5) * 5));
                        processor.metrics.tasks = Math.max(0, Math.min(50,
                            processor.metrics.tasks + Math.floor((Math.random() - 0.5) * 3)));

                        processor.lastActive = new Date();
                    }
                });

                this.renderProcessors();
            }
        }, 5000); // Aggiorna ogni 5 secondi
    }

    stopAutoRefresh() {
        this.autoRefreshEnabled = false;
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }

    resumeAutoRefresh() {
        this.autoRefreshEnabled = true;
        this.startAutoRefresh();
    }

    showModal(modalId) {
        document.getElementById(modalId).style.display = 'block';
        document.body.style.overflow = 'hidden';
    }

    closeModal(modalId) {
        document.getElementById(modalId).style.display = 'none';
        document.body.style.overflow = 'auto';
    }

    showLoading() {
        document.getElementById('loadingOverlay').style.display = 'flex';
    }

    hideLoading() {
        document.getElementById('loadingOverlay').style.display = 'none';
    }

    showNotification(message, type = 'info') {
        // Crea notifica temporanea
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 25px;
            border-radius: 8px;
            color: white;
            font-weight: 600;
            z-index: 10000;
            transform: translateX(100%);
            transition: transform 0.3s ease;
        `;

        const colors = {
            success: '#28a745',
            error: '#dc3545',
            warning: '#ffc107',
            info: '#17a2b8'
        };

        notification.style.backgroundColor = colors[type] || colors.info;
        document.body.appendChild(notification);

        // Animazione di ingresso
        setTimeout(() => {
            notification.style.transform = 'translateX(0)';
        }, 100);

        // Rimozione automatica
        setTimeout(() => {
            notification.style.transform = 'translateX(100%)';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 3000);
    }

    // Metodi per API esterne (placeholder)
    async fetchProcessorsFromAPI() {
        try {
            // const response = await fetch('/api/processors');
            // return await response.json();
            return this.generateMockProcessors();
        } catch (error) {
            console.error('Errore nel caricamento dei processori:', error);
            this.showNotification('Errore nel caricamento dei dati', 'error');
            return [];
        }
    }

    async saveProcessorToAPI(processor) {
        try {
            // const response = await fetch('/api/processors', {
            //     method: 'POST',
            //     headers: { 'Content-Type': 'application/json' },
            //     body: JSON.stringify(processor)
            // });
            // return await response.json();
            return processor;
        } catch (error) {
            console.error('Errore nel salvataggio del processore:', error);
            this.showNotification('Errore nel salvataggio', 'error');
            throw error;
        }
    }

    async updateProcessorStatus(id, status) {
        try {
            // const response = await fetch(`/api/processors/${id}/status`, {
            //     method: 'PATCH',
            //     headers: { 'Content-Type': 'application/json' },
            //     body: JSON.stringify({ status })
            // });
            // return await response.json();
            return { id, status };
        } catch (error) {
            console.error('Errore nell\'aggiornamento dello stato:', error);
            this.showNotification('Errore nell\'aggiornamento dello stato', 'error');
            throw error;
        }
    }
}

// Inizializza l'applicazione quando il DOM è pronto
document.addEventListener('DOMContentLoaded', () => {
    window.prosserBoard = new ProsserBoard();
});

// Gestione della visibilità della pagina per pausare gli aggiornamenti
document.addEventListener('visibilitychange', () => {
    if (document.hidden) {
        console.log('Pagina nascosta - aggiornamenti in pausa');
        if (window.prosserBoard) {
            window.prosserBoard.stopAutoRefresh();
        }
    } else {
        console.log('Pagina visibile - ripresa aggiornamenti');
        if (window.prosserBoard) {
            window.prosserBoard.resumeAutoRefresh();
            window.prosserBoard.refreshProcessors();
        }
    }
});

// Gestione degli errori globali
window.addEventListener('error', (event) => {
    console.error('Errore JavaScript:', event.error);
    if (window.prosserBoard) {
        window.prosserBoard.showNotification('Si è verificato un errore imprevisto', 'error');
    }
});

// Utility functions
const ProsserUtils = {
    formatBytes(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    },

    formatDuration(ms) {
        const seconds = Math.floor((ms / 1000) % 60);
        const minutes = Math.floor((ms / (1000 * 60)) % 60);
        const hours = Math.floor((ms / (1000 * 60 * 60)) % 24);

        if (hours > 0) {
            return `${hours}h ${minutes}m ${seconds}s`;
        } else if (minutes > 0) {
            return `${minutes}m ${seconds}s`;
        } else {
            return `${seconds}s`;
        }
    },

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
};