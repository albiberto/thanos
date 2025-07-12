/**
 * Metodo di sanitizzazione migliorato per il contenuto delle griglie BattleSnake
 * Versione più robusta e flessibile del metodo esistente
 */
class ImprovedSanitizer {
    constructor() {
        // Mappa caratteri/emoji a simboli standard
        this.charMap = new Map([
            // === EMOJI PRINCIPALI ===
            ['👽', 'H'], // Testa mia (My Head)
            ['💲', 'B'], // Corpo mio (My Body)
            ['😈', 'E'], // Testa nemico (Enemy Head)
            ['⛔', 'b'], // Corpo nemico (Enemy Body)
            ['🍎', 'F'], // Cibo (Food)
            ['💀', '#'], // Pericolo/Hazard
            ['⬛', '.'], // Spazio vuoto (Empty)

            // === DIREZIONI CON EMOJI ===
            ['⬆️', '^'], // Up
            ['⬇️', 'v'], // Down
            ['⬅️', '<'], // Left
            ['➡️', '>'], // Right

            // === DIREZIONI SENZA EMOJI ===
            ['⬆', '^'], // Up
            ['⬇', 'v'], // Down
            ['⬅', '<'], // Left
            ['➡', '>'], // Right

            // === SIMBOLI ALTERNATIVI ===
            ['❌', '.'], // Bloccato/Invalid -> Empty
            ['🔴', 'E'], // Alternativa per Enemy Head
            ['🟢', 'H'], // Alternativa per My Head
            ['🔵', 'b'], // Alternativa per Enemy Body
            ['🟡', 'F'], // Alternativa per Food
            ['⚫', '#'], // Alternativa per Hazard
            ['⚪', '.'], // Alternativa per Empty

            // === CARATTERI TESTUALI ===
            ['H', 'H'], // Head (già standard)
            ['B', 'B'], // Body (già standard)
            ['E', 'E'], // Enemy Head (già standard)
            ['b', 'b'], // Enemy Body (già standard)
            ['F', 'F'], // Food (già standard)
            ['#', '#'], // Hazard (già standard)
            ['.', '.'], // Empty (già standard)
            ['^', '^'], // Up (già standard)
            ['v', 'v'], // Down (già standard)
            ['<', '<'], // Left (già standard)
            ['>', '>'], // Right (già standard)

            // === SPAZI E CONTROLLO ===
            [' ', ' '],   // Spazio normale
            ['\t', '\t'], // Tab
            ['\n', '\n'], // Newline
            ['\r', '\r'], // Carriage return

            // === SIMBOLI NUMERICI PER DIREZIONI ===
            ['1', '^'], // 1 = Up
            ['2', 'v'], // 2 = Down
            ['3', '<'], // 3 = Left
            ['4', '>'], // 4 = Right

            // === SIMBOLI ALTERNATIVI COMUNI ===
            ['*', 'F'], // Asterisco come food
            ['X', '#'], // X maiuscola come hazard
            ['x', '#'], // x minuscola come hazard
            ['O', '.'], // O maiuscola come empty
            ['o', '.'], // o minuscola come empty
            ['0', '.'], // Zero come empty
        ]);

        // Pattern per identificare potenziali coordinate
        this.coordinatePattern = /^\(\d+,\s*\d+\)$/;

        // Caratteri validi dopo la sanitizzazione
        this.validChars = new Set(['H', 'B', 'E', 'b', 'F', '#', '.', '^', 'v', '<', '>', ' ', '\t', '\n', '\r']);
    }

