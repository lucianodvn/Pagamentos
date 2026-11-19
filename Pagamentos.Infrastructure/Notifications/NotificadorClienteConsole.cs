using Microsoft.Extensions.Logging;
using Pagamentos.Application.Abstraction;
using Pagamentos.Domain.Entity;

namespace Pagamentos.Infrastructure.Notifications;

public sealed class NotificadorClienteConsole(ILogger<NotificadorClienteConsole> logger) : INotificadorCliente
{
    public Task NotificarPagamentoAprovadoAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[NOTIFICAÇÃO] Para: {Email} | Olá, {Nome}! Pagamento OK. Valor R$ {Valor:N2} via {Meio} (ref. {Ref}).",
            pagamento.EmailCliente, pagamento.NomeCliente, pagamento.Valor, pagamento.Meio, pagamento.ReferenciaExterna);
        return Task.CompletedTask;
    }

    public Task NotificarPagamentoRecusadoAsync(Pagamento pagamento, CancellationToken ct = default)
    {
        logger.LogWarning(
            "[NOTIFICAÇÃO] Para: {Email} | Olá, {Nome}. Seu pagamento de R$ {Valor:N2} via {Meio} foi recusado: {Motivo}",
            pagamento.EmailCliente, pagamento.NomeCliente, pagamento.Valor, pagamento.Meio, pagamento.MotivoFalha);
        return Task.CompletedTask;
    }
}