using Thanos.War.Snake;
using static NUnit.Framework.Assert;

namespace Thanos.Tests.Tests.WarSnakeTests;

[TestFixture]
public class WarSnakeTests
{
    // --- Test dei Costruttori ---

    [Test(Description = "Verifica che il costruttore principale inizializzi correttamente tutti gli stati interni.")]
    public void MainConstructor_ShouldInitializeAllStatesCorrectly()
    {
        // Arrange
        const int capacity = 16;
        const int initialHp = 90;
        const int snakeId = 42;
        var initialBody = new ushort[] { 1, 2, 3, 4 };

        var context = Harness.CreateTestContext(capacity, initialBody);

        // Act
        var snake = new WarSnake(ref context.Health, ref context.Anatomy, context.BodyBuffer, snakeId, initialHp, initialBody, capacity);

        // Assert
        That(snake.Id, Is.EqualTo(snakeId));
        That(snake.Length, Is.EqualTo(initialBody.Length));
        That(snake.Dead, Is.False);
        That(snake.Tail, Is.EqualTo(1));
        That(snake.Head, Is.EqualTo(4));

        var bodySlice = context.BodyBuffer.AsSpan(0, initialBody.Length);
        That(bodySlice.ToArray(), Is.EqualTo(initialBody).AsCollection);
    }

    [Test(Description = "Verifica che il costruttore 'viewer' si agganci correttamente a uno stato preesistente.")]
    public void ViewerConstructor_ShouldCorrectlyViewExistingState()
    {
        // Arrange
        // 1. Creiamo uno stato "manuale"
        var context = new Harness.SnakeTestContext
        {
            Health = new Health(50), // HP a metà
            Anatomy = new Anatomy(16, 5), // L=5, Coda all'indice 2
            BodyBuffer = new ushort[16]
        };

        // Posizioniamo manualmente la testa per coerenza
        context.BodyBuffer[(5 - 1) & 15] = 99; // Head = 99

        // Act
        // 2. Creiamo la vista WarSnake su questo stato
        var snake = new WarSnake(ref context.Health, ref context.Anatomy, context.BodyBuffer);

        // Assert
        // 3. Verifichiamo che la vista rifletta correttamente lo stato manuale
        That(snake.Length, Is.EqualTo(5));
        That(snake.Dead, Is.False);
        That(snake.Head, Is.EqualTo(99));
        That(snake.Tail, Is.EqualTo(0));
    }
    //
    // --- Test del Metodo Move ---

    [Test(Description = "Verifica che Move() senza mangiare sposti il serpente e non ne cambi la lunghezza.")]
    public void Move_WhenNotEating_ShouldShiftBody()
    {
        // Arrange
        var initialBody = new ushort[] { 10, 20, 30 };
        var context = Harness.CreateTestContext(16, initialBody);
        var snake = new WarSnake(ref context.Health, ref context.Anatomy, context.BodyBuffer, 1, 100, initialBody, 16);

        // Act
        snake.Move(40, false, 1);

        // Assert
        That(snake.Length, Is.EqualTo(3), "La lunghezza non deve cambiare.");
        That(snake.Tail, Is.EqualTo(20), "La coda deve avanzare.");
        That(snake.Head, Is.EqualTo(40), "La testa deve essere il nuovo valore.");
        That(context.BodyBuffer[3], Is.EqualTo(40), "Il buffer deve contenere la nuova testa.");
    }

    [Test(Description = "Verifica che Move() mangiando aumenti la lunghezza del serpente e non sposti la coda.")]
    public void Move_WhenEating_ShouldIncreaseLength()
    {
        // Arrange
        var initialBody = new ushort[] { 10, 20, 30 };
        var context = Harness.CreateTestContext(16, initialBody);
        var snake = new WarSnake(ref context.Health, ref context.Anatomy, context.BodyBuffer, 1, 100, initialBody, 16);

        // Act
        snake.Move(40, true, 0);

        // Assert
        That(snake.Length, Is.EqualTo(4), "La lunghezza deve aumentare di 1.");
        That(snake.Tail, Is.EqualTo(10), "La coda non deve muoversi quando si mangia.");
        That(snake.Head, Is.EqualTo(40), "La testa deve essere il nuovo valore.");
    }

    // --- Test del Metodo GetSpans ---

    [Test(Description = "Verifica che GetSpans() restituisca un solo span se il corpo è contiguo.")]
    public void GetSpans_WhenBodyIsContiguous_ShouldReturnOneSpan()
    {
        // Arrange
        var initialBody = new ushort[] { 10, 20, 30 };
        var context = Harness.CreateTestContext(16, initialBody);
        // Creiamo uno stato con TailIndex non a zero per un test più robusto
        var anatomy = new Anatomy(16, initialBody.Length);
        var snake = new WarSnake(ref context.Health, ref anatomy, context.BodyBuffer);

        // Copiamo manualmente il corpo nella posizione corretta
        initialBody.AsSpan().CopyTo(context.BodyBuffer.AsSpan());

        // Act
        snake.GetSpans(out var first, out var second);

        // Assert
        That(first.Length, Is.EqualTo(initialBody.Length));
        That(second.IsEmpty, Is.True);
        That(first.ToArray(), Is.EqualTo(initialBody).AsCollection);
    }

    [Test(Description = "Verifica che GetSpans() restituisca due span se il corpo è a cavallo del buffer.")]
    public void GetSpans_WhenBodyIsWrapped_ShouldReturnTwoSpans()
    {
        // --- ARRANGE ---

        const int capacity = 16;
        const int length = 8;
        var initialBody = new ushort[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        // 1. Creiamo il contesto e un'istanza di Anatomy.
        //    Per la nuova regola, TailIndex partirà SEMPRE da 0.
        var context = Harness.CreateTestContext(capacity, initialBody);
        var anatomy = new Anatomy(capacity, length);

        // 2. Facciamo "evolvere" lo stato.
        //    Vogliamo che la coda arrivi all'indice 12 per creare un wrap.
        //    Dato che parte da 0, dobbiamo chiamare PopTail() 12 volte.
        var desiredTailIndex = 12;
        for (var i = 0; i < desiredTailIndex; i++) anatomy.PopTail();
        // Ora, anatomy.TailIndex è 12 e anatomy.Length è 8. Questo è lo stato che vogliamo testare.

        // 3. Creiamo la vista WarSnake e sistemiamo la memoria PERCHÉ CORRISPONDA allo stato di Anatomy.
        var snake = new WarSnake(ref context.Health, ref anatomy, context.BodyBuffer);

        // Il corpo (valori 1..8) ora deve essere in [12,13,14,15] e [0,1,2,3]
        initialBody.AsSpan(0, 4).CopyTo(context.BodyBuffer.AsSpan(12));
        initialBody.AsSpan(4, 4).CopyTo(context.BodyBuffer.AsSpan(0));

        // --- ACT ---
        snake.GetSpans(out var first, out var second);

        // --- ASSERT ---
        That(first.Length, Is.EqualTo(4), "La prima parte va dalla coda (12) alla fine.");
        That(second.Length, Is.EqualTo(4), "La seconda parte va dall'inizio alla testa.");

        That(first.ToArray(), Is.EqualTo(new ushort[] { 1, 2, 3, 4 }).AsCollection);
        That(second.ToArray(), Is.EqualTo(new ushort[] { 5, 6, 7, 8 }).AsCollection);
    }
}