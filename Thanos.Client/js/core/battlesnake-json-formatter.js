/**
 * BattlesnakeJsonFormatter - Shared JSON formatting class
 * Ensures consistent JSON format between Formatter and Process tabs
 */
class BattlesnakeJsonFormatter {
    constructor() {
        // Snake body should be formatted as inline JSON string
        this.formatOptions = {
            inlineBody: true,
            indent: 2
        };
    }

    /**
     * Format a single test object with inline body
     * @param {Object} test - The test object to format
     * @returns {Object} - Formatted test object
     */
    formatTest(test) {
        // Deep clone to avoid modifying original
        const formatted = JSON.parse(JSON.stringify(test));

        // Format snakes with inline body
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
     * Format a snake object with inline body
     * @param {Object} snake - The snake object to format
     * @returns {Object} - Formatted snake object
     */
    formatSnake(snake) {
        if (!snake) return snake;

        const formattedSnake = { ...snake };

        // Convert body array to JSON string
        if (Array.isArray(snake.body)) {
            formattedSnake.body = JSON.stringify(snake.body);
        } else if (typeof snake.body === 'string') {
            // Already a string, ensure it's valid JSON
            try {
                JSON.parse(snake.body);
                formattedSnake.body = snake.body;
            } catch (e) {
                // Invalid JSON string, convert to empty array
                formattedSnake.body = "[]";
            }
        } else {
            // No body or invalid type
            formattedSnake.body = "[]";
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
     * Parse snake body from JSON string to array
     * @param {Object} snake - Snake object with body as JSON string
     * @returns {Object} - Snake object with body as array
     */
    parseSnake(snake) {
        if (!snake) return snake;

        const parsedSnake = { ...snake };

        // Convert body JSON string to array
        if (typeof snake.body === 'string') {
            try {
                parsedSnake.body = JSON.parse(snake.body);
            } catch (e) {
                console.error('Failed to parse snake body:', e);
                parsedSnake.body = [];
            }
        }

        return parsedSnake;
    }

    /**
     * Parse test object converting body strings back to arrays
     * @param {Object} test - Test object with inline body strings
     * @returns {Object} - Test object with body arrays
     */
    parseTest(test) {
        const parsed = JSON.parse(JSON.stringify(test));

        // Parse snakes
        if (parsed.MoveRequest?.board?.snakes) {
            parsed.MoveRequest.board.snakes = parsed.MoveRequest.board.snakes.map(snake =>
                this.parseSnake(snake)
            );
        }

        // Parse 'you' snake
        if (parsed.MoveRequest?.you) {
            parsed.MoveRequest.you = this.parseSnake(parsed.MoveRequest.you);
        }

        return parsed;
    }

    /**
     * Convert grid data from Process Tab to test JSON format
     * @param {Object} grid - Grid object from Process Tab
     * @param {number} testId - Test ID
     * @returns {Object} - Formatted test object
     */
    gridToTest(grid, testId) {
        // Build snakes array
        const snakes = [];

        // Add player snake if exists
        if (grid.analysis?.myHead) {
            const body = [grid.analysis.myHead, ...(grid.analysis.myBody || [])];
            snakes.push({
                id: "thanos",
                name: "Thanos",
                health: 100,
                body: body,
                head: grid.analysis.myHead,
                length: body.length,
                latency: "50",
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
                // Find corresponding body segments for this enemy
                const enemyBodySegments = this.findEnemyBodySegments(head, grid.analysis.enemyBodies || [], grid.cells);
                const body = [head, ...enemyBodySegments];

                snakes.push({
                    id: `enemy-${index + 1}`,
                    name: `Enemy ${index + 1}`,
                    health: 100,
                    body: body,
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
                    food: grid.analysis?.food || [],
                    hazards: grid.analysis?.hazards || [],
                    snakes: snakes
                },
                you: snakes.find(s => s.id === "thanos") || null
            }
        };

        // Format with inline body
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
        // Simple heuristic: find body segments adjacent to head
        // In a real implementation, you'd want to trace the snake path
        const segments = [];
        const used = new Set();

        // Check adjacent positions
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
     * Generate JSON string with proper formatting
     * @param {Array|Object} data - Data to stringify
     * @returns {string} - Formatted JSON string
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

            // Check snakes have inline body strings
            if (board.snakes) {
                for (const snake of board.snakes) {
                    if (typeof snake.body !== 'string') {
                        console.warn('Snake body should be JSON string');
                        return false;
                    }
                    // Validate body string is valid JSON
                    try {
                        JSON.parse(snake.body);
                    } catch (e) {
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