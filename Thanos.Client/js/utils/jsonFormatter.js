/**
 * JsonFormatter.js - Formatta le griglie in JSON per Battlesnake
 * Con ordinamento del corpo dei serpenti dalla testa alla coda
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
            const body = this.buildMySnakeBody(grid);

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
                const body = this.buildEnemySnakeBody(head, grid);

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
     * Costruisce il corpo del mio serpente ordinato dalla testa alla coda
     */
    buildMySnakeBody(grid) {
        const body = [grid.analysis.myHead];
        const myBodySegments = [...(grid.analysis.myBody || [])];
        const myTail = grid.analysis.myTail;

        // Se non ci sono segmenti del corpo, torna solo la testa
        if (myBodySegments.length === 0 && !myTail) {
            return body;
        }

        // Ordina i segmenti del corpo
        const orderedSegments = this.orderBodySegments(
            grid.analysis.myHead,
            myBodySegments,
            myTail,
            grid
        );

        // Aggiungi i segmenti ordinati
        body.push(...orderedSegments);

        // Aggiungi la coda se esiste e non è già inclusa
        if (myTail && !orderedSegments.some(seg => seg.x === myTail.x && seg.y === myTail.y)) {
            body.push(myTail);
        }

        return body;
    }

    /**
     * Costruisce il corpo di un serpente nemico ordinato
     */
    buildEnemySnakeBody(head, grid) {
        const body = [head];
        const allEnemyBodies = grid.analysis.enemyBodies || [];

        if (allEnemyBodies.length === 0) {
            return body;
        }

        // Trova tutti i segmenti connessi a questa testa
        const connectedSegments = this.findConnectedSegments(head, allEnemyBodies, grid);

        // Ordina i segmenti
        const orderedSegments = this.orderBodySegments(head, connectedSegments, null, grid);

        body.push(...orderedSegments);
        return body;
    }

    /**
     * Trova i segmenti del corpo connessi a una testa
     */
    findConnectedSegments(head, allBodies, grid) {
        const connected = [];
        const visited = new Set([`${head.x},${head.y}`]);
        const toVisit = [head];

        while (toVisit.length > 0) {
            const current = toVisit.shift();

            // Controlla tutte le direzioni
            const directions = [
                { dx: 0, dy: -1 }, { dx: 0, dy: 1 },
                { dx: -1, dy: 0 }, { dx: 1, dy: 0 }
            ];

            for (const dir of directions) {
                const nx = current.x + dir.dx;
                const ny = current.y + dir.dy;
                const key = `${nx},${ny}`;

                if (visited.has(key)) continue;

                const segment = allBodies.find(b => b.x === nx && b.y === ny);
                if (segment) {
                    connected.push(segment);
                    visited.add(key);
                    toVisit.push(segment);
                }
            }
        }

        return connected;
    }

    /**
     * Ordina i segmenti del corpo dalla testa alla coda
     */
    orderBodySegments(head, segments, tail, grid) {
        if (segments.length === 0) return [];
        if (segments.length === 1) return segments;

        const ordered = [];
        const used = new Set();
        let current = head;

        // Direzioni possibili
        const directions = [
            { dx: 0, dy: -1 }, { dx: 0, dy: 1 },
            { dx: -1, dy: 0 }, { dx: 1, dy: 0 }
        ];

        // Costruisci il percorso
        while (ordered.length < segments.length) {
            let nextSegment = null;

            // Cerca il prossimo segmento adiacente
            for (const dir of directions) {
                const nx = current.x + dir.dx;
                const ny = current.y + dir.dy;

                const candidate = segments.find(seg =>
                    seg.x === nx &&
                    seg.y === ny &&
                    !used.has(`${seg.x},${seg.y}`)
                );

                if (candidate) {
                    nextSegment = candidate;
                    break;
                }
            }

            if (!nextSegment) {
                // Se non troviamo un segmento adiacente, aggiungi i rimanenti
                // (questo può succedere con serpenti frammentati)
                segments.forEach(seg => {
                    if (!used.has(`${seg.x},${seg.y}`)) {
                        ordered.push(seg);
                        used.add(`${seg.x},${seg.y}`);
                    }
                });
                break;
            }

            ordered.push(nextSegment);
            used.add(`${nextSegment.x},${nextSegment.y}`);
            current = nextSegment;
        }

        return ordered;
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

                    // Verifica che il body sia ordinato (ogni segmento è adiacente al precedente)
                    for (let i = 1; i < snake.body.length; i++) {
                        const prev = snake.body[i - 1];
                        const curr = snake.body[i];
                        const distance = Math.abs(prev.x - curr.x) + Math.abs(prev.y - curr.y);

                        if (distance > 1) {
                            console.warn(`Snake ${snake.id} has non-adjacent body segments at index ${i}`);
                        }
                    }
                }
            }

            return true;
        } catch (error) {
            return false;
        }
    }
}