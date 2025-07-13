/**
 * BattlesnakeJsonFormatter - Fixed version with direct array bodies and proper hazard extraction
 * Snake body remains as array for proper JSON serialization
 */
class BattlesnakeJsonFormatter {
    constructor() {
        this.formatOptions = {
            indent: 2
        };
    }

    /**
     * Format a single test object keeping body as array
     * @param {Object} test - The test object to format
     * @returns {Object} - Formatted test object
     */
    formatTest(test) {
        // Deep clone to avoid modifying original
        const formatted = JSON.parse(JSON.stringify(test));

        // Format snakes keeping body as array
        if (formatted.MoveRequest?.board?.snakes) {
            formatted.MoveRequest.board.snakes = formatted.MoveRequest.board.snakes.map(snake =>
                this.formatSnake(snake)
            );
        }

        // Format 'you' snake if exists
        if (formatted.MoveRequest?.you) {
            formatted.MoveRequest.you = this.formatSnake(formatted.MoveRequest.you);
        }

        return formatted;
    }

    /**
     * Format a snake object keeping body as array
     * @param {Object} snake - The snake object to format
     * @returns {Object} - Formatted snake object
     */
    formatSnake(snake) {
        if (!snake) return snake;

        const formattedSnake = { ...snake };

        // Ensure body is array, not string
        if (typeof snake.body === 'string') {
            try {
                // Parse string back to array
                formattedSnake.body = JSON.parse(snake.body);
            } catch (e) {
                // Invalid JSON string, convert to empty array
                formattedSnake.body = [];
            }
        } else if (Array.isArray(snake.body)) {
            // Already an array, keep it as is
            formattedSnake.body = snake.body;
        } else {
            // No body or invalid type
            formattedSnake.body = [];
        }

        return formattedSnake;
    }

    /**
     * Format an array of tests
     * @param {Array} tests - Array of test objects
     * @returns {Array} - Array of formatted test objects
     */
    formatTests(tests) {
        if (!Array.isArray(tests)) {
            throw new Error('Input must be an array of test objects');
        }

        return tests.map(test => this.formatTest(test));
    }

    /**
     * Extract hazards from grid cells
     * @param {Array} cells - 2D array of grid cells
     * @param {number} width - Grid width
     * @param {number} height - Grid height
     * @returns {Array} - Array of hazard positions
     */
    extractHazards(cells, width, height) {
        const hazards = [];

        for (let y = 0; y < height; y++) {
            for (let x = 0; x < width; x++) {
                if (cells[y] && cells[y][x] === '#') {
                    hazards.push({ x, y });
                }
            }
        }

        console.log(`🔥 Found ${hazards.length} hazards:`, hazards);
        return hazards;
    }

    /**
     * Extract food from grid cells
     * @param {Array} cells - 2D array of grid cells
     * @param {number} width - Grid width
     * @param {number} height - Grid height
     * @returns {Array} - Array of food positions
     */
    extractFood(cells, width, height) {
        const food = [];

        for (let y = 0; y < height; y++) {
            for (let x = 0; x < width; x++) {
                if (cells[y] && cells[y][x] === 'F') {
                    food.push({ x, y });
                }
            }
        }

        console.log(`🍎 Found ${food.length} food:`, food);
        return food;
    }

