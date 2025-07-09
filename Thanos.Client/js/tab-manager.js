// Battlesnake Board Converter - Tab Manager

class TabManager {
    constructor() {
        this.currentTab = 'import';
        this.tabs = new Map();
        this.initialized = false;
    }

    // Register a tab with its handler
    registerTab(tabName, handler) {
        this.tabs.set(tabName, handler);
        console.log(`Tab registered: ${tabName}`);
    }

    // Initialize tab system
    init() {
        if (this.initialized) return;

        this.setupTabButtons();
        this.showTab(this.currentTab);
        this.initialized = true;

        console.log('TabManager initialized');
    }

    // Setup click events for tab buttons
    setupTabButtons() {
        const tabButtons = document.querySelectorAll('.tab-button');

        tabButtons.forEach(button => {
            button.addEventListener('click', (event) => {
                const tabName = this.getTabNameFromButton(button);
                if (tabName) {
                    this.switchTab(tabName);
                }
            });
        });
    }

    // Extract tab name from button
    getTabNameFromButton(button) {
        // Check onclick attribute first
        const onclick = button.getAttribute('onclick');
        if (onclick) {
            const match = onclick.match(/switchTab\(['"](.+?)['"]\)/);
            if (match) return match[1];
        }

        // Check data-tab attribute
        const dataTab = button.getAttribute('data-tab');
        if (dataTab) return dataTab;

        // Check button text as fallback
        const text = button.textContent.toLowerCase().trim();
        if (text.includes('import')) return 'import';
        if (text.includes('process')) return 'process';
        if (text.includes('format')) return 'formatter';

        return null;
    }

    // Switch to a specific tab
    switchTab(tabName) {
        if (!this.tabs.has(tabName)) {
            console.warn(`Tab not found: ${tabName}`);
            return;
        }

        // Hide all tab contents
        document.querySelectorAll('.tab-content').forEach(tab => {
            tab.classList.remove('active');
        });

        // Deactivate all tab buttons
        document.querySelectorAll('.tab-button').forEach(btn => {
            btn.classList.remove('active');
        });

        // Show target tab content
        const tabElement = document.getElementById(`${tabName}-tab`);
        if (tabElement) {
            tabElement.classList.add('active');
        }

        // Activate target tab button
        const buttonElement = document.querySelector(`[onclick*="${tabName}"], [data-tab="${tabName}"]`);
        if (buttonElement) {
            buttonElement.classList.add('active');
        }

        // Call tab's activation handler
        const handler = this.tabs.get(tabName);
        if (handler && typeof handler.onActivate === 'function') {
            handler.onActivate();
        }

        this.currentTab = tabName;
        console.log(`Switched to tab: ${tabName}`);
    }

    // Show a specific tab (alias for switchTab)
    showTab(tabName) {
        this.switchTab(tabName);
    }

    // Get current active tab
    getCurrentTab() {
        return this.currentTab;
    }

    // Check if a tab is registered
    hasTab(tabName) {
        return this.tabs.has(tabName);
    }

    // Get all registered tabs
    getRegisteredTabs() {
        return Array.from(this.tabs.keys());
    }

    // Unregister a tab
    unregisterTab(tabName) {
        this.tabs.delete(tabName);
        console.log(`Tab unregistered: ${tabName}`);
    }

    // Update tab button state manually (for dynamic buttons)
    updateTabButton(tabName, isActive = false) {
        const buttonElement = document.querySelector(`[onclick*="${tabName}"], [data-tab="${tabName}"]`);
        if (buttonElement) {
            if (isActive) {
                buttonElement.classList.add('active');
            } else {
                buttonElement.classList.remove('active');
            }
        }
    }

    // Update tab content visibility manually
    updateTabContent(tabName, isVisible = false) {
        const tabElement = document.getElementById(`${tabName}-tab`);
        if (tabElement) {
            if (isVisible) {
                tabElement.classList.add('active');
            } else {
                tabElement.classList.remove('active');
            }
        }
    }

    // Event system for tab changes
    onTabChange(callback) {
        // Store callback for tab change events
        this.tabChangeCallback = callback;
    }

    // Trigger tab change event
    triggerTabChange(fromTab, toTab) {
        if (this.tabChangeCallback) {
            this.tabChangeCallback(fromTab, toTab);
        }
    }
}

// Create global tab manager instance
const tabManager = new TabManager();

// Global function for onclick handlers (backward compatibility)
function switchTab(tabName) {
    tabManager.switchTab(tabName);
}

// Export for module use
window.BattlesnakeTabManager = {
    tabManager,
    switchTab
};