    /**
     * Sanitizza il contenuto della griglia
     * @param {string} content - Contenuto grezzo della griglia
     * @param {Object} options - Opzioni di sanitizzazione
     * @returns {string} - Contenuto sanitizzato
     */
    sanitizeGridContent(content, options = {}) {
        const {
            preserveSpacing = true,           // Mantieni spaziatura originale
            normalizeWhitespace = false,      // Normalizza spazi multipli
            removeInvalidChars = true,        // Rimuovi caratteri non validi
            logUnknownChars = true,          // Log caratteri sconosciuti
            allowCoordinates = false         // Permetti coordinate tipo (x,y)
        } = options;

        if (!content || typeof content !== 'string') {
            return '';
        }

        let result = '';
        const unknownChars = new Set();
        let charCount = 0;

        // Itera attraverso ogni carattere (gestisce correttamente emoji multi-byte)
        for (const char of content) {
            charCount++;

            // Controlla se il carattere è mappato
            if (this.charMap.has(char)) {
                result += this.charMap.get(char);
                continue;
            }

            // Gestione spazi e whitespace
            if (this.isWhitespace(char)) {
                if (preserveSpacing) {
                    result += char;
                } else if (normalizeWhitespace && char === ' ') {
                    result += ' ';
                }
                continue;
            }

            // Se è già un carattere valido, mantienilo
            if (this.validChars.has(char)) {
                result += char;
                continue;
            }

            // Carattere sconosciuto
            if (removeInvalidChars) {
                if (logUnknownChars) {
                    unknownChars.add(char);
                }
                // Non aggiungere il carattere (rimuovilo)
                continue;
            } else {
                // Mantieni il carattere anche se sconosciuto
                result += char;
            }
        }

        // Log caratteri sconosciuti se richiesto
        if (logUnknownChars && unknownChars.size > 0) {
            console.warn('Caratteri sconosciuti trovati durante la sanitizzazione:',
                Array.from(unknownChars).join(', '));
        }

        // Post-processing
        if (normalizeWhitespace) {
            result = this.normalizeWhitespace(result);
        }

        return result;
    }

    /**
     * Controlla se un carattere è whitespace
     * @param {string} char - Carattere da controllare
     * @returns {boolean}
     */
    isWhitespace(char) {
        return /\s/.test(char);
    }

    /**
     * Normalizza gli spazi multipli
     * @param {string} content - Contenuto da normalizzare
     * @returns {string}
     */
    normalizeWhitespace(content) {
        return content
        .replace(/[ \t]+/g, '')          // Converti spazi/tab multipli in singolo spazio
        .replace(/\n\s*\n/g, '\n\n')      // Normalizza newline multipli
        .replace(/^\s+|\s+$/gm, '');      // Rimuovi spazi all'inizio/fine delle righe
    }

    /**
     * Aggiunge un nuovo mapping carattere -> simbolo standard
     * @param {string} char - Carattere da mappare
     * @param {string} standardChar - Simbolo standard corrispondente
     */
    addCharMapping(char, standardChar) {
        if (!this.validChars.has(standardChar)) {
            throw new Error(`Carattere standard '${standardChar}' non valido`);
        }
        this.charMap.set(char, standardChar);
    }

    /**
     * Rimuove un mapping
     * @param {string} char - Carattere da rimuovere dal mapping
     */
    removeCharMapping(char) {
        this.charMap.delete(char);
    }

    /**
     * Ottieni tutti i mapping correnti
     * @returns {Object} - Oggetto con tutti i mapping
     */
    getAllMappings() {
        return Object.fromEntries(this.charMap);
    }

    /**
     * Sanitizzazione specifica per griglie BattleSnake
     * Versione ottimizzata del metodo originale
     * @param {string} content - Contenuto della griglia
     * @returns {string} - Contenuto sanitizzato
     */
    sanitizeBattlesnakeGrid(content) {
        return this.sanitizeGridContent(content, {
            preserveSpacing: true,
            normalizeWhitespace: false,
            removeInvalidChars: true,
            logUnknownChars: true,
            allowCoordinates: false
        });
    }

    /**
     * Sanitizzazione aggressiva che normalizza anche gli spazi
     * @param {string} content - Contenuto della griglia
     * @returns {string} - Contenuto sanitizzato e normalizzato
     */
    sanitizeAndNormalize(content) {
        return this.sanitizeGridContent(content, {
            preserveSpacing: false,
            normalizeWhitespace: true,
            removeInvalidChars: true,
            logUnknownChars: true,
            allowCoordinates: false
        });
    }

