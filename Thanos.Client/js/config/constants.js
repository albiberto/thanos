/**
 * constants.js - Costanti globali dell'applicazione
 */

// Storage keys
export const STORAGE_KEYS = {
    GRIDS: 'battlesnake_grids',
    SETTINGS: 'battlesnake_settings'
};

// Cell types per la griglia
export const CELL_TYPES = {
    MY_HEAD: 'H',
    MY_BODY: 'B',
    MY_TAIL: 'T',
    ENEMY_HEAD: 'E',
    ENEMY_BODY: 'b',
    FOOD: 'F',
    HAZARD: '#',
    EMPTY: '.',
    DIRECTION_UP: '^',
    DIRECTION_DOWN: 'v',
    DIRECTION_LEFT: '<',
    DIRECTION_RIGHT: '>'
};

// Mapping emoji -> codice cella
export const EMOJI_MAP = {
    '👽': CELL_TYPES.MY_HEAD,
    '💲': CELL_TYPES.MY_BODY,
    '🌀': CELL_TYPES.MY_TAIL,
    '😈': CELL_TYPES.ENEMY_HEAD,
    '⛔': CELL_TYPES.ENEMY_BODY,
    '🍎': CELL_TYPES.FOOD,
    '💀': CELL_TYPES.HAZARD,
    '⬛': CELL_TYPES.EMPTY,
    '⬆️': CELL_TYPES.DIRECTION_UP,
    '⬇️': CELL_TYPES.DIRECTION_DOWN,
    '⬅️': CELL_TYPES.DIRECTION_LEFT,
    '➡️': CELL_TYPES.DIRECTION_RIGHT
};

// Direzioni movimento
export const DIRECTIONS = {
    UP: { value: 1, dx: 0, dy: -1, symbol: '⬆️' },
    DOWN: { value: 2, dx: 0, dy: 1, symbol: '⬇️' },
    LEFT: { value: 4, dx: -1, dy: 0, symbol: '⬅️' },
    RIGHT: { value: 8, dx: 1, dy: 0, symbol: '➡️' }
};

// Expected value labels
export const EXPECTED_LABELS = {
    1: "⬆️ UP",
    2: "⬇️ DOWN",
    3: "⬆️⬇️ UP|DOWN",
    4: "⬅️ LEFT",
    5: "⬆️⬅️ UP|LEFT",
    6: "⬇️⬅️ DOWN|LEFT",
    7: "⬆️⬇️⬅️ UP|DOWN|LEFT",
    8: "➡️ RIGHT",
    9: "⬆️➡️ UP|RIGHT",
    10: "⬇️➡️ DOWN|RIGHT",
    11: "⬆️⬇️➡️ UP|DOWN|RIGHT",
    12: "⬅️➡️ LEFT|RIGHT",
    13: "⬆️⬅️➡️ UP|LEFT|RIGHT",
    14: "⬇️⬅️➡️ DOWN|LEFT|RIGHT",
    15: "⬆️⬇️⬅️➡️ ALL"
};

// Stati della griglia
export const GRID_STATUS = {
    IMPORTED: 'imported',
    PENDING: 'pending',
    READY: 'ready',
    FAILED: 'failed'
};

// Configurazione UI
export const UI_CONFIG = {
    GRID_MIN_SIZE: 3,
    GRID_MAX_SIZE: 25,
    DEFAULT_START_ID: 101,
    TOAST_DURATION: 3000,
    ANIMATION_DURATION: 300
};