    /**
     * Convert grid data from Process Tab to test JSON format
     * @param {Object} grid - Grid object from Process Tab
     * @param {number} testId - Test ID
     * @returns {Object} - Formatted test object
     */
    gridToTest(grid, testId) {
        console.log(`🔧 Converting grid ${testId}:`, grid);

        // Extract hazards and food directly from grid cells (più affidabile dell'analysis)
        const hazards = this.extractHazards(grid.cells, grid.width, grid.height);
        const food = this.extractFood(grid.cells, grid.width, grid.height);

        // Build snakes array
        const snakes = [];

        // Add player snake if exists
        if (grid.analysis?.myHead) {
            const body = [grid.analysis.myHead, ...(grid.analysis.myBody || [])];
            snakes.push({
                id: "thanos",
                name: "Thanos",
                health: 100,
                body: body, // Keep as array, don't stringify
                head: grid.analysis.myHead,
                length: body.length,
                latency: 50,
                shout: "",
                squad: "",
                customizations: {
                    color: "#FF0000",
                    head: "default",
                    tail: "default"
                }
            });
        }

        // Add enemy snakes
        if (grid.analysis?.enemyHeads) {
            grid.analysis.enemyHeads.forEach((head, index) => {
                const enemyBodySegments = this.findEnemyBodySegments(head, grid.analysis.enemyBodies || [], grid.cells);
                const body = [head, ...enemyBodySegments];

                snakes.push({
                    id: `enemy-${index + 1}`,
                    name: `Enemy ${index + 1}`,
                    health: 100,
                    body: body, // Keep as array, don't stringify
                    head: head,
                    length: body.length,
                    latency: "50",
                    shout: "",
                    squad: "",
                    customizations: {
                        color: "#0000FF",
                        head: "default",
                        tail: "default"
                    }
                });
            });
        }

        // Build test object
        const test = {
            Id: testId,
            Name: `Test-${testId}`,
            Expected: grid.expectedValue || 0,
            MoveRequest: {
                game: {
                    id: `game-${testId}`,
                    ruleset: {
                        name: "standard",
                        version: "v1.0.0"
                    },
                    timeout: 500
                },
                turn: 5,
                board: {
                    height: grid.height,
                    width: grid.width,
                    food: food, // Usa i food estratti direttamente dalle celle
                    hazards: hazards, // Usa gli hazard estratti direttamente dalle celle
                    snakes: snakes
                },
                you: snakes.find(s => s.id === "thanos") || null
            }
        };

        console.log(`✅ Generated test ${testId} with ${hazards.length} hazards and ${food.length} food`);

        // Format but keep body as arrays
        return this.formatTest(test);
    }

    /**
     * Find connected body segments for an enemy snake
     * @param {Object} head - Enemy head position
     * @param {Array} allEnemyBodies - All enemy body positions
     * @param {Array} cells - Grid cells
     * @returns {Array} - Connected body segments
     */
    findEnemyBodySegments(head, allEnemyBodies, cells) {
        const segments = [];
        const used = new Set();

        const directions = [
            { x: 0, y: -1 }, // up
            { x: 0, y: 1 },  // down
            { x: -1, y: 0 }, // left
            { x: 1, y: 0 }   // right
        ];

        let currentPos = head;
        let foundSegment = true;

        while (foundSegment && segments.length < allEnemyBodies.length) {
            foundSegment = false;

            for (const dir of directions) {
                const nextPos = {
                    x: currentPos.x + dir.x,
                    y: currentPos.y + dir.y
                };

                const key = `${nextPos.x},${nextPos.y}`;
                if (used.has(key)) continue;

                const bodySegment = allEnemyBodies.find(b =>
                    b.x === nextPos.x && b.y === nextPos.y
                );

                if (bodySegment) {
                    segments.push(bodySegment);
                    used.add(key);
                    currentPos = bodySegment;
                    foundSegment = true;
                    break;
                }
            }
        }

        return segments;
    }

    /**
     * Generate clean JSON string with proper array formatting
     * @param {Array|Object} data - Data to stringify
     * @returns {string} - Clean JSON string
     */
    toJSON(data) {
        return JSON.stringify(data, null, this.formatOptions.indent);
    }

    /**
     * Validate test JSON structure
     * @param {Object} test - Test object to validate
     * @returns {boolean} - True if valid
     */
    validateTest(test) {
        try {
            // Check required fields
            if (!test.Id || !test.Name || test.Expected === undefined) {
                return false;
            }

            if (!test.MoveRequest?.board || !test.MoveRequest?.game) {
                return false;
            }

            // Check board dimensions
            const board = test.MoveRequest.board;
            if (!board.width || !board.height || board.width < 1 || board.height < 1) {
                return false;
            }

            // Check snakes have body as arrays
            if (board.snakes) {
                for (const snake of board.snakes) {
                    if (!Array.isArray(snake.body)) {
                        console.warn('Snake body should be an array, not string');
                        return false;
                    }
                }
            }

            return true;
        } catch (error) {
            console.error('Validation error:', error);
            return false;
        }
    }
}

// Export for global access
if (typeof window !== 'undefined') {
    window.BattlesnakeJsonFormatter = BattlesnakeJsonFormatter;
}