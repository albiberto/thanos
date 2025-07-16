/**
 * JsonFormatter.js - Formatta le griglie in JSON per Battlesnake
 */

export class JsonFormatter {
    constructor() {
        this.formatOptions = {
            indent: 2
        };
    }

    /**
     * Converte una Grid in formato test Battlesnake
     */
    gridToTest(grid, testId) {
        const test = {
            Id: testId,
            Name: `Test-${testId}`,
            Expected: grid.expectedValue || 0,
            MoveRequest: this.createMoveRequest(grid)
        };

        return test;
    }

    /**
     * Crea l'oggetto MoveRequest
     */
    createMoveRequest(grid) {
        const snakes = this.extractSnakes(grid);
        const you = snakes.find(s => s.id === "thanos") || null;

        return {
            game: {
                id: `game-${grid.id}`,
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
                food: grid.analysis.food || [],
                hazards: grid.analysis.hazards || [],
                snakes: snakes
            },
            you: you
        };
    }

    /**
     * Estrae i serpenti dalla griglia
     */
    extractSnakes(grid) {
        const snakes = [];

        // Serpente del giocatore
        if (grid.analysis.myHead) {
            const body = [
                grid.analysis.myHead,
                ...(grid.analysis.myBody || [])
            ];

            if (grid.analysis.myTail) {
                body.push(grid.analysis.myTail);
            }

            snakes.push({
                id: "thanos",
                name: "Thanos",
                health: 100,
                body: body,
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

        // Serpenti nemici
        if (grid.analysis.enemyHeads) {
            grid.analysis.enemyHeads.forEach((head, index) => {
                const body = this.buildEnemyBody(head, grid.analysis.enemyBodies, grid);

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

        return snakes;
    }

    /**
     * Costruisce il corpo di un serpente nemico
     */
    buildEnemyBody(head, allBodies, grid) {
        const body = [head];
        const used = new Set([`${head.x},${head.y}`]);

        let current = head;
        const directions = [
            { dx: 0, dy: -1 }, { dx: 0, dy: 1 },
            { dx: -1, dy: 0 }, { dx: 1, dy: 0 }
        ];

        // Cerca segmenti adiacenti
        while (body.length < allBodies.length) {
            let found = false;

            for (const dir of directions) {
                const next = {
                    x: current.x + dir.dx,
                    y: current.y + dir.dy
                };

                const key = `${next.x},${next.y}`;
                if (used.has(key)) continue;

                const segment = allBodies.find(b =>
                    b.x === next.x && b.y === next.y
                );

                if (segment) {
                    body.push(segment);
                    used.add(key);
                    current = segment;
                    found = true;
                    break;
                }
            }

            if (!found) break;
        }

        return body;
    }

    /**
     * Formatta array di test
     */
    formatTests(tests) {
        return JSON.stringify(tests, null, this.formatOptions.indent);
    }

    /**
     * Valida un test
     */
    validateTest(test) {
        try {
            // Controlli base
            if (!test.Id || !test.Name || test.Expected === undefined) {
                return false;
            }

            if (!test.MoveRequest?.board || !test.MoveRequest?.game) {
                return false;
            }

            const board = test.MoveRequest.board;
            if (!board.width || !board.height || board.width < 1 || board.height < 1) {
                return false;
            }

            // Verifica che i body siano array
            if (board.snakes) {
                for (const snake of board.snakes) {
                    if (!Array.isArray(snake.body)) {
                        return false;
                    }
                }
            }

            return true;
        } catch (error) {
            return false;
        }
    }
}