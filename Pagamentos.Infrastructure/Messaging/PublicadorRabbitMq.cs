using Pagamentos.Application.Abstraction;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Pagamentos.Infrastructure.Messaging
{
    internal static class JsonMensagem
    {
        public static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);
    }

    public sealed class PublicadorRabbitMq(ConexaoRabbitMq conexao) : IPublicarMensagens
    {
        public async Task PublicarAsync<T>(string fila, T mensagem, CancellationToken ct = default)
        {
            await using var canal = await conexao.CriarCanalAsync(ct);

            await canal.QueueDeclareAsync(
                queue: fila, durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: ct);

            var corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem, JsonMensagem.Opcoes);
            var propriedades = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json"
            };

            await canal.BasicPublishAsync(
                exchange: string.Empty, routingKey: fila, mandatory: false,
                basicProperties: propriedades, body: corpo, cancellationToken: ct);
        }
    }
}
