// Battlesnake Board Converter - Common Utilities

// Global constants
const EXPECTED_OPTIONS = [
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

// Global state
let importedBoards = [];
let boardExpectedValues = [];

// Utility Functions
function showStatus(message, isError = false) {
    const statusDiv = document.getElementById('statusMessage');
    statusDiv.innerHTML = `<div class="status-message ${isError ? 'status-error' : 'status-success'}">${message}</div>`;
    setTimeout(() => {
        statusDiv.innerHTML = '';
    }, 3000);
}

async function copyIcon(icon) {
    try {
        await navigator.clipboard.writeText(icon);
        showStatus(`📋 Icona ${icon} copiata!`);
    } catch (error) {
        showStatus('❌ Errore nella copia: ' + error.message, true);
    }
}

async function copyToClipboard(elementId, buttonId) {
    try {
        const jsonText = document.getElementById(elementId).textContent;
        await navigator.clipboard.writeText(jsonText);

        if (buttonId) {
            const button = document.getElementById(buttonId);
            const originalText = button.textContent;
            button.textContent = '✅ Copiato!';

            setTimeout(() => {
                button.textContent = originalText;
            }, 2000);
        }

        showStatus('📋 JSON copiato!');

    } catch (error) {
        showStatus('❌ Errore nella copia: ' + error.message, true);
    }
}

// Board Parsing Functions
function isAdjacent(pos1, pos2) {
    const dx = Math.abs(pos1.x - pos2.x);
    const dy = Math.abs(pos1.y - pos2.y);
    return (dx === 1 && dy === 0) || (dx === 0 && dy === 1);
}

function findSnakePath(head, bodySegments) {
    if (bodySegments.length === 0) return [head];

    const path = [head];
    const remaining = [...bodySegments];

    while (remaining.length > 0) {
        const current = path[path.length - 1];
        let nextIndex = -1;

        // Find the next adjacent segment
        for (let i = 0; i < remaining.length; i++) {
            if (isAdjacent(current, remaining[i])) {
                nextIndex = i;
                break;
            }
        }

        if (nextIndex === -1) {
            // If no adjacent segment found, take the closest one
            let minDistance = Infinity;
            for (let i = 0; i < remaining.length; i++) {
                const distance = Math.abs(current.x - remaining[i].x) + Math.abs(current.y - remaining[i].y);
                if (distance < minDistance) {
                    minDistance = distance;
                    nextIndex = i;
                }
            }
        }

        if (nextIndex !== -1) {
            path.push(remaining[nextIndex]);
            remaining.splice(nextIndex, 1);
        } else {
            break;
        }
    }

    // Add any remaining segments that couldn't be connected
    path.push(...remaining);

    return path;
}

function parseBoard(boardText) {
    const lines = boardText.split('\n').filter(line => line.trim());
    const gridLines = lines.filter(line => line.match(/^\s*\d+/));

    if (gridLines.length === 0) {
        throw new Error('Nessuna riga della griglia trovata');
    }

    const snakes = [];
    let bodySegments = [];
    let head = null;
    const food = [];
    const hazards = [];

    // Use actual row number instead of index
    for (let i = 0; i < gridLines.length; i++) {
        const line = gridLines[i];
        const parts = line.trim().split(/\s+/).filter(part => part.length > 0);
        const rowNumber = parseInt(parts[0]); // Extract actual row number
        const cells = parts.slice(1).filter(cell => cell.trim().length > 0); // Remove row number and empty cells

        for (let x = 0; x < cells.length; x++) {
            const cell = cells[x];

            if (cell === '👽') {
                head = { x, y: rowNumber };
            } else if (cell === '💲') {
                const bodyPart = { x, y: rowNumber };
                bodySegments.push(bodyPart);
            } else if (cell === '🍎' || cell === '🟢') {
                food.push({ x, y: rowNumber });
            } else if (cell === '💀' || cell === '🔥' || cell === '⚠️') {
                hazards.push({ x, y: rowNumber });
            }
        }
    }

    if (head) {
        // Create properly ordered snake body from head to tail
        const snakeBody = findSnakePath(head, bodySegments);

        snakes.push({
            id: "thanos",
            name: "Thanos",
            health: 100,
            body: snakeBody,
            latency: 50,
            head: head,
            length: snakeBody.length,
            shout: "",
            squad: "",
            customizations: {
                color: "#FF0000",
                head: "default",
                tail: "default"
            }
        });
    }

    // Calculate height and width correctly
    const widths = gridLines.map(line => {
        const parts = line.trim().split(/\s+/).filter(part => part.length > 0);
        const cells = parts.slice(1); // Remove row number
        const filteredCells = cells.filter(cell => cell.trim().length > 0); // Remove empty cells
        return filteredCells.length;
    });
    const width = Math.max(...widths);

    const heights = gridLines.map(line => {
        const parts = line.split(/\s+/);
        return parseInt(parts[0]);
    });
    const height = Math.max(...heights) + 1; // +1 because it starts from 0

    return {
        game: {
            id: `game-${Math.floor(Math.random() * 1000)}`,
            ruleset: {
                name: "standard"
            },
            timeout: 500
        },
        turn: 5,
        board: {
            height: height,
            width: width,
            food: food,
            hazards: hazards,
            snakes: snakes
        }
    };
}

function parseMultipleBoards(inputText) {
    // Split input into sections separated by multiple empty lines or separators
    const sections = inputText.split(/\n\s*\n|\n.*?---.*?\n/).filter(section => section.trim());

    if (sections.length === 1) {
        // Single input - original behavior
        return [parseBoard(sections[0])];
    }

    // Multiple grids
    const boards = [];
    for (let i = 0; i < sections.length; i++) {
        const section = sections[i].trim();
        if (section && section.match(/^\s*\d+/m)) { // Verify it contains a grid
            try {
                const board = parseBoard(section);
                boards.push(board);
            } catch (error) {
                console.warn(`Error parsing grid ${i + 1}:`, error);
            }
        }
    }

    return boards;
}

// JSON Formatting Functions
function formatSnakeBody(body) {
    return body.map(pos => `{"x":${pos.x},"y":${pos.y}}`).join(',');
}

function formatSingleJSON(jsonData) {
    const formattedData = JSON.parse(JSON.stringify(jsonData));

    if (formattedData.MoveRequest && formattedData.MoveRequest.board && formattedData.MoveRequest.board.snakes) {
        formattedData.MoveRequest.board.snakes.forEach(snake => {
            if (snake.body && Array.isArray(snake.body)) {
                snake.body = `[${formatSnakeBody(snake.body)}]`;
            }
        });
    }

    let output = JSON.stringify(formattedData, null, 2);
    output = output.replace(/"body"\s*:\s*"\[(.*?)\]"/g, (match, bodyContent) => {
        const decodedContent = bodyContent.replace(/\\"/g, '"');
        return `"body": [${decodedContent}]`;
    });

    return output;
}

// State Management Functions
function getImportedBoards() {
    return importedBoards;
}

function getBoardExpectedValues() {
    return boardExpectedValues;
}

function setImportedBoards(boards) {
    importedBoards = boards;
    boardExpectedValues = new Array(boards.length).fill(13); // Default to UP|LEFT|RIGHT
}

function setBoardExpectedValue(index, value) {
    if (index >= 0 && index < boardExpectedValues.length) {
        boardExpectedValues[index] = value;
    }
}

function setAllBoardExpectedValues(value) {
    boardExpectedValues.fill(value);
}

// Export functions for module use
window.BattlesnakeCommon = {
    // Constants
    EXPECTED_OPTIONS,

    // Utilities
    showStatus,
    copyIcon,
    copyToClipboard,

    // Board parsing
    parseBoard,
    parseMultipleBoards,

    // JSON formatting
    formatSingleJSON,

    // State management
    getImportedBoards,
    getBoardExpectedValues,
    setImportedBoards,
    setBoardExpectedValue,
    setAllBoardExpectedValues
};