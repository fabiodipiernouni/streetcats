using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StreetCats.Client.Models.DTOs;

/// <summary>
/// Converter personalizzato per gestire la conversione da stringa a Guid
/// Gestisce sia stringhe che Guid dal JSON dell'API
/// </summary>
public class FlexibleGuidConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                var stringValue = reader.GetString();
                if (string.IsNullOrEmpty(stringValue))
                    return Guid.Empty;
                
                // Prova a parsare come Guid
                if (Guid.TryParse(stringValue, out var guidResult))
                    return guidResult;
                
                // Se non è un Guid valido, genera un nuovo Guid deterministico dalla stringa
                return GenerateGuidFromString(stringValue);
                
            case JsonTokenType.Null:
                return Guid.Empty;
                
            default:
                throw new JsonException($"Unable to convert token type {reader.TokenType} to Guid");
        }
    }

    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    /// <summary>
    /// Genera un Guid deterministico da una stringa
    /// Utile per ID MongoDB che sono stringhe
    /// Usa un algoritmo di hash semplice compatibile con browser/WebAssembly
    /// </summary>
    private static Guid GenerateGuidFromString(string input)
    {
        // Usa un algoritmo di hash semplice e deterministico compatibile con WebAssembly
        var hash = ComputeSimpleHash(input);
        
        // Crea un array di 16 byte per il Guid
        var guidBytes = new byte[16];
        
        // Riempi l'array con i byte dell'hash, ripetendo se necessario
        for (int i = 0; i < 16; i++)
        {
            guidBytes[i] = (byte)((hash >> ((i % 4) * 8)) & 0xFF);
        }
        
        // Assicurati che il primo byte non sia zero per evitare Guid.Empty
        if (guidBytes[0] == 0)
            guidBytes[0] = 1;
            
        return new Guid(guidBytes);
    }
    
    /// <summary>
    /// Calcola un hash semplice e deterministico della stringa
    /// Compatibile con tutti gli ambienti incluso WebAssembly
    /// </summary>
    private static uint ComputeSimpleHash(string input)
    {
        uint hash = 2166136261u; // FNV-1a offset basis
        
        foreach (char c in input)
        {
            hash ^= c;
            hash *= 16777619u; // FNV-1a prime
        }
        
        return hash;
    }
}

/// <summary>
/// Converter per Guid nullable
/// </summary>
public class FlexibleNullableGuidConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
            
        var guidConverter = new FlexibleGuidConverter();
        return guidConverter.Read(ref reader, typeof(Guid), options);
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString());
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}