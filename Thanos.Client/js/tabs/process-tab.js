// Battlesnake Board Converter - Process Tab

class ProcessTab {
    constructor() {
        this.initialized = false;
    }

    // Initialize the process tab
    init() {
        if (this.initialized) return;

        this.setupEventListeners();
        this.updateUI();
        this.initialized = true;

        console.log('ProcessTab initialized');
    }

    // Setup event listeners
    setupEventListeners() {
        // Generate JSON button
        const generateBtn = document.getElementById('generateBtn');
        if (generateBtn) {
            generateBtn.addEventListener('click', () => this.generateAllJSON());
        }

        // Bulk action buttons
        this.setupBulkActionButtons();

        // Listen for boards imported event
        document.addEventListener('boardsImported', (event) => {
            this.onBoardsImported(event.detail);
        });
    }

    // Setup bulk action buttons
    setupBulkActionButtons() {
        const bulkButtons = [
            { selector: '[onclick*="setAllExpected(13)"]', value: 13 },
            { selector: '[onclick*="setAllExpected(15)"]', value: 15 },
            { selector: '[onclick*="setAllExpected(1)"]', value: 1 },
            { selector: '[onclick*="setAllExpected(2)"]', value: 2 },
            { selector: '[onclick*="setAllExpected(4)"]', value: 4 },
            { selector: '[onclick*="setAllExpected(8)"]', value: 8 },
            { selector: '[onclick*="setAllExpected(3)"]', value: 3 },
            { selector: '[onclick*="setAllExpected(12)"]', value: 12 },
            { selector: '[onclick*="setAllExpected(5)"]', value: 5 },
            { selector: '[onclick*="setAllExpected(9)"]', value: 9 }
        ];

        bulkButtons.forEach(({ selector, value }) => {
            const button = document.querySelector(selector);
            if (button) {
                button.addEventListener('click', () => this.setAllExpected(value));
            }
        });
    }

    // Update the entire UI
    updateUI() {
        this.updateHeader();
        this.updateBoardsList();
    }

    // Update header information
    updateHeader() {
        const boards = window.BattlesnakeCommon.getImportedBoards();
        const boardsCount = document.getElementById('boardsCount');
        const generateBtn = document.getElementById('generateBtn');

        if (boardsCount) {
            boardsCount.textContent = boards.length;
        }

        if (generateBtn) {
            generateBtn.disabled = boards.length === 0;
        }
    }

    // Update boards list
    updateBoardsList() {
        const container = document.getElementById('boardsList');
        if (!container) return;

        const boards = window.BattlesnakeCommon.getImportedBoards();
        const expectedValues = window.BattlesnakeCommon.getBoardExpectedValues();

        if (boards.length === 0) {
            container.innerHTML = `
                <div style="text-align: center; color: #6b7280; margin-top: 40px;">
                    No boards loaded. Use the Import tab to load boards first.
                </div>
            `;
            return;
        }

        container.innerHTML = '';

        boards.forEach((board, index) => {
            const preview = this.generateBoardPreview(board);
            const cardElement = this.createBoardCard(index, preview, expectedValues[index]);
            container.appendChild(cardElement);
        });
    }

    // Generate board preview text
    generateBoardPreview(gameState) {
        const board = gameState.board;
        const width = board.width;
        const height = board.height;

        let preview = `${width}x${height} board\n`;

        if (board.snakes.length > 0) {
            const snake = board.snakes[0];
            preview += `Snake: (${snake.head.x},${snake.head.y})`;
            if (snake.body.length > 1) {
                preview += ` + ${snake.body.length - 1} body`;
            }
            preview += `\n`;
        }

        if (board.food.length > 0) {
            preview += `Food: ${board.food.length} pieces\n`;
        }

        if (board.hazards.length > 0) {
            preview += `Hazards: ${board.hazards.length} pieces\n`;
        }

        return preview.trim();
    }

    // Create a board card element
    createBoardCard(index, preview, expectedValue) {
        const card = document.createElement('div');
        card.className = 'board-card';

        const expectedOption = window.BattlesnakeCommon.EXPECTED_OPTIONS.find(opt => opt.value === expectedValue) ||
            window.BattlesnakeCommon.EXPECTED_OPTIONS[12];

        card.innerHTML = `
            <div>
                <div style="font-weight: bold; margin-bottom: 8px; color: #374151;">
                    Board ${index + 1}
                </div>
                <div class="board-preview">${preview}</div>
            </div>
            <div class="board-controls">
                <div style="font-size: 12px; font-weight: bold; margin-bottom: 5px;">Expected:</div>
                <div style="background: #dcfce7; border: 1px solid #22c55e; border-radius: 4px; padding: 5px; text-align: center; margin-bottom: 8px;">
                    <div style="font-size: 10px; color: #22c55e; font-weight: bold;">${expectedValue}</div>
                    <div style="font-size: 12px; margin: 2px 0;">${expectedOption.icons}</div>
                    <div style="font-size: 8px; color: #6b7280;">${expectedOption.desc}</div>
                </div>
                <div class="expected-mini-grid" id="miniGrid${index}"></div>
            </div>
        `;

        // Setup mini grid after DOM insertion
        setTimeout(() => this.createMiniExpectedGrid(index, expectedValue), 0);

        return card;
    }

