/**
 * BattleSnake Board Converter - Core Utilities
 * File unificato con tutte le utilità comuni
 */

// === CONSTANTS ===
const BOARD_DIRECTIONS = [
    { value: 1, icons: "⬆️", desc: "UP" },
    { value: 2, icons: "⬇️", desc: "DOWN" },
    { value: 3, icons: "⬆️⬇️", desc: "UP | DOWN" },
    { value: 4, icons: "⬅️", desc: "LEFT" },
    { value: 5, icons: "⬆️⬅️", desc: "UP | LEFT" },
    { value: 6, icons: "⬇️⬅️", desc: "DOWN | LEFT" },
    { value: 7, icons: "⬆️⬇️⬅️", desc: "UP | DOWN | LEFT" },
    { value: 8, icons: "➡️", desc: "RIGHT" },
    { value: 9, icons: "⬆️➡️", desc: "UP | RIGHT" },
    { value: 10, icons: "⬇️➡️", desc: "DOWN | RIGHT" },
    { value: 11, icons: "⬆️⬇️➡️", desc: "UP | DOWN | RIGHT" },
    { value: 12, icons: "⬅️➡️", desc: "LEFT | RIGHT" },
    { value: 13, icons: "⬆️⬅️➡️", desc: "UP | LEFT | RIGHT" },
    { value: 14, icons: "⬇️⬅️➡️", desc: "DOWN | LEFT | RIGHT" },
    { value: 15, icons: "⬆️⬇️⬅️➡️", desc: "ALL" }
];

// === DOM UTILITIES ===
class DOMUtils {
    /**
     * Trova elemento DOM con gestione errori
     */
    static findElement(selector, context = document) {
        try {
            const element = context.querySelector(selector);
            if (!element) {
                console.warn(`Element not found: ${selector}`);
            }
            return element;
        } catch (error) {
            console.error(`Error finding element ${selector}:`, error);
            return null;
        }
    }

    /**
     * Trova tutti gli elementi DOM
     */
    static findElements(selector, context = document) {
        try {
            return Array.from(context.querySelectorAll(selector));
        } catch (error) {
            console.error(`Error finding elements ${selector}:`, error);
            return [];
        }
    }

    /**
     * Aggiunge event listener con gestione errori
     */
    static addListener(element, event, handler, options = {}) {
        if (!element) return false;

        try {
            element.addEventListener(event, handler, options);
            return true;
        } catch (error) {
            console.error(`Error adding listener:`, error);
            return false;
        }
    }

    /**
     * Mostra/nasconde elemento
     */
    static toggleElement(element, show = null) {
        if (!element) return;

        if (show === null) {
            element.classList.toggle('d-none');
        } else {
            element.classList.toggle('d-none', !show);
        }
    }

    /**
     * Aggiunge classe CSS con animazione
     */
    static addClass(element, className, animate = false) {
        if (!element) return;

        element.classList.add(className);
        if (animate) {
            element.classList.add('fade-in');
        }
    }

    /**
     * Rimuove classe CSS
     */
    static removeClass(element, className) {
        if (!element) return;
        element.classList.remove(className);
    }
}

// === VALIDATION UTILITIES ===
class ValidationUtils {
    /**
     * Valida JSON string
     */
    static isValidJSON(jsonString) {
        try {
            JSON.parse(jsonString);
            return true;
        } catch {
            return false;
        }
    }

    /**
     * Valida structure di board BattleSnake
     */
    static isValidBoard(board) {
        if (!board || typeof board !== 'object') return false;

        const required = ['width', 'height', 'snakes', 'food'];
        return required.every(field => field in board);
    }

    /**
     * Valida array di boards
     */
    static validateBoards(boards) {
        if (!Array.isArray(boards)) return { valid: false, error: 'Input non è un array' };

        if (boards.length === 0) return { valid: false, error: 'Array vuoto' };

        for (let i = 0; i < boards.length; i++) {
            if (!this.isValidBoard(boards[i])) {
                return { valid: false, error: `Board ${i + 1} non valida` };
            }
        }

        return { valid: true };
    }

    /**
     * Sanitizza input string
     */
    static sanitizeInput(input) {
        return input
        .replace(/[<>]/g, '') // Rimuove caratteri pericolosi
        .trim();
    }
}

// === STRING UTILITIES ===
class StringUtils {
    /**
     * Trunca string con ellipsis
     */
    static truncate(str, maxLength = 50) {
        if (!str || str.length <= maxLength) return str;
        return str.substring(0, maxLength - 3) + '...';
    }

    /**
     * Capitalizza prima lettera
     */
    static capitalize(str) {
        if (!str) return '';
        return str.charAt(0).toUpperCase() + str.slice(1);
    }

    /**
     * Format numero con separatori
     */
    static formatNumber(num) {
        return new Intl.NumberFormat('it-IT').format(num);
    }

    /**
     * Format byte size
     */
    static formatBytes(bytes) {
        if (bytes === 0) return '0 Bytes';

        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));

        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    /**
     * Generate random ID
     */
    static generateId(prefix = 'id') {
        return `${prefix}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    }
}

// === CLIPBOARD UTILITIES ===
class ClipboardUtils {
    /**
     * Copia testo negli appunti
     */
    static async copy(text) {
        try {
            await navigator.clipboard.writeText(text);
            return { success: true };
        } catch (error) {
            console.error('Clipboard copy failed:', error);

            // Fallback per browser non supportati
            try {
                const textArea = document.createElement('textarea');
                textArea.value = text;
                document.body.appendChild(textArea);
                textArea.select();
                document.execCommand('copy');
                document.body.removeChild(textArea);
                return { success: true };
            } catch (fallbackError) {
                return { success: false, error: fallbackError.message };
            }
        }
    }

    /**
     * Copia con feedback visivo
     */
    static async copyWithFeedback(text, buttonElement = null) {
        const result = await this.copy(text);

        if (result.success) {
            if (buttonElement) {
                const originalText = buttonElement.textContent;
                buttonElement.textContent = '✅ Copiato!';
                buttonElement.disabled = true;

                setTimeout(() => {
                    buttonElement.textContent = originalText;
                    buttonElement.disabled = false;
                }, 2000);
            }

            window.NotifyService?.success('📋 Testo copiato negli appunti');
        } else {
            window.NotifyService?.error('❌ Errore nella copia: ' + result.error);
        }

        return result.success;
    }
}