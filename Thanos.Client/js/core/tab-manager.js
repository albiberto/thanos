// Battlesnake Board Converter - Tab Manager

class TabManager {
    constructor() {
        this.currentTab = 'import';
        this.tabs = new Map();
        this.initialized = false;
        this.tabChangeCallback = null;
    }

    // Register a tab with its handler
    registerTab(tabName, handler) {
        this.tabs.set(tabName, handler);
        console.log(`Tab registered: ${tabName}`);

        // If we're already initialized, make sure the tab button is properly set up
        if (this.initialized) {
            this.updateTabButton(tabName, tabName === this.currentTab);
        }
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

        const previousTab = this.currentTab;

        // Call previous tab's deactivation handler
        if (previousTab && this.tabs.has(previousTab)) {
            const previousHandler = this.tabs.get(previousTab);
            if (previousHandler && typeof previousHandler.onDeactivate === 'function') {
                previousHandler.onDeactivate();
            }
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
        } else {
            console.warn(`Tab element not found: ${tabName}-tab`);
        }

        // Activate target tab button - fixed selector
        const buttonElement = this.findTabButton(tabName);
        if (buttonElement) {
            buttonElement.classList.add('active');
        } else {
            console.warn(`Tab button not found for: ${tabName}`);
        }

        // Call tab's activation handler
        const handler = this.tabs.get(tabName);
        if (handler && typeof handler.onActivate === 'function') {
            handler.onActivate();
        }

        // Update current tab and trigger change event
        this.currentTab = tabName;
        this.triggerTabChange(previousTab, tabName);

        console.log(`Switched to tab: ${tabName}`);
    }

    // Find tab button with more reliable selector
    findTabButton(tabName) {
        // Try multiple selection strategies
        const selectors = [
            `[data-tab="${tabName}"]`,
            `[onclick*="switchTab('${tabName}')"]`,
            `[onclick*='switchTab("${tabName}")']`,
            `.tab-button[onclick*="${tabName}"]`
        ];

        for (const selector of selectors) {
            const element = document.querySelector(selector);
            if (element) return element;
        }

        // Last resort: search by text content
        const buttons = document.querySelectorAll('.tab-button');
        for (const button of buttons) {
            const text = button.textContent.toLowerCase().trim();
            if ((tabName === 'import' && text.includes('import')) ||
                (tabName === 'process' && text.includes('process')) ||
                (tabName === 'formatter' && text.includes('format'))) {
                return button;
            }
        }

        return null;
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
        const buttonElement = this.findTabButton(tabName);
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
        if (this.tabChangeCallback && typeof this.tabChangeCallback === 'function') {
            try {
                this.tabChangeCallback(fromTab, toTab);
            } catch (error) {
                console.error('Error in tab change callback:', error);
            }
        }

        // Dispatch custom event for other listeners
        const event = new CustomEvent('tabChanged', {
            detail: { fromTab, toTab, timestamp: Date.now() }
        });
        document.dispatchEvent(event);
    }

    // Safe tab registration for delayed initialization
    safeRegisterTab(tabName, handler, maxRetries = 10, retryDelay = 100) {
        let retries = 0;

        const attemptRegistration = () => {
            if (this.initialized) {
                this.registerTab(tabName, handler);
                return;
            }

            if (retries < maxRetries) {
                retries++;
                setTimeout(attemptRegistration, retryDelay);
            } else {
                console.warn(`Failed to register tab ${tabName} after ${maxRetries} retries`);
            }
        };

        attemptRegistration();
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
    switchTab,
    // Add helper for safe registration
    registerTab: (tabName, handler) => tabManager.safeRegisterTab(tabName, handler)
};

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        // Small delay to ensure all scripts are loaded
        setTimeout(() => tabManager.init(), 50);
    });
} else {
    setTimeout(() => tabManager.init(), 50);
}