    // Create mini expected value grid for a board
    createMiniExpectedGrid(boardIndex, currentValue) {
        const container = document.getElementById(`miniGrid${boardIndex}`);
        if (!container) return;

        container.innerHTML = '';

        // Show only most common values in mini grid
        const commonValues = [1, 2, 4, 8, 3, 12, 13, 15];

        commonValues.forEach(value => {
            const option = window.BattlesnakeCommon.EXPECTED_OPTIONS.find(opt => opt.value === value);
            if (!option) return;

            const div = document.createElement('div');
            div.className = 'expected-mini-option';
            if (value === currentValue) {
                div.classList.add('selected');
            }

            div.innerHTML = `
                <div style="font-size: 8px; font-weight: bold;">${value}</div>
                <div style="font-size: 8px;">${option.icons}</div>
            `;

            div.addEventListener('click', () => {
                window.BattlesnakeCommon.setBoardExpectedValue(boardIndex, value);
                this.updateBoardsList();
            });

            container.appendChild(div);
        });
    }

    // Set all boards to same expected value
    setAllExpected(value) {
        const boards = window.BattlesnakeCommon.getImportedBoards();

        if (boards.length === 0) {
            window.BattlesnakeCommon.showStatus('❌ Nessuna board caricata', true);
            return;
        }

        window.BattlesnakeCommon.setAllBoardExpectedValues(value);
        this.updateBoardsList();
        window.BattlesnakeCommon.showStatus(`✅ Tutte le board impostate a ${value}`);
    }

    // Generate JSON for all boards
    generateAllJSON() {
        try {
            const boards = window.BattlesnakeCommon.getImportedBoards();
            const expectedValues = window.BattlesnakeCommon.getBoardExpectedValues();

            if (boards.length === 0) {
                throw new Error('Nessuna board caricata');
            }

            const startingTestId = parseInt(document.getElementById('processTestId')?.value || '101');
            const testNameBase = document.getElementById('processTestName')?.value?.trim() || 'Test';

            if (!testNameBase) {
                throw new Error('Inserisci un nome base per i test');
            }

            let jsonOutput;

            if (boards.length === 1) {
                // Single board - output as single object
                const testCase = {
                    Id: startingTestId,
                    Name: testNameBase,
                    Expected: expectedValues[0],
                    MoveRequest: boards[0]
                };
                jsonOutput = window.BattlesnakeCommon.formatSingleJSON(testCase);
            } else {
                // Multiple boards - output as array
                const testCases = boards.map((gameState, index) => ({
                    Id: startingTestId + index,
                    Name: `${testNameBase} ${index + 1}`,
                    Expected: expectedValues[index],
                    MoveRequest: gameState
                }));

                const formattedCases = testCases.map(testCase =>
                    window.BattlesnakeCommon.formatSingleJSON(testCase)
                );
                jsonOutput = '[\n' + formattedCases.map(json =>
                    json.replace(/^/gm, '  ')
                ).join(',\n') + '\n]';
            }

            // Display output
            const jsonCodeElement = document.getElementById('jsonCode');
            const jsonOutputElement = document.getElementById('jsonOutput');

            if (jsonCodeElement && jsonOutputElement) {
                jsonCodeElement.textContent = jsonOutput;
                jsonOutputElement.style.display = 'flex';
            }

            const message = boards.length === 1 ?
                '✅ JSON generato!' :
                `✅ Generato JSON per ${boards.length} board!`;
            window.BattlesnakeCommon.showStatus(message);

        } catch (error) {
            window.BattlesnakeCommon.showStatus(`❌ Errore: ${error.message}`, true);
            console.error('Generation error:', error);
        }
    }

    // Called when boards are imported
    onBoardsImported(detail) {
        this.updateUI();
    }

    // Called when tab becomes active
    onActivate() {
        this.updateUI();
    }

    // Get current configuration
    getConfiguration() {
        return {
            startingTestId: parseInt(document.getElementById('processTestId')?.value || '101'),
            testNameBase: document.getElementById('processTestName')?.value?.trim() || 'Test',
            boardCount: window.BattlesnakeCommon.getImportedBoards().length
        };
    }
}

// Create process tab instance
const processTab = new ProcessTab();

// Global functions for backward compatibility
function setAllExpected(value) {
    processTab.setAllExpected(value);
}

function generateAllJSON() {
    processTab.generateAllJSON();
}

// Auto-register with tab manager when available
document.addEventListener('DOMContentLoaded', () => {
    if (window.BattlesnakeTabManager) {
        window.BattlesnakeTabManager.tabManager.registerTab('process', processTab);
    }
    processTab.init();
});

// Export for module use
window.BattlesnakeProcessTab = {
    processTab,
    setAllExpected,
    generateAllJSON
};