using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pagamentos.Application.Messages;
using Pagamentos.Application.UseCases;
using Pagamentos.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Pagamentos.Infrastructure.Messaging
{
    public abstract class ConsumidorRabbitMq<TMensagem>(
    ConexaoRabbitMq conexao,
    IServiceScopeFactory fabricaEscopo,
    ILogger logger,
    string fila) : BackgroundService
    {
        protected abstract Task TratarAsync(TMensagem mensagem, IServiceProvider servicos, CancellationToken ct);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var canal = await conexao.CriarCanalAsync(stoppingToken);

                    await canal.QueueDeclareAsync(
                        queue: fila, durable: true, exclusive: false, autoDelete: false,
                        arguments: null, cancellationToken: stoppingToken);

                    await canal.BasicQosAsync(0, 10, false, stoppingToken);

                    var consumidor = new AsyncEventingBasicConsumer(canal);
                    consumidor.ReceivedAsync += async (_, ea) =>
                    {
                        try
                        {
                            var mensagem = JsonSerializer.Deserialize<TMensagem>(ea.Body.Span, JsonMensagem.Opcoes)
                                           ?? throw new InvalidOperationException("Mensagem vazia.");

                            using var escopo = fabricaEscopo.CreateScope();
                            await TratarAsync(mensagem, escopo.ServiceProvider, stoppingToken);

                            await canal.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            logger.LogError(ex, "Falha ao processar mensagem da fila {Fila}.", fila);
                            await canal.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                        }
                    };

                    await canal.BasicConsumeAsync(fila, autoAck: false, consumer: consumidor, cancellationToken: stoppingToken);
                    logger.LogInformation("Consumindo a fila {Fila}.", fila);

                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "RabbitMQ indisponível para a fila {Fila}. Nova tentativa em 5s.", fila);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
    }
}

public sealed class ConsumidorPagamentoSolicitado(
    ConexaoRabbitMq conexao,
    IServiceScopeFactory fabricaEscopo,
    ILogger<ConsumidorPagamentoSolicitado> logger)
    : ConsumidorRabbitMq<PagamentoSolicitadoMensagem>(conexao, fabricaEscopo, logger, Filas.PagamentoSolicitado)
{
    protected override Task TratarAsync(PagamentoSolicitadoMensagem mensagem, IServiceProvider servicos, CancellationToken ct)
        => servicos.GetRequiredService<ProcessarPagamentoHandler>().ExecutarAsync(mensagem, ct);
}

// Fila 2: avisa o cliente do resultado (pagamento OK ou recusado).
public sealed class ConsumidorPagamentoProcessado(
    ConexaoRabbitMq conexao,
    IServiceScopeFactory fabricaEscopo,
    ILogger<ConsumidorPagamentoProcessado> logger)
    : ConsumidorRabbitMq<PagamentoProcessadoMensagem>(conexao, fabricaEscopo, logger, Filas.PagamentoProcessado)
{
    protected override Task TratarAsync(PagamentoProcessadoMensagem mensagem, IServiceProvider servicos, CancellationToken ct)
        => servicos.GetRequiredService<NotificarResultadoHandler>().ExecutarAsync(mensagem, ct);
}