    /**
     * Analizza il contenuto senza modificarlo per vedere cosa contiene
     * @param {string} content - Contenuto da analizzare
     * @returns {Object} - Statistiche del contenuto
     */
    analyzeContent(content) {
        const stats = {
            totalChars: 0,
            knownChars: 0,
            unknownChars: 0,
            whitespaceChars: 0,
            mappedChars: new Map(),
            unknownCharsList: new Set(),
            isEmpty: !content || content.trim().length === 0
        };

        if (stats.isEmpty) {
            return stats;
        }

        for (const char of content) {
            stats.totalChars++;

            if (this.isWhitespace(char)) {
                stats.whitespaceChars++;
                continue;
            }

            if (this.charMap.has(char) || this.validChars.has(char)) {
                stats.knownChars++;
                const mappedChar = this.charMap.get(char) || char;
                stats.mappedChars.set(mappedChar, (stats.mappedChars.get(mappedChar) || 0) + 1);
            } else {
                stats.unknownChars++;
                stats.unknownCharsList.add(char);
            }
        }

        return stats;
    }

    /**
     * Valida se una griglia sanitizzata è valida per BattleSnake
     * @param {string} sanitizedContent - Contenuto già sanitizzato
     * @returns {Object} - Risultato della validazione
     */
    validateSanitizedGrid(sanitizedContent) {
        const result = {
            isValid: true,
            errors: [],
            warnings: [],
            stats: {}
        };

        if (!sanitizedContent || !sanitizedContent.trim()) {
            result.isValid = false;
            result.errors.push('Contenuto vuoto dopo la sanitizzazione');
            return result;
        }

        const lines = sanitizedContent.split('\n').filter(line => line.trim());

        if (lines.length === 0) {
            result.isValid = false;
            result.errors.push('Nessuna riga valida trovata');
            return result;
        }

        // Controlla consistenza delle righe
        const firstLineLength = lines[0].split(/\s+/).filter(cell => cell).length;

        for (let i = 0; i < lines.length; i++) {
            const cells = lines[i].split(/\s+/).filter(cell => cell);

            if (cells.length !== firstLineLength) {
                result.warnings.push(`Riga ${i + 1}: ${cells.length} celle invece di ${firstLineLength}`);
            }
        }

        // Statistiche
        result.stats = {
            rows: lines.length,
            columns: firstLineLength,
            totalCells: lines.length * firstLineLength
        };

        return result;
    }
}

// === ESEMPIO DI UTILIZZO ===

// Crea un'istanza del sanitizer
const sanitizer = new ImprovedSanitizer();

// Esempio di contenuto con vari tipi di caratteri
const exampleContent = `
👽 💲 💲 ⬛ 🍎
😈 ⛔ ⛔ . F
⬆️ ⬇️ ⬅️ ➡️ #
* X o 0 💀
`;

console.log('=== ESEMPIO DI SANITIZZAZIONE ===');
console.log('Contenuto originale:');
console.log(exampleContent);

// Analizza il contenuto prima della sanitizzazione
const analysis = sanitizer.analyzeContent(exampleContent);
console.log('\nAnalisi del contenuto:', analysis);

// Sanitizza con opzioni standard
const sanitized = sanitizer.sanitizeBattlesnakeGrid(exampleContent);
console.log('\nContenuto sanitizzato:');
console.log(sanitized);

// Valida il risultato
const validation = sanitizer.validateSanitizedGrid(sanitized);
console.log('\nValidazione:', validation);

// === INTEGRAZIONE CON IL CODICE ESISTENTE ===

/**
 * Metodo di sostituzione per il ImportTabManager esistente
 * Mantiene la stessa interfaccia ma con funzionalità migliorate
 */
function improvedSanitizeGridContent(content) {
    const sanitizer = new ImprovedSanitizer();
    return sanitizer.sanitizeBattlesnakeGrid(content);
}

// Export per l'uso nei moduli
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ImprovedSanitizer, improvedSanitizeGridContent };
}

if (typeof window !== 'undefined') {
    window.ImprovedSanitizer = ImprovedSanitizer;
    window.improvedSanitizeGridContent = improvedSanitizeGridContent;
}