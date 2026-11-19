using Pagamentos.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Application.Abstraction
{
    public interface IRepositorioPagamento
    {
        Task AdicionarAsync(Pagamento pagamento, CancellationToken ct = default);
        Task<Pagamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
        Task AtualizarAsync(Pagamento pagamento, CancellationToken ct = default);
    }

    public sealed record ResultadoGateway(bool Aprovado, string? Referencia = null, string? MotivoFalha = null);

    public interface IGatewayPagamento
    {
        bool Suporta(Pagamento pagamento);
        Task<ResultadoGateway> AutorizarAsync(Pagamento pagamento, CancellationToken ct = default);
    }

    public interface IPublicarMensagens
    {
        Task PublicarAsync<T>(string fila, T mensagem, CancellationToken ct = default);
    }

    public interface INotificadorCliente
    {
        Task NotificarPagamentoAprovadoAsync(Pagamento pagamento, CancellationToken ct = default);
        Task NotificarPagamentoRecusadoAsync(Pagamento pagamento, CancellationToken ct = default);
    }
}
