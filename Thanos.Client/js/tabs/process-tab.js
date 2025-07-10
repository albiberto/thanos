/**
 * Process Tab - Unobtrusive JavaScript
 * Works with existing HTML structure without creating DOM elements
 */
class ProcessTab {
    constructor() {
        this.processors = [];
        this.filteredProcessors = [];
        this.currentProcessor = null;
        this.refreshInterval = null;
        this.autoRefreshEnabled = true;
        this.initialized = false;
    }

    /**
     * Initialize the process tab - unobtrusive approach
     */
    init() {
        if (this.initialized) return;

        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.doInit());
        } else {
            this.doInit();
        }
    }

    /**
     * Actual initialization after DOM is ready
     */
    doInit() {
        try {
            this.setupEventListeners();
            this.loadProcessors();
            this.initialized = true;
            console.log('ProcessTab initialized (unobtrusive)');
        } catch (error) {
            console.error('Failed to initialize ProcessTab:', error);
        }
    }

    /**
     * Setup event listeners - unobtrusive approach
     */
    setupEventListeners() {
        // Header controls
        this.bindEvent('addProcessorBtn', 'click', () => this.showAddProcessorModal());
        this.bindEvent('refreshBtn', 'click', () => this.refreshProcessors());

        // Filters
        this.bindEvent('statusFilter', 'change', () => this.applyFilters());
        this.bindEvent('typeFilter', 'change', () => this.applyFilters());
        this.bindEvent('searchFilter', 'input', () => this.applyFilters());

        // Modal controls
        this.bindEvent('closeModal', 'click', () => this.hideModal('processorModal'));
        this.bindEvent('cancelBtn', 'click', () => this.hideModal('processorModal'));
        this.bindEvent('saveBtn', 'click', () => this.saveProcessor());
        this.bindEvent('closeDetailsModal', 'click', () => this.hideModal('detailsModal'));
        this.bindEvent('closeDetailsBtn', 'click', () => this.hideModal('detailsModal'));

        // Form submission
        this.bindEvent('processorForm', 'submit', (e) => {
            e.preventDefault();
            this.saveProcessor();
        });

        // Notification close
        this.bindEvent('notificationClose', 'click', () => this.hideNotification());

        // Close modals on backdrop click
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('modal')) {
                this.hideModal(e.target.id);
            }
        });

        // Escape key to close modals
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.hideModal('processorModal');
                this.hideModal('detailsModal');
            }
        });
    }

    /**
     * Bind event with null safety
     */
    bindEvent(elementId, event, handler) {
        const element = document.getElementById(elementId);
        if (element) {
            element.addEventListener(event, handler);
        } else {
            console.warn(`Element not found: ${elementId}`);
        }
    }

    /**
     * Update element text content with null safety
     */
    updateText(elementId, text) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = text;
        }
    }

    /**
     * Update element innerHTML with null safety
     */
    updateHTML(elementId, html) {
        const element = document.getElementById(elementId);
        if (element) {
            element.innerHTML = html;
        }
    }

    /**
     * Show/hide element with null safety
     */
    toggleElement(elementId, show) {
        const element = document.getElementById(elementId);
        if (element) {
            element.style.display = show ? 'block' : 'none';
        }
    }

    /**
     * Load processors data
     */
    loadProcessors() {
        this.showLoading();

        // Simulate API call
        setTimeout(() => {
            this.processors = this.generateMockProcessors();
            this.filteredProcessors = [...this.processors];
            this.renderProcessors();
            this.updateStats();
            this.hideLoading();
            this.startAutoRefresh();
        }, 1000);
    }

    /**
     * Generate mock processor data
     */
    generateMockProcessors() {
        const types = ['data', 'image', 'text', 'queue'];
        const statuses = ['active', 'idle', 'processing', 'error'];
        const priorities = ['low', 'medium', 'high'];

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

        return processorNames.map((name, index) => ({
            id: index + 1,
            name,
            type: types[index % types.length],
            status: statuses[Math.floor(Math.random() * statuses.length)],
            description: descriptions[index],
            priority: priorities[Math.floor(Math.random() * priorities.length)],
            autoStart: Math.random() > 0.3,
            metrics: {
                cpu: Math.floor(Math.random() * 100),
                memory: Math.floor(Math.random() * 100),
                tasks: Math.floor(Math.random() * 50)
            },
            created: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000),
            lastActive: new Date(Date.now() - Math.random() * 24 * 60 * 60 * 1000)
        }));
    }

    /**
     * Render processors list
     */
    renderProcessors() {
        const container = document.getElementById('processorsContainer');
        if (!container) return;

        if (this.filteredProcessors.length === 0) {
            container.innerHTML = `
                <div class="no-processors">
                    <div class="no-processors-icon">📭</div>
                    <div class="no-processors-text">Nessun processore trovato</div>
                </div>
            `;
            return;
        }

        const typeLabels = {
            data: 'Data Processor',
            image: 'Image Processor',
            text: 'Text Processor',
            queue: 'Queue Processor'
        };

        const statusLabels = {
            active: 'Attivo',
            idle: 'Inattivo',
            processing: 'In Elaborazione',
            error: 'Errore'
        };

        const processorsHTML = this.filteredProcessors.map(processor => `
            <div class="processor-card" data-status="${processor.status}" data-id="${processor.id}">
                <div class="processor-header">
                    <div class="processor-info">
                        <h3 class="processor-name">${this.escapeHtml(processor.name)}</h3>
                        <div class="processor-type">${typeLabels[processor.type]}</div>
                    </div>
                    <div class="processor-status ${processor.status}">
                        ${statusLabels[processor.status]}
                    </div>
                </div>
                
                <div class="processor-description">
                    ${this.escapeHtml(processor.description)}
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
                    ${this.getProcessorActionsHTML(processor)}
                </div>
            </div>
        `).join('');

        container.innerHTML = processorsHTML;

        // Bind action buttons after rendering
        this.bindProcessorActions();
    }

    /**
     * Generate processor action buttons HTML
     */
    getProcessorActionsHTML(processor) {
        const actions = [];

        if (processor.status === 'idle') {
            actions.push(`<button class="btn btn-success" data-action="start" data-id="${processor.id}">▶️ Avvia</button>`);
        } else if (processor.status === 'active' || processor.status === 'processing') {
            actions.push(`<button class="btn btn-warning" data-action="pause" data-id="${processor.id}">⏸️ Pausa</button>`);
            actions.push(`<button class="btn btn-danger" data-action="stop" data-id="${processor.id}">⏹️ Stop</button>`);
        }

        actions.push(`<button class="btn btn-info" data-action="details" data-id="${processor.id}">ℹ️ Dettagli</button>`);
        actions.push(`<button class="btn btn-secondary" data-action="edit" data-id="${processor.id}">✏️ Modifica</button>`);

        return actions.join('');
    }

    /**
     * Bind processor action buttons
     */
    bindProcessorActions() {
        const container = document.getElementById('processorsContainer');
        if (!container) return;

        container.addEventListener('click', (e) => {
            const button = e.target.closest('[data-action]');
            if (!button) return;

            const action = button.dataset.action;
            const processorId = parseInt(button.dataset.id);

            switch (action) {
                case 'start':
                    this.startProcessor(processorId);
                    break;
                case 'pause':
                    this.pauseProcessor(processorId);
                    break;
                case 'stop':
                    this.stopProcessor(processorId);
                    break;
                case 'details':
                    this.showProcessorDetails(processorId);
                    break;
                case 'edit':
                    this.editProcessor(processorId);
                    break;
            }
        });
    }

    /**
     * Apply filters
     */
    applyFilters() {
        const statusFilter = document.getElementById('statusFilter')?.value || '';
        const typeFilter = document.getElementById('typeFilter')?.value || '';
        const searchFilter = document.getElementById('searchFilter')?.value.toLowerCase() || '';

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

    /**
     * Update statistics
     */
    updateStats() {
        const stats = { active: 0, processing: 0, idle: 0, error: 0 };

        this.processors.forEach(processor => {
            stats[processor.status]++;
        });

        this.updateText('activeCount', stats.active);
        this.updateText('processingCount', stats.processing);
        this.updateText('idleCount', stats.idle);
        this.updateText('errorCount', stats.error);
    }

    /**
     * Show loading state
     */
    showLoading() {
        this.toggleElement('loadingIndicator', true);
        const container = document.getElementById('processorsContainer');
        if (container) container.style.opacity = '0.5';
    }

    /**
     * Hide loading state
     */
    hideLoading() {
        this.toggleElement('loadingIndicator', false);
        const container = document.getElementById('processorsContainer');
        if (container) container.style.opacity = '1';
    }

    /**
     * Show modal
     */
    showModal(modalId) {
        this.toggleElement(modalId, true);
        const modal = document.getElementById(modalId);
        if (modal) modal.style.display = 'flex';
    }

    /**
     * Hide modal
     */
    hideModal(modalId) {
        this.toggleElement(modalId, false);
    }

    /**
     * Show notification
     */
    showNotification(message, type = 'info') {
        this.updateText('notificationMessage', message);
        const notification = document.getElementById('notification');
        if (notification) {
            notification.className = `notification ${type}`;
            notification.style.display = 'block';

            // Auto hide after 3 seconds
            setTimeout(() => this.hideNotification(), 3000);
        }
    }

    /**
     * Hide notification
     */
    hideNotification() {
        this.toggleElement('notification', false);
    }

    /**
     * Refresh processors
     */
    refreshProcessors() {
        this.loadProcessors();
        this.showNotification('Processori aggiornati', 'success');
    }

    /**
     * Show add processor modal
     */
    showAddProcessorModal() {
        this.updateText('modalTitle', 'Aggiungi Processore');
        const form = document.getElementById('processorForm');
        if (form) form.reset();
        this.currentProcessor = null;
        this.showModal('processorModal');
    }

    /**
     * Edit processor
     */
    editProcessor(id) {
        const processor = this.processors.find(p => p.id === id);
        if (!processor) return;

        this.updateText('modalTitle', 'Modifica Processore');

        // Fill form with processor data
        const fields = {
            processorName: processor.name,
            processorType: processor.type,
            processorDescription: processor.description,
            processorPriority: processor.priority
        };

        Object.entries(fields).forEach(([id, value]) => {
            const element = document.getElementById(id);
            if (element) element.value = value;
        });

        const autoStartElement = document.getElementById('processorAutoStart');
        if (autoStartElement) autoStartElement.checked = processor.autoStart;

        this.currentProcessor = processor;
        this.showModal('processorModal');
    }

    /**
     * Save processor
     */
    saveProcessor() {
        const form = document.getElementById('processorForm');
        if (!form || !form.checkValidity()) {
            form?.reportValidity();
            return;
        }

        const formData = {
            name: document.getElementById('processorName')?.value || '',
            type: document.getElementById('processorType')?.value || 'data',
            description: document.getElementById('processorDescription')?.value || '',
            priority: document.getElementById('processorPriority')?.value || 'medium',
            autoStart: document.getElementById('processorAutoStart')?.checked || false
        };

        if (this.currentProcessor) {
            // Update existing processor
            Object.assign(this.currentProcessor, formData);
            this.showNotification('Processore modificato con successo', 'success');
        } else {
            // Add new processor
            const newProcessor = {
                id: Math.max(...this.processors.map(p => p.id), 0) + 1,
                ...formData,
                status: formData.autoStart ? 'active' : 'idle',
                metrics: { cpu: 0, memory: 0, tasks: 0 },
                created: new Date(),
                lastActive: new Date()
            };

            this.processors.push(newProcessor);
            this.showNotification('Processore aggiunto con successo', 'success');
        }

        this.applyFilters();
        this.updateStats();
        this.hideModal('processorModal');
    }

    /**
     * Show processor details
     */
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

        this.updateText('detailsTitle', `Dettagli - ${processor.name}`);
        this.updateHTML('detailsContent', `
            <div class="detail-section">
                <h3>Informazioni Generali</h3>
                <div class="detail-item">
                    <span class="detail-label">Nome:</span>
                    <span class="detail-value">${this.escapeHtml(processor.name)}</span>
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
                <div class="detail-item">
                    <span class="detail-label">Creato:</span>
                    <span class="detail-value">${processor.created.toLocaleString()}</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Ultima Attività:</span>
                    <span class="detail-value">${processor.lastActive.toLocaleString()}</span>
                </div>
            </div>

            <div class="detail-section">
                <h3>Metriche</h3>
                <div class="detail-item">
                    <span class="detail-label">CPU:</span>
                    <span class="detail-value">${processor.metrics.cpu}%</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Memoria:</span>
                    <span class="detail-value">${processor.metrics.memory}%</span>
                </div>
                <div class="detail-item">
                    <span class="detail-label">Tasks:</span>
                    <span class="detail-value">${processor.metrics.tasks}</span>
                </div>
            </div>
        `);

        this.showModal('detailsModal');
    }

    /**
     * Start processor
     */
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

    /**
     * Pause processor
     */
    pauseProcessor(id) {
        const processor = this.processors.find(p => p.id === id);
        if (processor) {
            processor.status = 'idle';
            this.renderProcessors();
            this.updateStats();
            this.showNotification(`Processore ${processor.name} messo in pausa`, 'warning');
        }
    }

    /**
     * Stop processor
     */
    stopProcessor(id) {