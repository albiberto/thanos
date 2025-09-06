using System.Text.Json;
using System.Text.Json.Serialization;

namespace Thanos.SourceGen;

public class CoordinateArrayToUshortArrayConverter(int gridWidth) : JsonConverter<ushort[]>
{
    public override ushort[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected StartArray token");

        // --- PRIMO PASSAGGIO: CONTARE GLI ELEMENTI ---

        // Creiamo una copia del reader per il conteggio. È una struct, quindi la copia è veloce.
        var counterReader = reader;
        var count = 0;
        while (counterReader.Read() && counterReader.TokenType != JsonTokenType.EndArray)
            // Saltiamo l'intero oggetto interno per andare al successivo
            if (counterReader.TokenType == JsonTokenType.StartObject)
            {
                counterReader.Skip();
                count++;
            }

        // Se array è vuoto, restituiamo un array vuoto senza altre operazioni.
        if (count == 0)
        {
            // Dobbiamo comunque consumare il token EndArray dal reader originale
            reader.Read();
            return [];
        }

        // --- ALLOCAZIONE SINGOLA ---
        // Ora allochiamo un singolo array della dimensione esatta. Nessuna riallocazione, nessuna copia extra.
        var result = new ushort[count];
        var current_index = 0;

        // --- SECONDO PASSAGGIO: LEGGERE E CONVERTIRE ---
        // Usiamo il reader originale, che è ancora posizionato all'inizio dell'array.

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartObject token");

            int x = -1, y = -1;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString()!;
                    reader.Read();
                    if (propertyName.Equals("x", StringComparison.OrdinalIgnoreCase)) x = reader.GetInt32();
                    else if (propertyName.Equals("y", StringComparison.OrdinalIgnoreCase)) y = reader.GetInt32();
                }

            if (x != -1 && y != -1) result[current_index++] = (ushort)(y * gridWidth + x);
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, ushort[] value, JsonSerializerOptions options) => throw new NotImplementedException();
}